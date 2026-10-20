using Hexalith.EventStore.Contracts.Events;
namespace Hexalith.Agents.Contracts.ConversationAgentState.Events;

/// <summary>Append-only directory fact.</summary>
/// <param name="LeaseId">The LeaseId binding.</param>
/// <param name="Revision">The Revision binding.</param>
public sealed record ConversationEffectLeaseCancelled(string LeaseId, long Revision) : IEventPayload;
