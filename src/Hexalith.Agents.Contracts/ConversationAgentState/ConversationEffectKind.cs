using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.ConversationAgentState;

/// <summary>Closed directory vocabulary.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ConversationEffectKind>))]
public enum ConversationEffectKind
{
    /// <summary>Unknown classification.</summary>
    Unknown = 0,
    /// <summary>WorkflowStart classification.</summary>
    WorkflowStart = 1,
    /// <summary>ApproverResolution classification.</summary>
    ApproverResolution = 2,
    /// <summary>ContextRead classification.</summary>
    ContextRead = 3,
    /// <summary>ConversationMembership classification.</summary>
    ConversationMembership = 4,
    /// <summary>ProviderInvocation classification.</summary>
    ProviderInvocation = 5,
    /// <summary>ProposalMutation classification.</summary>
    ProposalMutation = 6,
    /// <summary>ConversationPosting classification.</summary>
    ConversationPosting = 7,
}
