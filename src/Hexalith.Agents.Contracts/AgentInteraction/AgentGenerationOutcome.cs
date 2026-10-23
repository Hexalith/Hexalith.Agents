using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

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
[JsonConverter(typeof(UnknownFallbackEnumConverter<AgentGenerationOutcome>))]
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
