using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> Conversation context could not be built within safe bounds (AC3; AD-11,
/// AD-12). Recorded on <see cref="Events.AgentInteractionContextBlocked"/> as fail-closed Audit Evidence so an
/// administrator can distinguish context-policy failures (FR-25) without any raw content.
/// </summary>
/// <remarks>
/// <see cref="ContextUnavailable"/> covers both the unauthorized and the unavailable load outcomes (coarse, so no
/// cross-tenant disclosure — AC1/AD-12). Serialized by name so an absent value never resolves to a concrete reason;
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentInteractionContextBlockReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The Source Conversation could not be loaded (unauthorized or unavailable) — coarse, no cross-tenant disclosure (AC1).</summary>
    ContextUnavailable,

    /// <summary>The Source Conversation could not be loaded fresh enough (stale) — context cannot be trusted (AC3).</summary>
    ContextNotFresh,

    /// <summary>The full context exceeds the model context budget and no approved bounded-context behavior is configured (AC3 — never a silent truncation).</summary>
    ExceedsModelBudget,

    /// <summary>The Provider/model context budget could not be determined or trusted (catalog entry missing/disabled/not text-capable, or invalid limits) — fail closed.</summary>
    ModelBudgetUnavailable,
}
