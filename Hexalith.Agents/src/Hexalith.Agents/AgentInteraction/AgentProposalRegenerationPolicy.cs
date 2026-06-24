using System;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, dependency-free proposal-regeneration decision shared by the <see cref="AgentInteractionAggregate"/> regeneration
/// handler and the Server regeneration orchestration so the two can never drift (AC1, AC2, AC3, AC4; AD-3, AD-5). Centralizing
/// the outcome → decision math mirrors <see cref="AgentProposalEditPolicy"/> and <see cref="AgentOutputGenerationPolicy"/>:
/// the aggregate calls <see cref="Evaluate"/> to emit the outcome event; the orchestrator calls <see cref="Decide"/> to return
/// the same decided status it dispatched.
/// </summary>
/// <remarks>
/// No method here reads I/O, time, secrets, or any sibling aggregate (AD-3). It operates only on the safe-plus-content
/// <see cref="AgentProposalRegenerationResult"/>. Fail-closed by construction: success requires the
/// <see cref="AgentProposalRegenerationOutcome.Regenerated"/> outcome AND a <see cref="ApproverPolicyValidationStatus.Valid"/>
/// authorization verdict AND a non-null content-bearing version (defense-in-depth — the orchestrator also refuses to dispatch
/// an unauthorized/unsafe regeneration, AD-12); every other combination maps to a recorded
/// <see cref="ProposedAgentReplyRegenerationFailed"/> with safe id + provider/policy-basis evidence and NO version, so a
/// failed regeneration is durable Audit Evidence, prior versions are preserved, and the proposal stays retryable (AC3; AD-5,
/// AD-12). The regenerated content rides only on the success event's version (AD-14) — never on the failure event or the
/// evidence.
/// </remarks>
internal static class AgentProposalRegenerationPolicy
{
    /// <summary>
    /// Evaluates the supplied proposal-regeneration result into the durable outcome event (AC1, AC2, AC3, AC4). A
    /// <see cref="AgentProposalRegenerationOutcome.Regenerated"/> outcome with a
    /// <see cref="ApproverPolicyValidationStatus.Valid"/> verdict and a content-bearing version →
    /// <see cref="ProposedAgentReplyRegenerated"/> (status <c>ProposalRegenerated</c>, carrying the regenerated version);
    /// every other combination → <see cref="ProposedAgentReplyRegenerationFailed"/> (status <c>ProposalRegenerationFailed</c>)
    /// with the mapped safe reason — the fail-closed Audit Evidence (FR-24).
    /// </summary>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id) to stamp on the emitted event.</param>
    /// <param name="result">The server-assembled proposal-regeneration result.</param>
    /// <returns>A success domain result carrying exactly one outcome event.</returns>
    internal static DomainResult Evaluate(string interactionId, AgentProposalRegenerationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        RegenerationDecision decision = Compute(result);
        return decision.Status == AgentInteractionStatus.ProposalRegenerated
            ? DomainResult.Success([new ProposedAgentReplyRegenerated(interactionId, decision.Version!, decision.Evidence)])
            : DomainResult.Success([new ProposedAgentReplyRegenerationFailed(interactionId, decision.Reason, decision.Evidence)]);
    }

    /// <summary>
    /// Computes the decided regeneration status from the supplied result using the same rule as <see cref="Evaluate"/>
    /// (authorized + safe <c>Regenerated</c> → <see cref="AgentInteractionStatus.ProposalRegenerated"/>; otherwise
    /// <see cref="AgentInteractionStatus.ProposalRegenerationFailed"/>). The orchestrator returns this so its reported status
    /// cannot drift from the aggregate's recorded decision.
    /// </summary>
    /// <param name="result">The server-assembled proposal-regeneration result.</param>
    /// <returns>The decided terminal regeneration status.</returns>
    internal static AgentInteractionStatus Decide(AgentProposalRegenerationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return Compute(result).Status;
    }

