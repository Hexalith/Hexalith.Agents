using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The outcome discriminator of one server-assembled Proposed-Agent-Reply regeneration attempt (AC1, AC2, AC3, AC4; AD-3,
/// AD-5, AD-9). The regeneration orchestrator classifies the authorized provider invocation + content-safety gate into
/// exactly one of these; the pure policy maps it to the terminal event + status. <see cref="Regenerated"/> means the
/// provider produced fresh content AND it passed Content Safety Policy (the new immutable regenerated version is carried on
/// the result); every other value is a fail-closed class.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete outcome; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel (treated as a regeneration failure). The fail-closed values mirror the Story 2.4 generation outcome
/// surface (<see cref="AgentGenerationOutcome"/>) — provider/timeout/safety/policy — because regeneration re-invokes the
/// provider, while authorization fail-closed rides the result's verdict (mirroring the Story 3.3 edit path). Mirrors
/// <see cref="AgentProposalEditOutcome"/> in role.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposalRegenerationOutcome
{
    /// <summary>Not-an-outcome sentinel — treated as a regeneration failure (fail closed).</summary>
    Unknown = 0,

    /// <summary>The provider produced fresh content and it passed Content Safety Policy — the new immutable regenerated version is carried on the result; the only success outcome.</summary>
    Regenerated,

    /// <summary>The provider invocation exceeded its bounded request timeout.</summary>
    ProviderTimeout,

    /// <summary>The selected Provider/model is disabled or not text-generation capable — regeneration must not proceed.</summary>
    ProviderDisabled,

    /// <summary>The Provider/model entry could not be read (missing, not-found, or read failure) — fail closed.</summary>
    ProviderUnavailable,

    /// <summary>The provider adapter failed (it threw or returned a fail-closed adapter outcome), including the all-deferred default graph — no provider error text is exposed (AD-14).</summary>
    AdapterFailure,

    /// <summary>The Source Conversation context could not be re-read fresh enough to build the model input — fail closed.</summary>
    InvalidContext,

    /// <summary>Regenerated content was blocked by Content Safety Policy (paired with the <see cref="AgentInteractionStatus.ProposalRegenerationFailed"/> decision; AD-5, AD-14).</summary>
    ContentSafetyBlocked,

    /// <summary>The effective Content Safety Policy could not be loaded/evaluated — fail closed rather than skip the safety gate.</summary>
    PolicyFailure,
}
