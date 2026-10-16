using Hexalith.Agents.AgentInteraction;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Fail-closed audit state with the authoritative stream provenance, distinct from the requested scope.</summary>
/// <param name="CanLoad">Whether the state dependency could be loaded.</param>
/// <param name="IsFresh">Whether the read state is fresh enough to render as available.</param>
/// <param name="State">The rehydrated state, when available.</param>
public sealed record AgentInteractionAuditStateReadResult(bool CanLoad, bool IsFresh, AgentInteractionState? State)
{
    /// <summary>Gets the tenant from the authoritative source identity; omitted provenance grants no read authority.</summary>
    public string? SourceTenantId { get; init; }

    /// <summary>Gets the aggregate from the authoritative source identity, including for an absent stream.</summary>
    public string? SourceAggregateId { get; init; }

    /// <summary>A dependency-unavailable result.</summary>
    public static AgentInteractionAuditStateReadResult Unavailable { get; } = new(false, false, null);

    /// <summary>Preserves legacy construction; an unscoped result cannot authorize rendering.</summary>
    public static AgentInteractionAuditStateReadResult Available(AgentInteractionState? state, bool isFresh = true)
        => new(true, isFresh, state);

    /// <summary>Creates a result whose provenance must correspond to the query before any state is inspected.</summary>
    public static AgentInteractionAuditStateReadResult Available(string sourceTenantId, string sourceAggregateId,
        AgentInteractionState? state, bool isFresh = true)
        => new(true, isFresh, state)
        {
            SourceTenantId = sourceTenantId,
            SourceAggregateId = sourceAggregateId,
        };
}
