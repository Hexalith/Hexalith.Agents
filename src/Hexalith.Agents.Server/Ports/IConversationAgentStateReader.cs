using Hexalith.Agents.ConversationAgentState;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Exact authoritative EventStore read; absence never stands in for a projection.</summary>
public interface IConversationAgentStateReader
{
    /// <summary>Reads the exact owner facts or fails closed.</summary>
    Task<ConversationAgentStateState?> ReadAsync(string tenantId, string conversationId, CancellationToken ct);
}
