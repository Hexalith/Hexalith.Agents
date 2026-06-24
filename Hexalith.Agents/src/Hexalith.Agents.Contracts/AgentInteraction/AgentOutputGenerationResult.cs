using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The outcome discriminator of one server-assembled generation attempt (AC2, AC3, AC4; AD-3, AD-9). The orchestrator
/// classifies the provider invocation + content-safety gate into exactly one of these; the pure policy maps it to the
/// terminal event(s) + status. <see cref="Succeeded"/> means the provider produced content AND it passed Content Safety
/// Policy; every other value is a fail-closed class.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete outcome; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel (treated as a generation failure). The values mirror
/// <see cref="AgentOutputGenerationFailureReason"/> (plus <see cref="Succeeded"/>) so the policy mapping is total.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentGenerationOutcome
{
    /// <summary>Not-an-outcome sentinel — treated as a generation failure (fail closed).</summary>
    Unknown = 0,

    /// <summary>The provider produced content and it passed Content Safety Policy — the only success outcome.</summary>
    Succeeded,

    /// <summary>The provider invocation exceeded its bounded request timeout.</summary>
    ProviderTimeout,

    /// <summary>The selected Provider/model is disabled or not text-generation capable.</summary>
    ProviderDisabled,

    /// <summary>The Provider/model entry could not be read (missing, not-found, or read failure).</summary>
    ProviderUnavailable,

    /// <summary>The provider adapter failed (threw or returned a fail-closed adapter outcome).</summary>
    AdapterFailure,

    /// <summary>The Source Conversation context could not be re-read to build the model input.</summary>
    InvalidContext,

    /// <summary>The provider returned a generation error outcome.</summary>
    GenerationError,

    /// <summary>Generated content was blocked by Content Safety Policy (maps to the <see cref="AgentInteractionStatus.SafetyFailed"/> decision).</summary>
    ContentSafetyBlocked,

    /// <summary>The effective Content Safety Policy could not be loaded/evaluated — fail closed rather than skip the gate.</summary>
    PolicyFailure,
}

/// <summary>
/// The server-assembled input to the pure generation decision (AC2, AC3, AC4; AD-3). The orchestration assembles this
/// from a trusted conversation re-read, the catalog budget/timeout read, the provider invocation, and the content-safety
/// gate, then puts it on <see cref="Commands.GenerateAgentOutput"/>; the pure aggregate decides on it and never reads any
/// dependency itself (AD-3). It mirrors <see cref="AgentInteractionContextMeasurement"/> as the server→aggregate carrier.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sensitive content (AD-14):</b> <see cref="GeneratedContent"/> is conversation-derived content of the same class as
/// the caller <c>Prompt</c>. It is carried here ONLY as the transport into the aggregate so the durable success event can
/// become its sole home — exactly as the prompt is carried on the request command. The orchestrator populates it ONLY for
/// a <see cref="AgentGenerationOutcome.Succeeded"/> outcome; for every failure outcome (including a safety block) it is
/// <see langword="null"/>, so unsafe/failed content never reaches the aggregate and no approvable version is ever built
/// (AD-5, AD-14).
/// </para>
/// <para>
/// Every other member is a safe id/numeric. <see cref="AttemptId"/> is deterministic so a retried generation dedupes
/// (AD-13). When the outcome is a pre-invocation failure (e.g. provider disabled / invalid context) the token counts are
/// <c>0</c>.
/// </para>
/// </remarks>
/// <param name="Outcome">The server-assembled outcome classification the aggregate decides on.</param>
/// <param name="AttemptId">The deterministic generation attempt identifier (reused across retries — AD-13).</param>
/// <param name="ProviderId">The safe provider identifier the attempt targeted (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The safe model identifier the attempt targeted (a reference, not a secret — AD-9).</param>
/// <param name="ProviderCapabilityVersion">The provider capability version backing the attempt.</param>
/// <param name="ContentSafetyPolicyVersion">The Content Safety Policy version evaluated against the content.</param>
/// <param name="GeneratedContent">The generated content (sensitive — non-null ONLY on <see cref="AgentGenerationOutcome.Succeeded"/>; AD-14).</param>
/// <param name="PromptTokenCount">The prompt/input token usage (<c>0</c> when the provider produced none).</param>
/// <param name="OutputTokenCount">The generated-output token usage (<c>0</c> when the provider produced none).</param>
public record AgentOutputGenerationResult(
    AgentGenerationOutcome Outcome,
    string AttemptId,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int ContentSafetyPolicyVersion,
    string? GeneratedContent,
    int PromptTokenCount,
    int OutputTokenCount);
