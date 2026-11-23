using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>Records the safe result of the fresh Conversation context check for a generation or regeneration attempt.</summary>
/// <param name="LoadOutcome">The coarse outcome of the authorized timeline read.</param>
/// <param name="Mode">Whether the full context fit or the check blocked the attempt.</param>
public sealed record AgentAttemptContextEvidence(
    AgentInteractionContextLoadOutcome LoadOutcome,
    AgentInteractionContextMode Mode)
{
    /// <summary>Gets the safe reason the context check refused the attempt, when it blocked.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentInteractionContextBlockReason? BlockReason { get; init; }

    /// <summary>Gets the evaluated budget terms when a valid exact token measurement was obtained.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentAttemptContextBudgetEvidence? Budget { get; init; }
}
