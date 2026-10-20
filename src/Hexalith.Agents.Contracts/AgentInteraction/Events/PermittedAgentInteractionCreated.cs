using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.EventStore.Contracts.Events;
namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>Immutable permit-created interaction and its reciprocal durable outboxes.</summary>
/// <param name="Permit">The Permit binding.</param>
/// <param name="PermitRevision">The PermitRevision binding.</param>
/// <param name="WorkflowStartOutboxId">The WorkflowStartOutboxId binding.</param>
/// <param name="DirectoryAcknowledgementOutboxId">The DirectoryAcknowledgementOutboxId binding.</param>
/// <param name="AttemptId">The AttemptId binding.</param>
/// <param name="ReservationId">The ReservationId binding.</param>
/// <param name="AdmissionId">The AdmissionId binding.</param>
/// <param name="ProposalVersionId">The ProposalVersionId binding.</param>
/// <param name="MessageId">The MessageId binding.</param>
/// <param name="Revision">The Revision binding.</param>
public sealed record PermittedAgentInteractionCreated(InteractionPermit Permit, long PermitRevision, string WorkflowStartOutboxId, string DirectoryAcknowledgementOutboxId, string AttemptId, string ReservationId, string AdmissionId, string ProposalVersionId, string MessageId, long Revision) : IEventPayload;
