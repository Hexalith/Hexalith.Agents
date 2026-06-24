using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// How a recorded <see cref="AgentGeneratedVersion"/> was produced (AC1, AC2, AC4; AD-5 append-only version history).
/// Story 2.4 records the first-pass <see cref="Generated"/> kind; Story 3.3 appends <see cref="Edited"/> for an
/// authorized Approver edit; Story 3.4 appends <see cref="Regenerated"/> for an authorized Approver regeneration so the
/// version-history surface labels a regenerated version distinctly from a generated or edited one.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete kind; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel. Ordinals are append-only — never reorder/renumber (asserted by the contract tests).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentGenerationKind
{
    /// <summary>Not-a-kind sentinel — an absent/unrecognized kind never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>A first-pass generated version produced by the provider invocation (Story 2.4).</summary>
    Generated,

    /// <summary>An immutable version produced by an authorized Approver edit of a prior version (Story 3.3; AC1; FR-14, FR-15).</summary>
    Edited,

    /// <summary>An immutable version produced by an authorized Approver regeneration — a fresh provider generation from the same Source Conversation, appended without losing prior versions (Story 3.4; AC2; FR-14, FR-16).</summary>
    Regenerated,
}
