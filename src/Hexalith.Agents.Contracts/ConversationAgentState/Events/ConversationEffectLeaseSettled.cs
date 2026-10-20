using Hexalith.EventStore.Contracts.Events;
namespace Hexalith.Agents.Contracts.ConversationAgentState.Events;

/// <summary>Append-only directory fact.</summary>
/// <param name="LeaseId">The LeaseId binding.</param>
/// <param name="CommitRevision">The CommitRevision binding.</param>
/// <param name="ResultRevision">The ResultRevision binding.</param>
/// <param name="Revision">The Revision binding.</param>
public sealed record ConversationEffectLeaseSettled(string LeaseId, long CommitRevision, long ResultRevision, long Revision) : IEventPayload;
