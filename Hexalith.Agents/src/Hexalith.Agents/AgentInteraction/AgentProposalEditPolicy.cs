using System;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, dependency-free proposal-edit decision shared by the <see cref="AgentInteractionAggregate"/> edit handler and the
/// Server edit orchestration so the two can never drift (AC1, AC2, AC4; AD-3, AD-5). Centralizing the outcome → decision
/// math mirrors <see cref="AgentProposalCreationPolicy"/>: the aggregate calls <see cref="Evaluate"/> to emit the outcome
/// event; the orchestrator calls <see cref="Decide"/> to return the same decided status it dispatched.
/// </summary>
/// <remarks>
/// No method here reads I/O, time, secrets, or any sibling aggregate (AD-3). It operates only on the safe-plus-content
/// <see cref="AgentProposalEditResult"/>. Fail-closed by construction: success requires the
/// <see cref="AgentProposalEditOutcome.Edited"/> outcome AND a <see cref="ApproverPolicyValidationStatus.Valid"/>
/// authorization verdict (defense-in-depth — the orchestrator also refuses to dispatch an unauthorized edit, AD-12); every
/// other combination maps to a recorded <see cref="ProposedAgentReplyEditFailed"/> with safe id + policy-basis evidence, so
/// a failed edit is durable Audit Evidence and prior versions are preserved (AD-5, AD-12). The edited content rides only on
/// the success event's version (AD-14) — never on the failure event or the evidence.
/// </remarks>
internal static class AgentProposalEditPolicy
{
    /// <summary>
    /// Evaluates the supplied proposal-edit result into the durable outcome event (AC1, AC2, AC4). A
    /// <see cref="AgentProposalEditOutcome.Edited"/> outcome with a <see cref="ApproverPolicyValidationStatus.Valid"/>
    /// verdict → <see cref="ProposedAgentReplyEdited"/> (status <c>ProposalEdited</c>, carrying the content-bearing edited
    /// version); every other combination → <see cref="ProposedAgentReplyEditFailed"/> (status <c>ProposalEditFailed</c>)
    /// with the mapped safe reason — the fail-closed Audit Evidence (FR-24).
    /// </summary>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id) to stamp on the emitted event.</param>
    /// <param name="result">The server-assembled proposal-edit result.</param>
    /// <returns>A success domain result carrying exactly one outcome event.</returns>
    internal static DomainResult Evaluate(string interactionId, AgentProposalEditResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        EditDecision decision = Compute(result);
        return decision.Status == AgentInteractionStatus.ProposalEdited
            ? DomainResult.Success([new ProposedAgentReplyEdited(interactionId, result.EditedVersion, decision.Evidence)])
            : DomainResult.Success([new ProposedAgentReplyEditFailed(interactionId, decision.Reason, decision.Evidence)]);
    }

    /// <summary>
    /// Computes the decided edit status from the supplied result using the same rule as <see cref="Evaluate"/>
    /// (authorized <c>Edited</c> → <see cref="AgentInteractionStatus.ProposalEdited"/>; otherwise
    /// <see cref="AgentInteractionStatus.ProposalEditFailed"/>). The orchestrator returns this so its reported status cannot
    /// drift from the aggregate's recorded decision.
    /// </summary>
    /// <param name="result">The server-assembled proposal-edit result.</param>
    /// <returns>The decided terminal edit status.</returns>
    internal static AgentInteractionStatus Decide(AgentProposalEditResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return Compute(result).Status;
    }

    // The deterministic proposal-edit decision. An authorized Edited outcome → ProposedAgentReplyEdited with the edited
    // version + safe evidence. Every other combination → a fail-closed decision with the mapped safe reason and the same
    // safe id + policy-basis evidence (the attempted ids; never content — AD-14).
    private static EditDecision Compute(AgentProposalEditResult result)
    {
        AgentProposedReplyEditEvidence evidence = Evidence(result);
        bool authorizedEdit = result.Outcome == AgentProposalEditOutcome.Edited
            && result.AuthorizationVerdict == ApproverPolicyValidationStatus.Valid;
        return authorizedEdit
            ? new EditDecision(AgentInteractionStatus.ProposalEdited, AgentProposalEditFailureReason.Unknown, evidence)
            : new EditDecision(AgentInteractionStatus.ProposalEditFailed, MapFailureReason(result), evidence);
    }

    // Safe edit evidence built from the result's safe ids + the version provenance + the policy basis — never the edited
    // content or a provider/Conversations payload/error (AD-14). Carried on both the success and failure branches for symmetry.
    private static AgentProposedReplyEditEvidence Evidence(AgentProposalEditResult r)
        => new(
            r.ProposalId,
            r.SourceConversationId,
            r.EditedVersion.VersionId,
            r.EditedVersion.SourceVersionId ?? string.Empty,
            r.EditedVersion.EditorPartyId ?? string.Empty,
            r.ApproverPolicyVersion,
            r.AuthorizationVerdict,
            r.DisclosureCategory);

    // Maps a non-success result to its safe failure reason. A non-Valid verdict is NotAuthorized; an adapter/garbage
    // outcome fails closed to the content-free adapter generic.
    private static AgentProposalEditFailureReason MapFailureReason(AgentProposalEditResult result)
    {
        if (result.AuthorizationVerdict != ApproverPolicyValidationStatus.Valid)
        {
            return AgentProposalEditFailureReason.NotAuthorized;
        }

        // A Valid verdict reaching the failure branch means a non-Edited outcome (an AdapterFailure or a garbage
        // Unknown); both fail closed to the content-free adapter generic (AD-12, AD-14).
        return AgentProposalEditFailureReason.AdapterFailure;
    }

    // The internal carrier of one computed decision: the status, the failure reason (Unknown on success), and the safe
    // edit evidence. Lets Evaluate and Decide share the exact same computation.
    private readonly record struct EditDecision(
        AgentInteractionStatus Status,
        AgentProposalEditFailureReason Reason,
        AgentProposedReplyEditEvidence Evidence);
}
