using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.ConversationAgentState;

/// <summary>Closed directory vocabulary.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ConversationEffectLeaseStatus>))]
public enum ConversationEffectLeaseStatus
{
    /// <summary>Unknown classification.</summary>
    Unknown = 0,
    /// <summary>Reserved classification.</summary>
    Reserved = 1,
    /// <summary>CommittedToEffect classification.</summary>
    CommittedToEffect = 2,
    /// <summary>Settled classification.</summary>
    Settled = 3,
    /// <summary>CancelledBeforeCommit classification.</summary>
    CancelledBeforeCommit = 4,
}
