using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Application orchestration for an authorized Approver's edit of a pending Proposed Agent Reply (Story 3.3; AC1, AC2, AC4;
/// AD-3, AD-5, AD-12, AD-13, AD-14). It is the first <b>edit-time approver-authorization</b> use: it re-resolves the
/// proposal's snapshotted Approver Policy against current dependency availability + freshness, computes the fail-closed
/// verdict, derives a deterministic edited-version id, assembles a server-trusted <see cref="AgentProposalEditResult"/>
/// (carrying the user's edited content on the new version), and dispatches the <see cref="EditProposedAgentReply"/>
/// command. The impure work happens here, outside the pure aggregate, and feeds back through the single command (the AD-3
/// round-trip). Mirrors <see cref="AgentInteractionProposalOrchestrator"/> plus the
/// <see cref="AgentActivationProviderRevalidation"/> snapshot → re-resolve → verdict authorization pattern.
/// </summary>
/// <remarks>
/// <b>Fail closed (FR-15; AD-12):</b> authorization is resolved BEFORE any edit is accepted and fails closed on any
/// missing/stale/ambiguous/disabled/unavailable/cross-tenant/uncertain approver-policy result — a non-<c>Valid</c> verdict
/// returns a no-dispatch <see cref="AgentProposedReplyNotEditableReason.NotAuthorized"/> denial (no command, no event). The
/// approver resolver folds projection staleness into its <c>Unavailable</c> outcomes, which the pure
/// <see cref="ApproverPolicyVerdict"/> already fails closed on, so a stale dependency never authorizes an edit.
/// <b>Conversations boundary (AD-6):</b> an edited Proposed Agent Reply is NEVER a Conversation Message — this path makes
/// NO Conversations write. <b>Content confinement (AD-14):</b> the user's edited content rides into the aggregate ONLY on
/// the edit command's version; the returned outcome carries safe ids + status only. The returned status comes from the
/// shared <see cref="AgentProposalEditPolicy"/> so it cannot drift from the aggregate's recorded decision.
/// </remarks>
public sealed class AgentInteractionProposalEditOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    // Reserved server-populated trust keys. This edit path repopulates none of them; they are stripped from
    // client-supplied extensions so a client cannot smuggle a forged admin/verdict onto the interaction stream.
    private static readonly string[] _reservedExtensionKeys =
    [
        "actor:agentsAdmin",
        "provider:selectionValidation",
        "approver:policyValidation",
        "party:linkValidation",
    ];

    private readonly IApproverPolicyResolver _approverPolicyResolver;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionProposalEditOrchestrator"/> class.</summary>
    /// <param name="approverPolicyResolver">The reused approver-policy resolution port (live binding deferred — fails closed).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentInteractionProposalEditOrchestrator(
        IApproverPolicyResolver approverPolicyResolver,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(approverPolicyResolver);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _approverPolicyResolver = approverPolicyResolver;
        _dispatcher = dispatcher;
    }

    /// <summary>Resolves edit-time authorization, assembles + dispatches the edit command, and returns the safe decided outcome (AC1, AC2, AC4).</summary>
    /// <param name="request">The edit orchestration request (ids + policy snapshot + source version + edited content + caller context).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe edit outcome (edited version id + decided status), or a no-dispatch fail-closed denial.</returns>
    public async Task<AgentInteractionProposalEditOutcomeResult> ExecuteAsync(AgentInteractionProposalEditRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // (1) Resolve edit-time approver authorization, fail closed (AD-12). A non-Valid verdict denies the edit WITHOUT
        // dispatching any command — WHO may act is enforced here, before the aggregate accepts an event (FR-15).
        ApproverPolicyValidationStatus verdict = await ResolveAuthorizationAsync(request, ct).ConfigureAwait(false);
        if (verdict != ApproverPolicyValidationStatus.Valid)
        {
            return new AgentInteractionProposalEditOutcomeResult(
                string.Empty,
                AgentInteractionStatus.Unknown,
                AgentProposedReplyNotEditableReason.NotAuthorized);
        }

        // (2) Derive the deterministic edited-version id from the interaction + source version + edit attempt so a retry
        // reuses the same id and the aggregate's terminal no-op dedupes the edit (AD-13).
        string editedVersionId = AgentProposalEditIdentity.DeriveEditedVersionId(
            request.AgentInteractionId, request.SourceVersionId, request.EditAttemptId);

        // (3) Assemble the server-trusted edit result: the new immutable Edited version carries the user's edited content +
        // ids + provenance; the verdict + policy basis ride the result for the AC4 audit evidence.
        var result = new AgentProposalEditResult(
            AgentProposalEditOutcome.Edited,
            BuildEditedVersion(request, editedVersionId),
            verdict,
            request.ProposalId,
            request.SourceConversationId,
            request.ApproverPolicyVersion,
            request.ApproverPolicy?.DisclosureCategory ?? ApproverPolicyBasisDisclosure.Omitted);

        // (4) Build the server-trusted edit command + envelope (AggregateId = interaction id, Domain = agent-interaction);
        // strip reserved client trust extensions (none repopulated on this path).
        var command = new EditProposedAgentReply(request.AgentInteractionId, result);
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            request.AgentInteractionId,
            nameof(EditProposedAgentReply),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        // (5) Dispatch (live binding deferred), then return the safe outcome — status via the SHARED policy so the
        // orchestrator's reported status cannot drift from the aggregate's recorded decision (AD-3).
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
        return new AgentInteractionProposalEditOutcomeResult(editedVersionId, AgentProposalEditPolicy.Decide(result));
    }

    // Re-resolves the snapshotted Approver Policy against current dependencies and computes the fail-closed verdict
    // (AD-8, AD-12). A null/empty policy is Incomplete (nothing to authorize with → denied); a resolver that throws fails
    // closed to Unavailable. A genuine cancellation propagates (the catch deliberately excludes OperationCanceledException).
    private async Task<ApproverPolicyValidationStatus> ResolveAuthorizationAsync(AgentInteractionProposalEditRequest request, CancellationToken ct)
    {
        if (request.ApproverPolicy is not { Sources.Count: > 0 } policy)
        {
            return ApproverPolicyValidationStatus.Incomplete;
        }

        try
        {
            ApproverPolicyResolutionResult resolution = await _approverPolicyResolver
                .ResolveAsync(request.TenantId, policy, ct)
                .ConfigureAwait(false);
            return ApproverPolicyVerdict.Evaluate(policy, resolution);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — never propagate the raw exception (AD-14: no stack traces/payloads leak).
            return ApproverPolicyValidationStatus.Unavailable;
        }
    }

    // Builds the new immutable Edited version: the user's edited content + the deterministic edited version id + Kind=Edited
    // + the source/editor provenance. Provider/model/capability are inherited from the source version for audit provenance;
    // there is no provider call, so the token counts are zero (Dev Notes "Edited-version model decision").
    private static AgentGeneratedVersion BuildEditedVersion(AgentInteractionProposalEditRequest request, string editedVersionId)
        => new(
            editedVersionId,
            request.EditAttemptId,
            AgentGenerationKind.Edited,
            request.EditedContent,
            request.SourceProviderId,
            request.SourceModelId,
            request.SourceProviderCapabilityVersion,
            request.ContentSafetyPolicyVersion,
            PromptTokenCount: 0,
            OutputTokenCount: 0,
            request.SourceVersionId,
            request.EditorPartyId);

    // Copies the client-supplied extensions with the reserved trust keys removed; repopulates none (the edit path carries
    // no admin/verdict extension — the verdict rides the trusted command result, not an envelope extension). Returns null
    // when nothing benign remains so the envelope carries no empty map.
    private static Dictionary<string, string>? BuildTrustedExtensions(IReadOnlyDictionary<string, string>? clientSupplied)
    {
        if (clientSupplied is null)
        {
            return null;
        }

        var extensions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string key, string value) in clientSupplied)
        {
            if (Array.IndexOf(_reservedExtensionKeys, key) < 0)
            {
                extensions[key] = value;
            }
        }

        return extensions.Count > 0 ? extensions : null;
    }
}
