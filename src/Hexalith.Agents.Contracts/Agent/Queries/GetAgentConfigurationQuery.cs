namespace Hexalith.Agents.Contracts.Agent.Queries;

/// <summary>
/// Requests the safe administrative configuration view of the Agent (<c>hexa</c>) identified by the request's
/// aggregate scope (AC1; FR-1). Returns the authoritative <c>AgentSetupView</c> — identity metadata, lifecycle,
/// configuration/instructions versions, activation blockers, plus projection version, freshness, and truth stage —
/// and deliberately never exposes the raw Agent Instructions text (AD-14).
/// </summary>
/// <param name="ExpectedConfigurationVersion">
/// The configuration version the caller is waiting to see, or <see langword="null"/> when the caller simply wants
/// current projected truth. Supplying it is what lets the answer separate <c>AuthoritativePending</c> from
/// <c>ProjectionConfirmed</c> instead of presenting a lagging projection as confirmed.
/// </param>
public record GetAgentConfigurationQuery(int? ExpectedConfigurationVersion = null)
{
    /// <summary>The kebab-case EventStore domain this query is routed to.</summary>
    public const string Domain = "agent";

    /// <summary>The query type discriminator served by the live setup query handler.</summary>
    public const string QueryType = nameof(GetAgentConfigurationQuery);
}
