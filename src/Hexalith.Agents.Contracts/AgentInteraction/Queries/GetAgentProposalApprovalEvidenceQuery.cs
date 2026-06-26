namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Requests safe approval/posting audit evidence for one AgentInteraction proposal. Live query binding is Epic 4 scope.
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
public sealed record GetAgentProposalApprovalEvidenceQuery(string AgentInteractionId)
{
    /// <summary>The EventStore query domain.</summary>
    public const string Domain = "agent-interaction";

    /// <summary>The query type discriminator.</summary>
    public const string QueryType = "get-agent-proposal-approval-evidence";
}
