using System;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, dependency-free automatic-posting decision shared by the <see cref="AgentInteractionAggregate"/> posting handler
/// and the Server posting orchestration so the two can never drift (AC1, AC2, AC4; AD-3, AD-7). Centralizing the outcome →
/// decision math mirrors <see cref="AgentOutputGenerationPolicy"/>: the aggregate calls <see cref="Evaluate"/> to emit the
/// outcome event; the orchestrator calls <see cref="Decide"/> to return the same decided status it dispatched.
/// </summary>
/// <remarks>
/// No method here reads I/O, time, secrets, or any sibling aggregate (AD-3). It operates only on the safe
/// <see cref="AgentResponsePostingResult"/>. Fail-closed by construction: every non-<see cref="AgentResponsePostingOutcome.Posted"/>
/// outcome maps to a recorded <see cref="AgentResponsePostingFailed"/> with safe id evidence (the attempted ids), so a
/// failed post is durable Audit Evidence and no Conversation Message exists (AD-6, AD-12). The generated content is NEVER
/// carried here — posting transports only safe ids (AD-14).
/// </remarks>
internal static class AgentResponsePostingPolicy
{
    /// <summary>
    /// Evaluates the supplied posting result into the durable outcome event (AC1, AC2, AC4). <see cref="AgentResponsePostingOutcome.Posted"/>
    /// → <see cref="AgentResponsePosted"/> (status <c>Posted</c>); every other outcome → <see cref="AgentResponsePostingFailed"/>
    /// (status <c>PostingFailed</c>) with the mapped safe reason — the fail-closed Audit Evidence (FR-24).
    /// </summary>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id) to stamp on the emitted event.</param>
    /// <param name="result">The server-assembled posting result.</param>
    /// <returns>A success domain result carrying exactly one outcome event.</returns>
    internal static DomainResult Evaluate(string interactionId, AgentResponsePostingResult result)
    {
        PostingDecision decision = Compute(result);
        return decision.Status == AgentInteractionStatus.Posted
            ? DomainResult.Success([new AgentResponsePosted(interactionId, decision.Evidence)])
            : DomainResult.Success([new AgentResponsePostingFailed(interactionId, decision.Reason, decision.Evidence)]);
    }

    /// <summary>
    /// Computes the decided posting status from the supplied result using the same rule as <see cref="Evaluate"/>
    /// (<c>Posted</c> → <see cref="AgentInteractionStatus.Posted"/>; otherwise <see cref="AgentInteractionStatus.PostingFailed"/>).
    /// The orchestrator returns this so its reported status cannot drift from the aggregate's recorded decision.
    /// </summary>
    /// <param name="result">The server-assembled posting result.</param>
    /// <returns>The decided terminal posting status.</returns>
    internal static AgentInteractionStatus Decide(AgentResponsePostingResult result)
        => Compute(result).Status;

    // The deterministic posting decision. Posted → AgentResponsePosted with the safe posted-message evidence. Every other
    // outcome → a fail-closed decision with the mapped safe reason and the same safe id evidence (the attempted ids; never
    // content — AD-14).
    private static PostingDecision Compute(AgentResponsePostingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        AgentPostedMessageEvidence evidence = Evidence(result);
        return result.Outcome == AgentResponsePostingOutcome.Posted
            ? new PostingDecision(AgentInteractionStatus.Posted, AgentResponsePostingFailureReason.Unknown, evidence)
            : new PostingDecision(AgentInteractionStatus.PostingFailed, MapFailureReason(result.Outcome), evidence);
    }

    // Safe posted-message evidence built from the result's safe ids — never the generated content or a Conversations
    // payload/error (AD-14). Carried on both the success and failure branches for symmetry.
    private static AgentPostedMessageEvidence Evidence(AgentResponsePostingResult r)
        => new(r.MessageId, r.SourceConversationId, r.AgentPartyId, r.PostedVersionId);

    // Maps a non-success outcome to its safe failure reason. Unknown/unmapped fails closed to the content-free adapter generic.
    private static AgentResponsePostingFailureReason MapFailureReason(AgentResponsePostingOutcome outcome) => outcome switch
    {
        AgentResponsePostingOutcome.PartyIdentityUnavailable => AgentResponsePostingFailureReason.PartyIdentityUnavailable,
        AgentResponsePostingOutcome.MembershipUnavailable => AgentResponsePostingFailureReason.MembershipUnavailable,
        AgentResponsePostingOutcome.MembershipRejected => AgentResponsePostingFailureReason.MembershipRejected,
        AgentResponsePostingOutcome.ConversationUnavailable => AgentResponsePostingFailureReason.ConversationUnavailable,
        AgentResponsePostingOutcome.PostRejected => AgentResponsePostingFailureReason.PostRejected,
        AgentResponsePostingOutcome.AdapterFailure => AgentResponsePostingFailureReason.AdapterFailure,
        _ => AgentResponsePostingFailureReason.AdapterFailure,
    };

    // The internal carrier of one computed decision: the status, the failure reason (Unknown on success), and the safe
    // posted-message evidence. Lets Evaluate and Decide share the exact same computation.
    private readonly record struct PostingDecision(
        AgentInteractionStatus Status,
        AgentResponsePostingFailureReason Reason,
        AgentPostedMessageEvidence Evidence);
}
