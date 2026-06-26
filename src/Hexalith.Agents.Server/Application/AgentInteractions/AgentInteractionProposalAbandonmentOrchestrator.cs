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
/// Application orchestration for an authorized Approver's abandonment of a pending Proposed Agent Reply (Story 3.6; AC2, AC4;
/// AD-3, AD-5, AD-12, AD-13, AD-14). Mirrors <see cref="AgentInteractionProposalRejectionOrchestrator"/>: it injects only the
/// reused <see cref="IApproverPolicyResolver"/> + <see cref="IAgentCommandDispatcher"/>, re-resolves the snapshotted Approver
/// Policy, computes the fail-closed verdict, assembles a server-trusted <see cref="AgentProposalAbandonmentResult"/>, and
/// dispatches the <see cref="AbandonProposedAgentReply"/> command. An abandoned proposal can never act again.
/// </summary>
/// <remarks>
/// <b>Pipeline order (fail closed; FR-7, FR-20, FR-21; AD-12):</b> structural sub-state guard → authorization → assemble the
/// safe result → dispatch one command. A non-pending proposal returns a no-dispatch
/// <see cref="AgentProposedReplyNotAbandonableReason.ProposalNotPending"/> denial. A non-<c>Valid</c> verdict still dispatches
/// a <c>...Failed</c> command (Audit Evidence; FR-24) — never a terminal success. This path makes NO Conversations write and
/// reads no Party identity (AD-6). The returned status comes from the shared <see cref="AgentProposalAbandonmentPolicy"/>
/// (AD-3 no-drift).
/// </remarks>
public sealed class AgentInteractionProposalAbandonmentOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    private static readonly string[] _reservedExtensionKeys =
    [
        "actor:agentsAdmin",
        "provider:selectionValidation",
        "approver:policyValidation",
        "party:linkValidation",
    ];

    private readonly IApproverPolicyResolver _approverPolicyResolver;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionProposalAbandonmentOrchestrator"/> class.</summary>
    /// <param name="approverPolicyResolver">The reused approver-policy resolution port (live binding deferred — fails closed).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentInteractionProposalAbandonmentOrchestrator(
        IApproverPolicyResolver approverPolicyResolver,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(approverPolicyResolver);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _approverPolicyResolver = approverPolicyResolver;
        _dispatcher = dispatcher;
    }

    /// <summary>Resolves abandonment-time authorization, assembles + dispatches the abandon command, and returns the safe decided outcome (AC2, AC4).</summary>
    /// <param name="request">The abandonment orchestration request (ids + policy snapshot + actor).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe abandonment outcome (decided status), or a structural no-dispatch denial.</returns>
    public async Task<AgentInteractionProposalAbandonmentOutcomeResult> ExecuteAsync(AgentInteractionProposalAbandonmentRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsAbandonable(request.ProposalState))
        {
            return new AgentInteractionProposalAbandonmentOutcomeResult(
                AgentInteractionStatus.Unknown,
                AgentProposedReplyNotAbandonableReason.ProposalNotPending);
        }

        ApproverPolicyValidationStatus verdict = await ResolveAuthorizationAsync(request, ct).ConfigureAwait(false);
        AgentProposalAbandonmentResult result = verdict == ApproverPolicyValidationStatus.Valid
            ? BuildResult(request, verdict, AgentProposalAbandonmentOutcome.Abandoned)
            : BuildResult(
                request,
                verdict,
                verdict == ApproverPolicyValidationStatus.Unknown
                    ? AgentProposalAbandonmentOutcome.PolicyFailure
                    : AgentProposalAbandonmentOutcome.NotAuthorized);

        await DispatchAsync(request, result, ct).ConfigureAwait(false);
        return new AgentInteractionProposalAbandonmentOutcomeResult(AgentProposalAbandonmentPolicy.Decide(result));
    }

    private static bool IsAbandonable(ProposedAgentReplyState state)
        => state is ProposedAgentReplyState.Pending
            or ProposedAgentReplyState.Edited
            or ProposedAgentReplyState.Regenerated;

    private async Task<ApproverPolicyValidationStatus> ResolveAuthorizationAsync(AgentInteractionProposalAbandonmentRequest request, CancellationToken ct)
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

    private async Task DispatchAsync(AgentInteractionProposalAbandonmentRequest request, AgentProposalAbandonmentResult result, CancellationToken ct)
    {
        var command = new AbandonProposedAgentReply(request.AgentInteractionId, result);
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            request.AgentInteractionId,
            nameof(AbandonProposedAgentReply),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
    }

    private static AgentProposalAbandonmentResult BuildResult(
        AgentInteractionProposalAbandonmentRequest request,
        ApproverPolicyValidationStatus verdict,
        AgentProposalAbandonmentOutcome outcome)
        => new(
            outcome,
            request.ProposalId,
            request.SourceConversationId,
            request.ApproverPartyId,
            request.ApproverPolicyVersion,
            verdict,
            request.ApproverPolicy?.DisclosureCategory ?? ApproverPolicyBasisDisclosure.Omitted);

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
