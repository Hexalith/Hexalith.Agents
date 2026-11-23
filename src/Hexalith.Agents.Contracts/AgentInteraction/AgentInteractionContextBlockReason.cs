using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> Conversation context could not be built within safe bounds (AC3; AD-11,
/// AD-12). Recorded on <see cref="Events.AgentInteractionContextBlocked"/> as fail-closed Audit Evidence so an
/// administrator can distinguish context-policy failures (FR-25) without any raw content.
/// </summary>
/// <remarks>
/// The reasons carry no details about a Conversation or its contents. <see cref="Unknown"/> (ordinal 0)
/// is the sentinel for absent or unrecognized values.
/// </remarks>
[JsonConverter(typeof(UnknownFallbackEnumConverter<AgentInteractionContextBlockReason>))]
public enum AgentInteractionContextBlockReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>Access was denied or no more specific load outcome was established. Also used by historical records for read failures.</summary>
    ContextUnavailable,

    /// <summary>The Source Conversation could not be loaded fresh enough (stale) — context cannot be trusted (AC3).</summary>
    ContextNotFresh,

    /// <summary>The full context exceeds the model context budget and no approved bounded-context behavior is configured (AC3 — never a silent truncation).</summary>
    ExceedsModelBudget,

    /// <summary>The Provider/model context budget could not be determined or trusted (catalog entry missing/disabled/not text-capable, or invalid limits) — fail closed.</summary>
    ModelBudgetUnavailable,

    /// <summary>The Source Conversation read failed or returned unusable data.</summary>
    ContextReadUnavailable,
}
