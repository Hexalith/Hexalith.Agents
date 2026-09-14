using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The truth stage an Agent setup observation has reached (Story 5.2 AC2). A caller must never collapse these
/// stages: only <see cref="ProjectionConfirmed"/> is evidence that no further change is awaited for the accepted
/// configuration version.
/// </summary>
/// <remarks>
/// <para>
/// The stages are deliberately ordered by strength of evidence. <see cref="Submitted"/> means the command was
/// accepted by the gateway and nothing more; <see cref="AuthoritativePending"/> means the accepted change is not
/// yet visible in the projected read model; <see cref="ProjectionConfirmed"/> means no projected version is still
/// outstanding for the accepted configuration version. None of the stages says anything about callability —
/// lifecycle <c>Active</c> is a lifecycle flag, not a callability claim.
/// </para>
/// <para>
/// <see cref="ProjectionConfirmed"/> is reached two ways. A read whose projected configuration version has caught
/// up with the accepted one confirms an applied write. A no-op receipt also reports it directly: the command
/// appended no event, so the configuration version it reports is the unchanged current one and there is no later
/// version for a projection to reach. Polling in that case would wait for a version that may never be emitted, so
/// the no-op terminates instead — which means this stage is not, on its own, proof that a read was performed.
/// </para>
/// </remarks>
[JsonConverter(typeof(UnknownFallbackEnumConverter<AgentSetupTruthState>))]
public enum AgentSetupTruthState
{
    /// <summary>No truth stage has been established (the fail-closed default).</summary>
    Unknown = 0,

    /// <summary>The command was accepted for processing; no durable evidence exists yet.</summary>
    Submitted = 1,

    /// <summary>The Agent stream holds the accepted change, but the projected read model has not caught up.</summary>
    AuthoritativePending = 2,

    /// <summary>
    /// No projected version is still outstanding: either the projected read model reflects the accepted
    /// configuration version, or the command was a no-op that appended nothing to reach.
    /// </summary>
    ProjectionConfirmed = 3,
}