    // The deterministic proposal-regeneration decision. An authorized Regenerated outcome carrying a content-bearing version →
    // ProposedAgentReplyRegenerated with that version + safe evidence. Every other combination → a fail-closed decision with
    // the mapped safe reason and the same safe id + provider/policy-basis evidence (the attempted ids; never content — AD-14).
    private static RegenerationDecision Compute(AgentProposalRegenerationResult result)
    {
        AgentProposedReplyRegenerationEvidence evidence = Evidence(result);
        bool authorizedRegeneration = result.Outcome == AgentProposalRegenerationOutcome.Regenerated
            && result.AuthorizationVerdict == ApproverPolicyValidationStatus.Valid
            && result.RegeneratedVersion is not null;
        return authorizedRegeneration
            ? new RegenerationDecision(AgentInteractionStatus.ProposalRegenerated, result.RegeneratedVersion, AgentProposalRegenerationFailureReason.Unknown, evidence)
            : new RegenerationDecision(AgentInteractionStatus.ProposalRegenerationFailed, Version: null, MapFailureReason(result), evidence);
    }

    // Safe regeneration evidence built from the result's safe ids + provider/model/policy versions + the policy basis — never
    // the regenerated content or a provider/Conversations payload/error (AD-9, AD-14). Carried on both branches for symmetry.
    private static AgentProposedReplyRegenerationEvidence Evidence(AgentProposalRegenerationResult r)
        => new(
            r.ProposalId,
            r.SourceConversationId,
            r.RegeneratedVersionId,
            r.RegenerationAttemptId,
            r.RequesterPartyId,
            r.ProviderId,
            r.ModelId,
            r.ProviderCapabilityVersion,
            r.ContentSafetyPolicyVersion,
            r.ApproverPolicyVersion,
            r.AuthorizationVerdict,
            r.DisclosureCategory);

    // Maps a non-success result to its safe failure reason. A non-Valid verdict dominates (NotAuthorized, defense in depth);
    // otherwise the provider-class outcome maps straight through, and a garbage outcome (incl. Regenerated with no version)
    // fails closed to the content-free adapter generic (AD-12, AD-14).
    private static AgentProposalRegenerationFailureReason MapFailureReason(AgentProposalRegenerationResult result)
    {
        if (result.AuthorizationVerdict != ApproverPolicyValidationStatus.Valid)
        {
            return AgentProposalRegenerationFailureReason.NotAuthorized;
        }

        return result.Outcome switch
        {
            AgentProposalRegenerationOutcome.ProviderTimeout => AgentProposalRegenerationFailureReason.ProviderTimeout,
            AgentProposalRegenerationOutcome.ProviderDisabled => AgentProposalRegenerationFailureReason.ProviderDisabled,
            AgentProposalRegenerationOutcome.ProviderUnavailable => AgentProposalRegenerationFailureReason.ProviderUnavailable,
            AgentProposalRegenerationOutcome.AdapterFailure => AgentProposalRegenerationFailureReason.AdapterFailure,
            AgentProposalRegenerationOutcome.InvalidContext => AgentProposalRegenerationFailureReason.InvalidContext,
            AgentProposalRegenerationOutcome.ContentSafetyBlocked => AgentProposalRegenerationFailureReason.ContentSafetyBlocked,
            AgentProposalRegenerationOutcome.PolicyFailure => AgentProposalRegenerationFailureReason.PolicyFailure,

            // A Valid verdict reaching the failure branch with a Regenerated/Unknown outcome means a degraded result (a
            // success outcome with no content-bearing version, or a garbage sentinel); both fail closed to the adapter generic.
            _ => AgentProposalRegenerationFailureReason.AdapterFailure,
        };
    }

    // The internal carrier of one computed decision: the status, the success version (null on failure), the failure reason
    // (Unknown on success), and the safe regeneration evidence. Lets Evaluate and Decide share the exact same computation.
    private readonly record struct RegenerationDecision(
        AgentInteractionStatus Status,
        AgentGeneratedVersion? Version,
        AgentProposalRegenerationFailureReason Reason,
        AgentProposedReplyRegenerationEvidence Evidence);
}
