using Hexalith.Agents.AgentInteraction;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Exact authoritative EventStore read; absence never stands in for a projection.</summary>
public interface IAgentInteractionStateReader
{
    /// <summary>Reads the exact owner facts or fails closed.</summary>
    Task<AgentInteractionState?> ReadAsync(string tenantId, string interactionId, CancellationToken ct);
}
