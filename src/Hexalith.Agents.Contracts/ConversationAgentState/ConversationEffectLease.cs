
namespace Hexalith.Agents.Contracts.ConversationAgentState;

/// <summary>Directory-owned effect capability; only CommitRevision authorizes the bound phase.</summary>
/// <param name="LeaseId">The LeaseId binding.</param>
/// <param name="AgentInteractionId">The AgentInteractionId binding.</param>
/// <param name="Kind">The Kind binding.</param>
/// <param name="OperationFamily">The OperationFamily binding.</param>
/// <param name="OperationIdentity">The OperationIdentity binding.</param>
/// <param name="WorkflowInstanceId">The WorkflowInstanceId binding.</param>
/// <param name="DirectoryEpochId">The DirectoryEpochId binding.</param>
/// <param name="Status">The Status binding.</param>
/// <param name="CommitRevision">The CommitRevision binding.</param>
/// <param name="ResultRevision">The ResultRevision binding.</param>
/// <param name="MustSettle">The MustSettle binding.</param>
public sealed record ConversationEffectLease(string LeaseId, string AgentInteractionId, ConversationEffectKind Kind, string OperationFamily, string OperationIdentity, string WorkflowInstanceId, string DirectoryEpochId, ConversationEffectLeaseStatus Status, long CommitRevision, long ResultRevision, bool MustSettle);
