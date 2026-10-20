using Hexalith.Agents.Contracts.ConversationAgentState;
namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>The only new interaction creation path; consumes an exact directory creation outbox.</summary>
/// <param name="Permit">The Permit binding.</param>
/// <param name="PermitRevision">The PermitRevision binding.</param>
/// <param name="ExpectedRevision">The ExpectedRevision binding.</param>
public sealed record CreatePermittedAgentInteraction(InteractionPermit Permit, long PermitRevision, long ExpectedRevision);
