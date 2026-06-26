using System;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, dependency-free Agent-output generation decision shared by the <see cref="AgentInteractionAggregate"/>
/// generation handler and the Server generation orchestration so the two can never drift (AC2, AC3, AC4; AD-3, AD-5).
/// Centralizing the outcome → decision math mirrors <see cref="AgentInteractionContextPolicy"/>: the aggregate calls
/// <see cref="Evaluate"/> to emit the outcome event; the orchestrator calls <see cref="Decide"/> to return the same
/// decided status it dispatched.
/// </summary>
/// <remarks>
/// No method here reads I/O, time, secrets, or any sibling aggregate (AD-3). It operates only on the safe
/// <see cref="AgentOutputGenerationResult"/>. Fail-closed by construction: every non-<see cref="AgentGenerationOutcome.Succeeded"/>
/// outcome maps to a recorded <see cref="AgentOutputGenerationFailed"/> with safe attempt evidence and emits NO
/// <see cref="AgentGeneratedVersion"/>, so there is structurally nothing approvable on a failure (AD-5). The generated
/// content is placed ONLY on the success event's version (AD-14); on a safety/any failure it is never carried.
/// </remarks>
internal static class AgentOutputGenerationPolicy
{
    /// <summary>
    /// Evaluates the supplied generation result into the durable outcome event (AC2, AC3). <see cref="AgentGenerationOutcome.Succeeded"/>
    /// → <see cref="AgentOutputGenerated"/> (status <c>Generated</c>); <see cref="AgentGenerationOutcome.ContentSafetyBlocked"/>
    /// → <see cref="AgentOutputGenerationFailed"/> (decision <c>SafetyFailed</c>); every other outcome →
    /// <see cref="AgentOutputGenerationFailed"/> (decision <c>GenerationFailed</c>) — the fail-closed Audit Evidence (FR-24).
    /// </summary>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id) to stamp on the emitted event.</param>
    /// <param name="result">The server-assembled generation result.</param>
    /// <returns>A success domain result carrying exactly one outcome event.</returns>
    internal static DomainResult Evaluate(string interactionId, AgentOutputGenerationResult result)
    {
        GenerationDecision decision = Compute(result);
        return decision.Status == AgentInteractionStatus.Generated
            ? DomainResult.Success([new AgentOutputGenerated(interactionId, decision.Version!)])
            : DomainResult.Success([new AgentOutputGenerationFailed(interactionId, decision.Status, decision.Reason, decision.Evidence)]);
    }

    /// <summary>
    /// Computes the decided generation status from the supplied result using the same rule as <see cref="Evaluate"/>
    /// (<c>Succeeded</c> → <see cref="AgentInteractionStatus.Generated"/>; <c>ContentSafetyBlocked</c> →
    /// <see cref="AgentInteractionStatus.SafetyFailed"/>; otherwise <see cref="AgentInteractionStatus.GenerationFailed"/>).
    /// The orchestrator returns this so its reported status cannot drift from the aggregate's recorded decision.
    /// </summary>
    /// <param name="result">The server-assembled generation result.</param>
    /// <returns>The decided terminal generation status.</returns>
    internal static AgentInteractionStatus Decide(AgentOutputGenerationResult result)
        => Compute(result).Status;

    // The deterministic generation decision. Succeeded → build the approvable version (its VersionId derived from the
    // deterministic AttemptId so a retry reuses the same identity — AD-13). Every other outcome → a fail-closed decision
    // with safe attempt evidence and NO version (nothing approvable on failure — AD-5). ContentSafetyBlocked is the only
    // failure mapped to SafetyFailed; all others map to GenerationFailed.
    private static GenerationDecision Compute(AgentOutputGenerationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Outcome == AgentGenerationOutcome.Succeeded)
        {
            return new GenerationDecision(
                AgentInteractionStatus.Generated,
                BuildVersion(result),
                AgentOutputGenerationFailureReason.Unknown,
                AttemptEvidence(result));
        }

        AgentInteractionStatus decision = result.Outcome == AgentGenerationOutcome.ContentSafetyBlocked
            ? AgentInteractionStatus.SafetyFailed
            : AgentInteractionStatus.GenerationFailed;

        return new GenerationDecision(decision, Version: null, MapFailureReason(result.Outcome), AttemptEvidence(result));
    }

    // Builds the approvable version from the successful result, including the generated content (its sole durable home is
    // the success event — AD-14). VersionId is a pure deterministic transform of the AttemptId so retries never duplicate
    // a version (AD-13). GeneratedContent is non-null on a success outcome by construction; the null-coalesce is defensive.
    private static AgentGeneratedVersion BuildVersion(AgentOutputGenerationResult r)
        => new(
            VersionId: DeriveVersionId(r.AttemptId),
            r.AttemptId,
            AgentGenerationKind.Generated,
            r.GeneratedContent ?? string.Empty,
            r.ProviderId,
            r.ModelId,
            r.ProviderCapabilityVersion,
            r.ContentSafetyPolicyVersion,
            r.PromptTokenCount,
            r.OutputTokenCount);

    // Safe attempt evidence for a failure record (and carried alongside a success decision for symmetry). Ids + token
    // counts only — never the generated/failed content or a provider payload/error (AD-9, AD-14).
    private static AgentGenerationAttemptEvidence AttemptEvidence(AgentOutputGenerationResult r)
        => new(
            r.AttemptId,
            r.ProviderId,
            r.ModelId,
            r.ProviderCapabilityVersion,
            r.PromptTokenCount,
            r.OutputTokenCount);

    // The deterministic version identity derived purely from the attempt id (AD-13) — no Guid/random/time.
    private static string DeriveVersionId(string attemptId) => $"version-{attemptId}";

    // Maps a non-success outcome to its safe failure reason. Unknown/unmapped fails closed to a content-free generic.
    private static AgentOutputGenerationFailureReason MapFailureReason(AgentGenerationOutcome outcome) => outcome switch
    {
        AgentGenerationOutcome.ProviderTimeout => AgentOutputGenerationFailureReason.ProviderTimeout,
        AgentGenerationOutcome.ProviderDisabled => AgentOutputGenerationFailureReason.ProviderDisabled,
        AgentGenerationOutcome.ProviderUnavailable => AgentOutputGenerationFailureReason.ProviderUnavailable,
        AgentGenerationOutcome.AdapterFailure => AgentOutputGenerationFailureReason.AdapterFailure,
        AgentGenerationOutcome.InvalidContext => AgentOutputGenerationFailureReason.InvalidContext,
        AgentGenerationOutcome.GenerationError => AgentOutputGenerationFailureReason.GenerationError,
        AgentGenerationOutcome.ContentSafetyBlocked => AgentOutputGenerationFailureReason.ContentSafetyBlocked,
        AgentGenerationOutcome.PolicyFailure => AgentOutputGenerationFailureReason.PolicyFailure,
        _ => AgentOutputGenerationFailureReason.GenerationError,
    };

    // The internal carrier of one computed decision: the status, the success version (null on failure), the failure reason
    // (Unknown on success), and the safe attempt evidence. Lets Evaluate and Decide share the exact same computation.
    private readonly record struct GenerationDecision(
        AgentInteractionStatus Status,
        AgentGeneratedVersion? Version,
        AgentOutputGenerationFailureReason Reason,
        AgentGenerationAttemptEvidence Evidence);
}
