namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Requests the safe public status view of the Agent Call (<c>AgentInteraction</c>) identified by the request's
/// aggregate scope (AC2; FR-8, FR-24). Returns an <see cref="AgentInteractionStatusView"/> — the safe identity
/// references, coarse status, snapshotted Response Mode, and version numbers — never the raw prompt, Conversation
/// content, or internal stream/projection identifiers (AD-14).
/// </summary>
/// <remarks>
/// Binding this query to the EventStore SDK <c>IDomainQueryHandler</c>/<c>IReadModelStore</c> DAPR read path is
/// deferred to the dedicated Agents read-model story (mirroring Story 1.2 / <c>GetAgentStatusQuery</c>). The stable
/// query/view contracts land here.
/// </remarks>
public record GetAgentInteractionStatusQuery();
