using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> an invocation gate command could not be evaluated at all (AC4; AD-12).
/// Distinct from a recorded denied/blocked <em>decision</em>: a not-evaluable command produces no state change
/// (mirroring the structural request rejections), whereas a denied/blocked decision is a successfully-recorded
/// negative gate outcome. Carried only on <see cref="Events.Rejections.AgentInteractionGateNotEvaluableRejection"/>.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel: an absent/unrecognized reason is treated as "not a
/// concrete reason". Serialized by name so an absent value never resolves to a concrete classification.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentInteractionGateNotEvaluableReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The interaction has no recorded request yet, so there is nothing to gate.</summary>
    InteractionNotRequested,

    /// <summary>No verdicts were supplied, so the gate cannot reach a decision.</summary>
    NoVerdictsProvided,
}
