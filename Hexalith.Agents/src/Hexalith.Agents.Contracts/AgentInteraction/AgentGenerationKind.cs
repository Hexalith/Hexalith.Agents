using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// How a recorded <see cref="AgentGeneratedVersion"/> was produced (AC2, AC4; AD-5 append-only version history). V1
/// records only the first-pass <see cref="Generated"/> kind; edit/regenerate are deferred to Epic 3 and are reserved
/// here additively so the version-history surface does not need a contract change when they arrive.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete kind; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel. Do NOT add the deferred <c>Edited</c>/<c>Regenerated</c> kinds in this story (Epic 3 owns them).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentGenerationKind
{
    /// <summary>Not-a-kind sentinel — an absent/unrecognized kind never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>A first-pass generated version produced by the provider invocation (V1).</summary>
    Generated,
}
