namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, audit-ready evidence of one generation attempt, recorded on <see cref="Events.AgentOutputGenerationFailed"/>
/// so an authorized administrator can see WHICH attempt failed and its provider/model identity and token usage without
/// any unsafe content (AC3; FR-24; AD-9, AD-14). It mirrors <see cref="AgentInteractionContextEvidence"/>: safe
/// numerics/ids only.
/// </summary>
/// <remarks>
/// Carries ONLY safe ids/numerics — deliberately NEVER the generated/failed content, a raw provider payload, a
/// provider-specific error string, a stack trace, or a secret (AD-9, AD-14). On a failure that occurred before the
/// provider produced usage (e.g. an invalid-context or provider-disabled failure) the token counts are <c>0</c>.
/// </remarks>
/// <param name="AttemptId">The deterministic generation attempt identifier (reused across retries — AD-13).</param>
/// <param name="ProviderId">The safe provider identifier the attempt targeted (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The safe model identifier the attempt targeted (a reference, not a secret — AD-9).</param>
/// <param name="ProviderCapabilityVersion">The provider capability version backing the attempt (<c>0</c> when unknown).</param>
/// <param name="PromptTokenCount">The prompt/input token usage, if the provider produced it (<c>0</c> otherwise).</param>
/// <param name="OutputTokenCount">The generated-output token usage, if the provider produced it (<c>0</c> otherwise).</param>
public record AgentGenerationAttemptEvidence(
    string AttemptId,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int PromptTokenCount,
    int OutputTokenCount);
