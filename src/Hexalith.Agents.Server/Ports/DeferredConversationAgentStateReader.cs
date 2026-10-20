using Hexalith.Agents.ConversationAgentState;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Closed default; live directory migration/read binding has not been qualified.</summary>
public sealed class DeferredConversationAgentStateReader : IConversationAgentStateReader
{
    /// <inheritdoc />
    public Task<ConversationAgentStateState?> ReadAsync(string tenantId, string conversationId, CancellationToken ct) => throw new NotSupportedException("DependencyNotAvailable(EXT-HOST-1)");
}
