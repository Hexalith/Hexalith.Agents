namespace Hexalith.Agents.Server.Ports;

/// <summary>Reads rehydrated AgentInteraction state for support-safe audit query handlers.</summary>
public interface IAgentInteractionAuditStateReader
{
    /// <summary>Reads one AgentInteraction and returns its authoritative tenant/stream provenance for exact scope verification.</summary>
    Task<AgentInteractionAuditStateReadResult> ReadAsync(string tenantId, string agentInteractionId, CancellationToken ct);
}
