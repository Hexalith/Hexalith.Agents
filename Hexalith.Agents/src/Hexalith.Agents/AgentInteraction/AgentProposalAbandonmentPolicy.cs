using System;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, dependency-free proposal-abandonment decision shared by the <see cref="AgentInteractionAggregate"/> abandon handler
/// and the Server abandonment orchestration so the two can never drift (AC2, AC4; AD-3, AD-5). Mirrors
/// <see cref="AgentProposalRejectionPolicy"/>: the aggregate calls <see cref="Evaluate"/> to emit the outcome event; the
/// orchestrator calls <see cref="Decide"/> to return the same decided status it dispatched.
/// </summary>
/// <remarks>
/// No method here reads I/O, time, secrets, or any sibling aggregate (AD-3). Fail-closed by construction: a successful
/// abandonment requires the <see cref="AgentProposalAbandonmentOutcome.Abandoned"/> outcome AND a
/// <see cref="ApproverPolicyValidationStatus.Valid"/> verdict (defense-in-depth — the orchestrator also refuses to dispatch
/// an unauthorized abandonment, AD-12); every other combination maps to a recorded
/// <see cref="ProposedAgentReplyAbandonmentFailed"/> with safe id + policy-basis evidence, so a failed abandonment is durable
/// Audit Evidence and prior versions are preserved (AD-5, AD-12, AD-14).
/// </remarks>
internal static class AgentProposalAbandonmentPolicy
{
    /// <summary>Evaluates the supplied abandonment result into the durable outcome event (AC2, AC4).</summary>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id) to stamp on the emitted event.</param>
    /// <param name="result">The server-assembled proposal-abandonment result.</param>
    /// <returns>A success domain result carrying exactly one outcome event (abandoned or abandonment-failed).</returns>
    internal static DomainResult Evaluate(string interactionId, AgentProposalAbandonmentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        AbandonmentDecision decision = Compute(result);
        return decision.Status == AgentInteractionStatus.ProposalAbandoned
            ? DomainResult.Success([new ProposedAgentReplyAbandoned(interactionId, decision.Evidence)])
            : DomainResult.Success([new ProposedAgentReplyAbandonmentFailed(interactionId, decision.Reason, decision.Evidence)]);
    }

    /// <summary>Computes the decided abandonment status using the same rule as <see cref="Evaluate"/> (no-drift).</summary>
    /// <param name="result">The server-assembled proposal-abandonment result.</param>
    /// <returns>The decided abandonment status.</returns>
    internal static AgentInteractionStatus Decide(AgentProposalAbandonmentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return Compute(result).Status;
    }

    // The deterministic proposal-abandonment decision. An authorized Abandoned outcome → ProposalAbandoned with safe
    // evidence. Every other combination → a fail-closed ProposalAbandonmentFailed decision with the mapped safe reason and
    // the same safe id + policy-basis evidence (the attempted ids; never content — AD-14).
    private static AbandonmentDecision Compute(AgentProposalAbandonmentResult result)
    {
        AgentProposedReplyAbandonmentEvidence evidence = Evidence(result);
        bool authorizedAbandonment = result.Outcome == AgentProposalAbandonmentOutcome.Abandoned
            && result.AuthorizationVerdict == ApproverPolicyValidationStatus.Valid;
        return authorizedAbandonment
            ? new AbandonmentDecision(AgentInteractionStatus.ProposalAbandoned, AgentProposedReplyNotAbandonableReason.Unknown, evidence)
            : new AbandonmentDecision(AgentInteractionStatus.ProposalAbandonmentFailed, MapFailureReason(result), evidence);
    }

    // Safe abandonment evidence built from the result's safe ids + the policy basis — never any version content or a
    // provider/Conversations payload/error (AD-14). Carried on both the success and failure branches.
    private static AgentProposedReplyAbandonmentEvidence Evidence(AgentProposalAbandonmentResult r)
        => new(
            r.ProposalId,
            r.SourceConversationId,
            r.ActorPartyId,
            r.ApproverPolicyVersion,
            r.AuthorizationVerdict,
            r.DisclosureCategory);

    // Maps a non-success result to its safe failure reason. A non-Valid verdict is NotAuthorized; a Valid verdict reaching
    // the failure branch means a non-Abandoned outcome and fails closed to the coarse content-free reason.
    private static AgentProposedReplyNotAbandonableReason MapFailureReason(AgentProposalAbandonmentResult result)
    {
        if (result.AuthorizationVerdict != ApproverPolicyValidationStatus.Valid)
        {
            return AgentProposedReplyNotAbandonableReason.NotAuthorized;
        }

        return result.Outcome switch
        {
            AgentProposalAbandonmentOutcome.ProposalNotPending => AgentProposedReplyNotAbandonableReason.ProposalNotPending,
            _ => AgentProposedReplyNotAbandonableReason.Unknown,
        };
    }

    // The internal carrier of one computed decision: the status, the failure reason (Unknown on success), and the safe
    // abandonment evidence. Lets Evaluate and Decide share the exact same computation.
    private readonly record struct AbandonmentDecision(
        AgentInteractionStatus Status,
        AgentProposedReplyNotAbandonableReason Reason,
        AgentProposedReplyAbandonmentEvidence Evidence);
}
