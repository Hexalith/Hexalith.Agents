using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Server-internal request driving the Confirmation-mode proposal-rejection orchestration (Story 3.6; AC1, AC4; AD-3, AD-5,
/// AD-12, AD-13). It carries the already-authenticated caller context, the snapshot-recorded identity references and policy
/// snapshot the orchestration needs to resolve rejection-time approver authorization, and the optional policy-defined
/// <see cref="RationaleCode"/>. The orchestration resolves authorization, assembles a server-trusted
/// <c>AgentProposalRejectionResult</c>, and dispatches a <c>RejectProposedAgentReply</c> command — any client-supplied value
/// is discarded and the reserved trust extensions are stripped from <see cref="ClientSuppliedExtensions"/>.
/// </summary>
/// <remarks>
/// <b>Content confinement (AD-14):</b> a rejection carries NO content — only safe ids + the policy basis + the optional
/// <see cref="RationaleCode"/> (a policy-defined code/category, never free text). <see cref="ApproverPolicy"/> is the
/// snapshotted Confirmation-mode Approver Policy re-resolved live against current dependencies + freshness (fail closed on any
/// uncertainty — AD-12).
/// </remarks>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The interaction's tenant scope (the envelope scope and the approver-resolution tenant).</param>
/// <param name="AgentInteractionId">The deterministic interaction id (the proposal's aggregate id).</param>
/// <param name="ProposalId">The deterministic proposal id created in Story 3.1 (recorded on the rejection evidence; AD-13).</param>
/// <param name="ProposalState">The trusted current proposal sub-state (drives the structural rejectable guard).</param>
/// <param name="SourceConversationId">The snapshot-recorded source Conversation reference the proposal is linked to (opaque — AD-6).</param>
/// <param name="ApproverPartyId">The acting Approver's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="ApproverPolicy">The snapshotted Confirmation-mode Approver Policy to re-resolve against current dependencies (null fails closed).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (recorded as the AC4 policy basis version).</param>
/// <param name="RationaleCode">The optional policy-defined safe rationale code/category (never free text or content — AD-14).</param>
/// <param name="ActorUserId">The authenticated caller (the command-envelope user).</param>
/// <param name="ClientCorrelationId">An optional opaque client correlation reference for the caller's own tracing.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved trust keys are stripped).</param>
public sealed record AgentInteractionProposalRejectionRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentInteractionId,
    string ProposalId,
    ProposedAgentReplyState ProposalState,
    string SourceConversationId,
    string ApproverPartyId,
    AgentApproverPolicy? ApproverPolicy,
    int ApproverPolicyVersion,
    string? RationaleCode,
    string ActorUserId,
    string? ClientCorrelationId = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Server-internal outcome of the Confirmation-mode proposal-rejection orchestration (Story 3.6; AC1, AC4). It carries ONLY
/// the decided <see cref="AgentInteractionStatus"/> and the safe <see cref="NotRejectableReason"/> classification — never any
/// version content, an EventStore stream name, provider/Conversations detail, raw error text, or any evidence payload
/// (AD-14). On a dispatched path the status is computed by the shared <c>AgentProposalRejectionPolicy</c> from the same
/// result the command dispatched, so the reported status cannot drift from the aggregate's recorded decision (AD-3); a
/// structural no-dispatch denial returns <see cref="AgentInteractionStatus.Unknown"/> and dispatches nothing.
/// </summary>
/// <param name="Status">The decided rejection status — <see cref="AgentInteractionStatus.ProposalRejected"/> on success, <see cref="AgentInteractionStatus.ProposalRejectionFailed"/> on a fail-closed authorization decision, or <see cref="AgentInteractionStatus.Unknown"/> on a structural no-dispatch denial.</param>
/// <param name="NotRejectableReason">The safe structural denial classification (<see cref="AgentProposedReplyNotRejectableReason.ProposalNotPending"/> on a structural no-dispatch denial; <see cref="AgentProposedReplyNotRejectableReason.Unknown"/> otherwise).</param>
public sealed record AgentInteractionProposalRejectionOutcomeResult(
    AgentInteractionStatus Status,
    AgentProposedReplyNotRejectableReason NotRejectableReason = AgentProposedReplyNotRejectableReason.Unknown);
