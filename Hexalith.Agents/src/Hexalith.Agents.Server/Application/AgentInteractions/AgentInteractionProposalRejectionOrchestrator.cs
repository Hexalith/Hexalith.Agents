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
/// Application orchestration for an authorized Approver's rejection of a pending Proposed Agent Reply (Story 3.6; AC1, AC4;
/// AD-3, AD-5, AD-12, AD-13, AD-14). It is the minimal-deps terminal-action orchestrator: it injects only the reused
/// <see cref="IApproverPolicyResolver"/> + <see cref="IAgentCommandDispatcher"/>, re-resolves the proposal's snapshotted
/// Approver Policy against current dependency availability + freshness, computes the fail-closed verdict, assembles a
/// server-trusted <see cref="AgentProposalRejectionResult"/>, and dispatches the <see cref="RejectProposedAgentReply"/>
/// command. Mirrors <see cref="AgentInteractionProposalEditOrchestrator"/> minus the version derivation/content (a rejection
/// is a terminal-state transition, not a new version, and touches no Conversation).
/// </summary>
/// <remarks>
/// <b>Pipeline order (fail closed; FR-7, FR-20, FR-21; AD-12):</b> structural sub-state guard → authorization → assemble the
/// safe result → dispatch one command. A non-pending proposal returns a no-dispatch
/// <see cref="AgentProposedReplyNotRejectableReason.ProposalNotPending"/> denial (no command). A non-<c>Valid</c> verdict
/// still dispatches a <c>...Failed</c> command so the fail-closed decision is durable Audit Evidence (FR-24) — never a
/// terminal success. <b>Conversations boundary (AD-6):</b> a rejected proposal is NEVER a Conversation Message — this path
/// makes NO Conversations write and reads no Party identity. The returned status comes from the shared
/// <see cref="AgentProposalRejectionPolicy"/> so it cannot drift from the aggregate's recorded decision (AD-3).
/// </remarks>
public sealed class AgentInteractionProposalRejectionOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    // Reserved server-populated trust keys stripped from client-supplied extensions so a client cannot smuggle a forged
    // admin/verdict onto the interaction stream. This terminal-action path repopulates none of them.
    private static readonly string[] _reservedExtensionKeys =
    [
        "actor:agentsAdmin",
        "provider:selectionValidation",
        "approver:policyValidation",
        "party:linkValidation",
    ];

    private readonly IApproverPolicyResolver _approverPolicyResolver;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionProposalRejectionOrchestrator"/> class.</summary>
    /// <param name="approverPolicyResolver">The reused approver-policy resolution port (live binding deferred — fails closed).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentInteractionProposalRejectionOrchestrator(
        IApproverPolicyResolver approverPolicyResolver,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(approverPolicyResolver);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _approverPolicyResolver = approverPolicyResolver;
        _dispatcher = dispatcher;
    }

    /// <summary>Resolves rejection-time authorization, assembles + dispatches the reject command, and returns the safe decided outcome (AC1, AC4).</summary>
    /// <param name="request">The rejection orchestration request (ids + policy snapshot + actor + optional rationale code).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe rejection outcome (decided status), or a structural no-dispatch denial.</returns>
    public async Task<AgentInteractionProposalRejectionOutcomeResult> ExecuteAsync(AgentInteractionProposalRejectionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // (1) Structural sub-state guard (AC4): a terminal/non-pending proposal can never be rejected — deny without
        // dispatching any command, mirroring the approval orchestrator's structural denial.
        if (!IsRejectable(request.ProposalState))
        {
            return new AgentInteractionProposalRejectionOutcomeResult(
                AgentInteractionStatus.Unknown,
                AgentProposedReplyNotRejectableReason.ProposalNotPending);
        }

        // (2) Resolve rejection-time approver authorization, fail closed (FR-7; AD-12). A non-Valid verdict still records a
        // fail-closed decision (audit), so it dispatches a ...Failed command — never a success.
        ApproverPolicyValidationStatus verdict = await ResolveAuthorizationAsync(request, ct).ConfigureAwait(false);
        AgentProposalRejectionResult result = verdict == ApproverPolicyValidationStatus.Valid
            ? BuildResult(request, verdict, AgentProposalRejectionOutcome.Rejected)
            : BuildResult(
                request,
                verdict,
                verdict == ApproverPolicyValidationStatus.Unknown
                    ? AgentProposalRejectionOutcome.PolicyFailure
                    : AgentProposalRejectionOutcome.NotAuthorized);

        // (3+4) Dispatch one command (live binding deferred), then return the safe status via the SHARED policy so the
        // orchestrator's reported status cannot drift from the aggregate's recorded decision (AD-3).
        await DispatchAsync(request, result, ct).ConfigureAwait(false);
        return new AgentInteractionProposalRejectionOutcomeResult(AgentProposalRejectionPolicy.Decide(result));
    }

    // The structural rejectable set mirrors the aggregate's reject precondition: only a pending proposal (Pending/Edited/
    // Regenerated) can be rejected; any terminal/approved/posting sub-state is denied without dispatch (AC4).
    private static bool IsRejectable(ProposedAgentReplyState state)
        => state is ProposedAgentReplyState.Pending
            or ProposedAgentReplyState.Edited
            or ProposedAgentReplyState.Regenerated;

    // Re-resolves the snapshotted Approver Policy against current dependencies and computes the fail-closed verdict (AD-8,
    // AD-12). A null/empty policy is Incomplete (nothing to authorize with → denied); a resolver that throws fails closed to
    // Unavailable. A genuine cancellation propagates (the catch deliberately excludes OperationCanceledException).
    private async Task<ApproverPolicyValidationStatus> ResolveAuthorizationAsync(AgentInteractionProposalRejectionRequest request, CancellationToken ct)
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
            return ApproverPolicyValidationStatus.Unavailable;
        }
    }

    private async Task DispatchAsync(AgentInteractionProposalRejectionRequest request, AgentProposalRejectionResult result, CancellationToken ct)
    {
        var command = new RejectProposedAgentReply(request.AgentInteractionId, result);
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            request.AgentInteractionId,
            nameof(RejectProposedAgentReply),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
    }

    // Safe rejection result: safe ids + the policy basis + the optional rationale code only — never content (AD-14).
    private static AgentProposalRejectionResult BuildResult(
        AgentInteractionProposalRejectionRequest request,
        ApproverPolicyValidationStatus verdict,
        AgentProposalRejectionOutcome outcome)
        => new(
            outcome,
            request.ProposalId,
            request.SourceConversationId,
            request.ApproverPartyId,
            request.ApproverPolicyVersion,
            verdict,
            request.ApproverPolicy?.DisclosureCategory ?? ApproverPolicyBasisDisclosure.Omitted,
            request.RationaleCode);

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
