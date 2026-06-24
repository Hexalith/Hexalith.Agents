using System;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, dependency-free proposal-creation decision shared by the <see cref="AgentInteractionAggregate"/> creation handler
/// and the Server proposal orchestration so the two can never drift (AC1, AC3, AC4; AD-3, AD-5). Centralizing the outcome →
/// decision math mirrors <see cref="AgentResponsePostingPolicy"/>: the aggregate calls <see cref="Evaluate"/> to emit the
/// outcome event; the orchestrator calls <see cref="Decide"/> to return the same decided status it dispatched.
/// </summary>
/// <remarks>
/// No method here reads I/O, time, secrets, or any sibling aggregate (AD-3). It operates only on the safe
/// <see cref="AgentProposalCreationResult"/>. Fail-closed by construction: every non-<see cref="AgentProposalCreationOutcome.Created"/>
/// outcome maps to a recorded <see cref="ProposedAgentReplyCreationFailed"/> with safe id evidence (the attempted ids), so a
/// failed creation is durable Audit Evidence and no approvable proposal exists (AD-5, AD-12). The generated content is NEVER
/// carried here — proposal creation transports only safe ids (AD-14).
/// </remarks>
internal static class AgentProposalCreationPolicy
{
    /// <summary>
    /// Evaluates the supplied proposal-creation result into the durable outcome event (AC1, AC3, AC4). <see cref="AgentProposalCreationOutcome.Created"/>
    /// → <see cref="ProposedAgentReplyCreated"/> (status <c>ProposalCreated</c>); every other outcome → <see cref="ProposedAgentReplyCreationFailed"/>
    /// (status <c>ProposalCreationFailed</c>) with the mapped safe reason — the fail-closed Audit Evidence (FR-24).
    /// </summary>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id) to stamp on the emitted event.</param>
    /// <param name="result">The server-assembled proposal-creation result.</param>
    /// <returns>A success domain result carrying exactly one outcome event.</returns>
    internal static DomainResult Evaluate(string interactionId, AgentProposalCreationResult result)
    {
        ProposalDecision decision = Compute(result);
        return decision.Status == AgentInteractionStatus.ProposalCreated
            ? DomainResult.Success([new ProposedAgentReplyCreated(interactionId, decision.Evidence)])
            : DomainResult.Success([new ProposedAgentReplyCreationFailed(interactionId, decision.Reason, decision.Evidence)]);
    }

    /// <summary>
    /// Computes the decided creation status from the supplied result using the same rule as <see cref="Evaluate"/>
    /// (<c>Created</c> → <see cref="AgentInteractionStatus.ProposalCreated"/>; otherwise <see cref="AgentInteractionStatus.ProposalCreationFailed"/>).
    /// The orchestrator returns this so its reported status cannot drift from the aggregate's recorded decision.
    /// </summary>
    /// <param name="result">The server-assembled proposal-creation result.</param>
    /// <returns>The decided terminal creation status.</returns>
    internal static AgentInteractionStatus Decide(AgentProposalCreationResult result)
        => Compute(result).Status;

    // The deterministic proposal-creation decision. Created → ProposedAgentReplyCreated with the safe proposal evidence.
    // Every other outcome → a fail-closed decision with the mapped safe reason and the same safe id evidence (the attempted
    // ids; never content — AD-14).
    private static ProposalDecision Compute(AgentProposalCreationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        AgentProposedReplyEvidence evidence = Evidence(result);
        return result.Outcome == AgentProposalCreationOutcome.Created
            ? new ProposalDecision(AgentInteractionStatus.ProposalCreated, AgentProposalCreationFailureReason.Unknown, evidence)
            : new ProposalDecision(AgentInteractionStatus.ProposalCreationFailed, MapFailureReason(result.Outcome), evidence);
    }

    // Safe proposal evidence built from the result's safe ids — never the generated content or a provider/Conversations
    // payload/error (AD-14). Carried on both the success and failure branches for symmetry.
    private static AgentProposedReplyEvidence Evidence(AgentProposalCreationResult r)
        => new(
            r.ProposalId,
            r.SourceConversationId,
            r.ProposedVersionId,
            r.ApproverPolicyVersion,
            r.ContentSafetyPolicyVersion,
            r.ExpiresAt);

    // Maps a non-success outcome to its safe failure reason. Unknown/unmapped fails closed to the content-free adapter generic.
    private static AgentProposalCreationFailureReason MapFailureReason(AgentProposalCreationOutcome outcome) => outcome switch
    {
        AgentProposalCreationOutcome.GeneratedVersionUnavailable => AgentProposalCreationFailureReason.GeneratedVersionUnavailable,
        AgentProposalCreationOutcome.AdapterFailure => AgentProposalCreationFailureReason.AdapterFailure,
        _ => AgentProposalCreationFailureReason.AdapterFailure,
    };

    // The internal carrier of one computed decision: the status, the failure reason (Unknown on success), and the safe
    // proposal evidence. Lets Evaluate and Decide share the exact same computation.
    private readonly record struct ProposalDecision(
        AgentInteractionStatus Status,
        AgentProposalCreationFailureReason Reason,
        AgentProposedReplyEvidence Evidence);
}
