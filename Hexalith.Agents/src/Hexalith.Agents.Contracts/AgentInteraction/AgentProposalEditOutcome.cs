using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The outcome discriminator of one server-assembled Proposed-Agent-Reply edit attempt (AC1, AC2, AC4; AD-3, AD-5).
/// The edit orchestrator classifies the authorized edit assembly into exactly one of these; the pure policy maps it to
/// the terminal event + status. <see cref="Edited"/> means an authorized edit was assembled (the new immutable edited
/// version is carried on the result); every other value is a fail-closed class.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete outcome; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel (treated as an edit failure). The values mirror <see cref="AgentProposalEditFailureReason"/> (plus
/// <see cref="Edited"/>) so the policy mapping is total. Mirrors <see cref="AgentProposalCreationOutcome"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposalEditOutcome
{
    /// <summary>Not-an-outcome sentinel — treated as an edit failure (fail closed).</summary>
    Unknown = 0,

    /// <summary>An authorized edit was assembled and the new immutable edited version is carried on the result — the only success outcome.</summary>
    Edited,

    /// <summary>The edit adapter failed (threw or returned a fail-closed adapter outcome), including the all-deferred default graph.</summary>
    AdapterFailure,
}
