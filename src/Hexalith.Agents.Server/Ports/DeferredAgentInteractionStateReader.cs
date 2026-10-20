using Hexalith.Agents.AgentInteraction;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Closed default; live directory migration/read binding has not been qualified.</summary>
public sealed class DeferredAgentInteractionStateReader : IAgentInteractionStateReader
{
    /// <inheritdoc />
    public Task<AgentInteractionState?> ReadAsync(string tenantId, string interactionId, CancellationToken ct) => throw new NotSupportedException("DependencyNotAvailable(EXT-HOST-1)");
}
