using Hexalith.Agents.AgentInteraction;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Reads rehydrated AgentInteraction state for support-safe audit query handlers.</summary>
public interface IAgentInteractionAuditStateReader
{
    /// <summary>Reads one AgentInteraction within the supplied tenant scope.</summary>
    Task<AgentInteractionAuditStateReadResult> ReadAsync(string tenantId, string agentInteractionId, CancellationToken ct);
}

/// <summary>Fail-closed read result for AgentInteraction audit state.</summary>
/// <param name="CanLoad">Whether the state dependency could be loaded.</param>
/// <param name="IsFresh">Whether the read state is fresh enough to render as available.</param>
/// <param name="State">The rehydrated state, when available.</param>
public sealed record AgentInteractionAuditStateReadResult(bool CanLoad, bool IsFresh, AgentInteractionState? State)
{
    /// <summary>A dependency-unavailable result.</summary>
    public static AgentInteractionAuditStateReadResult Unavailable { get; } = new(false, false, null);

    /// <summary>Creates an available read result.</summary>
    public static AgentInteractionAuditStateReadResult Available(AgentInteractionState? state, bool isFresh = true)
        => new(true, isFresh, state);
}
