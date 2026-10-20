using Hexalith.EventStore.Contracts.Events;
namespace Hexalith.Agents.Contracts.ConversationAgentState.Events;

/// <summary>Append-only directory fact.</summary>
/// <param name="Lease">The Lease binding.</param>
/// <param name="Revision">The Revision binding.</param>
public sealed record ConversationEffectLeaseAcquired(ConversationEffectLease Lease, long Revision) : IEventPayload;
