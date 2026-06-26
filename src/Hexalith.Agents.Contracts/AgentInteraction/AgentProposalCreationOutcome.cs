using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The outcome discriminator of one server-assembled Proposed-Agent-Reply creation attempt (AC1, AC3, AC4; AD-3, AD-5).
/// The orchestrator classifies the selected-version read + proposal assembly into exactly one of these; the pure policy
/// maps it to the terminal event + status. <see cref="Created"/> means the selected generated version was read and the
/// proposal was assembled; every other value is a fail-closed class.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete outcome; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel (treated as a creation failure). The values mirror <see cref="AgentProposalCreationFailureReason"/>
/// (plus <see cref="Created"/>) so the policy mapping is total. Mirrors <see cref="AgentResponsePostingOutcome"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposalCreationOutcome
{
    /// <summary>Not-an-outcome sentinel — treated as a creation failure (fail closed).</summary>
    Unknown = 0,

    /// <summary>The selected generated version was read and the proposal was assembled — the only success outcome.</summary>
    Created,

    /// <summary>The selected generated version could not be read (missing, not-found, or read failure) — fail closed.</summary>
    GeneratedVersionUnavailable,

    /// <summary>The creation adapter failed (threw or returned a fail-closed adapter outcome), including the all-deferred default graph.</summary>
    AdapterFailure,
}
