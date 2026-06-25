namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>Requests support-safe posting evidence for an AgentInteraction.</summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
public sealed record GetAgentPostingEvidenceQuery(string AgentInteractionId)
{
    /// <summary>The EventStore query domain.</summary>
    public const string Domain = "agent-interaction";

    /// <summary>The query type discriminator.</summary>
    public const string QueryType = "get-agent-posting-evidence";
}
