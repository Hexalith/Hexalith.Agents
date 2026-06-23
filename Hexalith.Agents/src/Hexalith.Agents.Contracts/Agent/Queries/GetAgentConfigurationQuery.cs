namespace Hexalith.Agents.Contracts.Agent.Queries;

/// <summary>
/// Requests the safe administrative configuration view of the Agent (<c>hexa</c>) identified by the request's
/// aggregate scope (AC1; FR-1). Returns the same safe <c>AgentStatusView</c> — identity metadata, lifecycle,
/// configuration/instructions versions, and activation blockers — and deliberately never exposes the raw Agent
/// Instructions text (AD-14).
/// </summary>
/// <remarks>
/// Binding this query to the EventStore SDK <c>IDomainQueryHandler</c>/<c>IReadModelStore</c> DAPR read path is
/// deferred to the dedicated Agents read-model story (mirroring Story 1.2). The stable query/view contracts and
/// the pure inspection logic land here.
/// </remarks>
public record GetAgentConfigurationQuery();
