using System;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, dependency-free proposal-rejection decision shared by the <see cref="AgentInteractionAggregate"/> reject handler and
/// the Server rejection orchestration so the two can never drift (AC1, AC4; AD-3, AD-5). Centralizing the outcome → decision
/// math mirrors <see cref="AgentProposalEditPolicy"/>: the aggregate calls <see cref="Evaluate"/> to emit the outcome event;
/// the orchestrator calls <see cref="Decide"/> to return the same decided status it dispatched.
/// </summary>
/// <remarks>
/// No method here reads I/O, time, secrets, or any sibling aggregate (AD-3). It operates only on the safe
/// <see cref="AgentProposalRejectionResult"/>. Fail-closed by construction: a successful rejection requires the
/// <see cref="AgentProposalRejectionOutcome.Rejected"/> outcome AND a <see cref="ApproverPolicyValidationStatus.Valid"/>
/// authorization verdict (defense-in-depth — the orchestrator also refuses to dispatch an unauthorized rejection, AD-12);
/// every other combination maps to a recorded <see cref="ProposedAgentReplyRejectionFailed"/> with safe id + policy-basis
/// evidence, so a failed rejection is durable Audit Evidence and prior versions are preserved (AD-5, AD-12, AD-14).
/// </remarks>
internal static class AgentProposalRejectionPolicy
{
    /// <summary>Evaluates the supplied rejection result into the durable outcome event (AC1, AC4).</summary>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id) to stamp on the emitted event.</param>
    /// <param name="result">The server-assembled proposal-rejection result.</param>
    /// <returns>A success domain result carrying exactly one outcome event (rejected or rejection-failed).</returns>
    internal static DomainResult Evaluate(string interactionId, AgentProposalRejectionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        RejectionDecision decision = Compute(result);
        return decision.Status == AgentInteractionStatus.ProposalRejected
            ? DomainResult.Success([new ProposedAgentReplyRejected(interactionId, decision.Evidence)])
            : DomainResult.Success([new ProposedAgentReplyRejectionFailed(interactionId, decision.Reason, decision.Evidence)]);
    }

    /// <summary>Computes the decided rejection status using the same rule as <see cref="Evaluate"/> (no-drift).</summary>
    /// <param name="result">The server-assembled proposal-rejection result.</param>
    /// <returns>The decided rejection status.</returns>
    internal static AgentInteractionStatus Decide(AgentProposalRejectionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return Compute(result).Status;
    }

    // The deterministic proposal-rejection decision. An authorized Rejected outcome → ProposalRejected with safe evidence.
    // Every other combination → a fail-closed ProposalRejectionFailed decision with the mapped safe reason and the same safe
    // id + policy-basis evidence (the attempted ids; never content — AD-14).
    private static RejectionDecision Compute(AgentProposalRejectionResult result)
    {
        AgentProposedReplyRejectionEvidence evidence = Evidence(result);
        bool authorizedRejection = result.Outcome == AgentProposalRejectionOutcome.Rejected
            && result.AuthorizationVerdict == ApproverPolicyValidationStatus.Valid;
        return authorizedRejection
            ? new RejectionDecision(AgentInteractionStatus.ProposalRejected, AgentProposedReplyNotRejectableReason.Unknown, evidence)
            : new RejectionDecision(AgentInteractionStatus.ProposalRejectionFailed, MapFailureReason(result), evidence);
    }

    // Safe rejection evidence built from the result's safe ids + the policy basis + the optional rationale code — never any
    // version content or a provider/Conversations payload/error (AD-14). Carried on both the success and failure branches.
    private static AgentProposedReplyRejectionEvidence Evidence(AgentProposalRejectionResult r)
        => new(
            r.ProposalId,
            r.SourceConversationId,
            r.ActorPartyId,
            r.ApproverPolicyVersion,
            r.AuthorizationVerdict,
            r.DisclosureCategory,
            r.RationaleCode);

    // Maps a non-success result to its safe failure reason. A non-Valid verdict is NotAuthorized; a Valid verdict reaching
    // the failure branch means a non-Rejected outcome and fails closed to the coarse content-free reason.
    private static AgentProposedReplyNotRejectableReason MapFailureReason(AgentProposalRejectionResult result)
    {
        if (result.AuthorizationVerdict != ApproverPolicyValidationStatus.Valid)
        {
            return AgentProposedReplyNotRejectableReason.NotAuthorized;
        }

        return result.Outcome switch
        {
            AgentProposalRejectionOutcome.ProposalNotPending => AgentProposedReplyNotRejectableReason.ProposalNotPending,
            _ => AgentProposedReplyNotRejectableReason.Unknown,
        };
    }

    // The internal carrier of one computed decision: the status, the failure reason (Unknown on success), and the safe
    // rejection evidence. Lets Evaluate and Decide share the exact same computation.
    private readonly record struct RejectionDecision(
        AgentInteractionStatus Status,
        AgentProposedReplyNotRejectableReason Reason,
        AgentProposedReplyRejectionEvidence Evidence);
}
