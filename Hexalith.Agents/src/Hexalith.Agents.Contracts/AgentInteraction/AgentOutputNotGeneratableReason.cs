using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> a generate-output command could not be evaluated at all (AD-12). Distinct
/// from a recorded generation-failed <em>decision</em>: a not-generatable command produces no state change (mirroring
/// the context build's not-buildable rejection), whereas a generation-failed decision is a successfully-recorded
/// fail-closed outcome. Carried only on <see cref="Events.Rejections.AgentOutputNotGeneratableRejection"/>.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel: an absent/unrecognized reason is treated as "not a
/// concrete reason". Serialized by name so an absent value never resolves to a concrete classification.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentOutputNotGeneratableReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The interaction has no recorded request yet, so there is no call to generate output for.</summary>
    InteractionNotRequested,

    /// <summary>The interaction has not built its Conversation context (status is not <c>ContextReady</c>); generation must never run before context is ready.</summary>
    ContextNotReady,
}
