
namespace Hexalith.Agents.Contracts.ConversationAgentState.Commands;

/// <summary>Closes even an empty directory; the exact owner target is explicit.</summary>
/// <param name="TenantId">The TenantId binding.</param>
/// <param name="ConversationId">The ConversationId binding.</param>
/// <param name="DeletionRequestId">The DeletionRequestId binding.</param>
/// <param name="ExpectedRevision">The ExpectedRevision binding.</param>
public sealed record EnterConversationDeletionClosing(string TenantId, string ConversationId, string DeletionRequestId, long ExpectedRevision);
