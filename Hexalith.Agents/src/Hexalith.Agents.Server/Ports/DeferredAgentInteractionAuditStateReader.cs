namespace Hexalith.Agents.Server.Ports;

/// <summary>Deferred audit state reader that fails closed until the live EventStore read binding is configured.</summary>
public sealed class DeferredAgentInteractionAuditStateReader : IAgentInteractionAuditStateReader
{
    /// <inheritdoc />
    public Task<AgentInteractionAuditStateReadResult> ReadAsync(string tenantId, string agentInteractionId, CancellationToken ct)
        => Task.FromResult(AgentInteractionAuditStateReadResult.Unavailable);
}
