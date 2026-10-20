using Hexalith.EventStore.Contracts.Events;
namespace Hexalith.Agents.Contracts.ConversationAgentState.Events;

/// <summary>Append-only directory fact.</summary>
/// <param name="Permit">The Permit binding.</param>
/// <param name="PermitHighWater">The PermitHighWater binding.</param>
/// <param name="OutboxId">The OutboxId binding.</param>
/// <param name="Revision">The Revision binding.</param>
public sealed record InteractionCreationPermitted(InteractionPermit Permit, long PermitHighWater, string OutboxId, long Revision) : IEventPayload;
