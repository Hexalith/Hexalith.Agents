namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>Requests the canonical audit availability state for an AgentInteraction.</summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
public sealed record GetAgentAuditAvailabilityQuery(string AgentInteractionId)
{
    /// <summary>The EventStore query domain.</summary>
    public const string Domain = "agent-interaction";

    /// <summary>The query type discriminator.</summary>
    public const string QueryType = "get-agent-audit-availability";
}
