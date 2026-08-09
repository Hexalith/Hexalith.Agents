namespace Hexalith.Agents.Contracts.Agent.Queries;

/// <summary>
/// Requests the safe public status view of the Agent (<c>hexa</c>) identified by the request's aggregate scope
/// (AC3; FR-3). Returns the same authoritative <c>AgentSetupView</c> the configuration query serves — lifecycle
/// state, configuration version, instruction presence/validity + version, activation blockers, projection version,
/// freshness, and truth stage — never the raw Agent Instructions text (AD-14).
/// </summary>
/// <param name="ExpectedConfigurationVersion">
/// The configuration version the caller is waiting to see, or <see langword="null"/> when the caller simply wants
/// current projected truth. Supplying it is what lets the answer separate <c>AuthoritativePending</c> from
/// <c>ProjectionConfirmed</c> instead of presenting a lagging projection as confirmed.
/// </param>
public record GetAgentStatusQuery(int? ExpectedConfigurationVersion = null)
{
    /// <summary>The kebab-case EventStore domain this query is routed to.</summary>
    public const string Domain = "agent";

    /// <summary>The query type discriminator served by the live setup query handler.</summary>
    public const string QueryType = nameof(GetAgentStatusQuery);
}
