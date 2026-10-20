using Hexalith.EventStore.Contracts.Events;
namespace Hexalith.Agents.Contracts.ConversationAgentState.Events;

/// <summary>Append-only directory fact.</summary>
/// <param name="TenantId">The TenantId binding.</param>
/// <param name="ConversationId">The ConversationId binding.</param>
/// <param name="DeletionRequestId">The DeletionRequestId binding.</param>
/// <param name="MustSettleLeaseIds">The MustSettleLeaseIds binding.</param>
/// <param name="Revision">The Revision binding.</param>
public sealed record ConversationDeletionClosingEntered(string TenantId, string ConversationId, string DeletionRequestId, IReadOnlyList<string> MustSettleLeaseIds, long Revision) : IEventPayload;
