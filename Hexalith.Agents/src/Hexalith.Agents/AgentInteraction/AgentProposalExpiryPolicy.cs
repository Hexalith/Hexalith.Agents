using System;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, dependency-free proposal-expiry decision shared by the <see cref="AgentInteractionAggregate"/> expire handler and
/// the Server expiry orchestration so the two can never drift (AC3; AD-3, AD-5). Mirrors <see cref="AgentProposalEditPolicy"/>
/// in shape but differs in authority: expiry is system policy, so there is NO approver verdict — the elapsed
/// <c>ExpiresAt</c> (decided OUTSIDE the aggregate and carried on the result) is the sole authority (AD-3). The aggregate
/// never reads the clock.
/// </summary>
/// <remarks>
/// No method here reads I/O, time, secrets, or any sibling aggregate (AD-3). Only the
/// <see cref="AgentProposalExpiryOutcome.Expired"/> outcome transitions the proposal to
/// <see cref="ProposedAgentReplyState.Expired"/>; every other outcome (no-expiry / expiry-not-reached / already-terminal) is
/// a deterministic no-transition — <see cref="Evaluate"/> returns <see cref="DomainResult.NoOp"/> and records nothing,
/// because expiry has no fail-closed "expiry-failed" terminal state (the orchestrator simply does not dispatch those cases).
/// </remarks>
internal static class AgentProposalExpiryPolicy
{
    /// <summary>Evaluates the supplied expiry result into the durable outcome event (AC3): an <c>Expired</c> outcome emits one <see cref="ProposedAgentReplyExpired"/>; any other outcome is a no-op.</summary>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id) to stamp on the emitted event.</param>
    /// <param name="result">The server-assembled proposal-expiry result.</param>
    /// <returns>A success domain result carrying the expired event, or a no-op when the expiry did not elapse.</returns>
    internal static DomainResult Evaluate(string interactionId, AgentProposalExpiryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ExpiryDecision decision = Compute(result);
        return decision.Status == AgentInteractionStatus.ProposalExpired
            ? DomainResult.Success([new ProposedAgentReplyExpired(interactionId, decision.Evidence)])
            : DomainResult.NoOp();
    }

    /// <summary>Computes the decided expiry status using the same rule as <see cref="Evaluate"/> (no-drift): <c>Expired</c> → <see cref="AgentInteractionStatus.ProposalExpired"/>; otherwise <see cref="AgentInteractionStatus.Unknown"/> (no transition).</summary>
    /// <param name="result">The server-assembled proposal-expiry result.</param>
    /// <returns>The decided expiry status.</returns>
    internal static AgentInteractionStatus Decide(AgentProposalExpiryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return Compute(result).Status;
    }

    // The deterministic proposal-expiry decision. An Expired outcome → ProposalExpired with safe evidence; every other
    // outcome → Unknown (no transition). The recorded ExpiresAt is the sole authority — the aggregate never reads the clock.
    private static ExpiryDecision Compute(AgentProposalExpiryResult result)
    {
        AgentProposedReplyExpiryEvidence evidence = Evidence(result);
        return result.Outcome == AgentProposalExpiryOutcome.Expired
            ? new ExpiryDecision(AgentInteractionStatus.ProposalExpired, evidence)
            : new ExpiryDecision(AgentInteractionStatus.Unknown, evidence);
    }

    // Safe expiry evidence built from the result's safe ids + the recorded expiry timestamp — never any version content or a
    // payload/error (AD-14).
    private static AgentProposedReplyExpiryEvidence Evidence(AgentProposalExpiryResult r)
        => new(r.ProposalId, r.SourceConversationId, r.ExpiresAt);

    // The internal carrier of one computed decision: the status and the safe expiry evidence. Lets Evaluate and Decide share
    // the exact same computation.
    private readonly record struct ExpiryDecision(
        AgentInteractionStatus Status,
        AgentProposedReplyExpiryEvidence Evidence);
}
