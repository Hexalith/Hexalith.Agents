
namespace Hexalith.Agents.Contracts.ConversationAgentState.Commands;

/// <summary>Trusted directory command; expected revision serializes with Closing.</summary>
/// <param name="LeaseId">The LeaseId binding.</param>
/// <param name="AgentInteractionId">The AgentInteractionId binding.</param>
/// <param name="WorkflowInstanceId">The WorkflowInstanceId binding.</param>
/// <param name="ExpectedRevision">The ExpectedRevision binding.</param>
public sealed record CancelConversationEffectLease(string LeaseId, string AgentInteractionId, string WorkflowInstanceId, long ExpectedRevision);
