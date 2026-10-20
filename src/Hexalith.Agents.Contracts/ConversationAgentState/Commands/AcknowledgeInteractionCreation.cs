
namespace Hexalith.Agents.Contracts.ConversationAgentState.Commands;

/// <summary>Trusted directory command; expected revision serializes with Closing.</summary>
/// <param name="AgentInteractionId">The AgentInteractionId binding.</param>
/// <param name="PermitId">The PermitId binding.</param>
/// <param name="OutboxId">The OutboxId binding.</param>
/// <param name="InteractionCreationRevision">The InteractionCreationRevision binding.</param>
/// <param name="ExpectedRevision">The ExpectedRevision binding.</param>
public sealed record AcknowledgeInteractionCreation(string AgentInteractionId, string PermitId, string OutboxId, long InteractionCreationRevision, long ExpectedRevision);
