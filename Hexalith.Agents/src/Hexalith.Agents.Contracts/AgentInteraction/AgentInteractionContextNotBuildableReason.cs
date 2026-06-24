using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> a context-build command could not be evaluated at all (AD-12). Distinct
/// from a recorded context-blocked <em>decision</em>: a not-buildable command produces no state change (mirroring the
/// gate's not-evaluable rejection), whereas a context-blocked decision is a successfully-recorded fail-closed outcome.
/// Carried only on <see cref="Events.Rejections.AgentInteractionContextNotBuildableRejection"/>.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel: an absent/unrecognized reason is treated as "not a
/// concrete reason". Serialized by name so an absent value never resolves to a concrete classification.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentInteractionContextNotBuildableReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The interaction has no recorded request yet, so there is no context to build.</summary>
    InteractionNotRequested,

    /// <summary>The interaction has not passed the invocation gate (status is not <c>Authorized</c>); context must never be built on a call that failed or has not cleared the gate.</summary>
    InteractionNotAuthorized,
}
