namespace Hexalith.Agents.Contracts.Agent.Queries;

/// <summary>
/// Requests the safe public status view of the Agent (<c>hexa</c>) identified by the request's aggregate scope
/// (AC3; FR-3). Returns lifecycle state, configuration version, instruction presence/validity + version, and
/// current activation blockers — never the raw Agent Instructions text (AD-14).
/// </summary>
/// <remarks>
/// Binding this query to the EventStore SDK <c>IDomainQueryHandler</c>/<c>IReadModelStore</c> DAPR read path is
/// deferred to the dedicated Agents read-model story (mirroring Story 1.2). The stable query/view contracts and
/// the pure inspection logic land here.
/// </remarks>
public record GetAgentStatusQuery();
