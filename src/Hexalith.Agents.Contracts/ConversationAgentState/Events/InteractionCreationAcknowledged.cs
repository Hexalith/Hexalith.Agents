using Hexalith.EventStore.Contracts.Events;
namespace Hexalith.Agents.Contracts.ConversationAgentState.Events;

/// <summary>Append-only directory fact.</summary>
/// <param name="AgentInteractionId">The AgentInteractionId binding.</param>
/// <param name="PermitId">The PermitId binding.</param>
/// <param name="OutboxId">The OutboxId binding.</param>
/// <param name="InteractionCreationRevision">The InteractionCreationRevision binding.</param>
/// <param name="Revision">The Revision binding.</param>
public sealed record InteractionCreationAcknowledged(string AgentInteractionId, string PermitId, string OutboxId, long InteractionCreationRevision, long Revision) : IEventPayload;
