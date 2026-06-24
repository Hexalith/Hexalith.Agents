using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Server-internal request driving the Confirmation-mode proposal-edit orchestration (Story 3.3; AC1, AC2, AC4; AD-3,
/// AD-5, AD-12, AD-13). It carries the already-authenticated caller context, the snapshot-recorded identity references and
/// policy-snapshot versions the orchestration needs to resolve edit-time approver authorization, the source version being
/// edited from, and the user-supplied <see cref="EditedContent"/>. The orchestration resolves authorization, derives the
/// deterministic edited-version id, assembles a server-trusted <c>AgentProposalEditResult</c>, and dispatches an
/// <c>EditProposedAgentReply</c> command — any client-supplied value is discarded and the reserved trust extensions are
/// stripped from <see cref="ClientSuppliedExtensions"/>.
/// </summary>
/// <remarks>
/// <b>Content confinement (AD-14):</b> <see cref="EditedContent"/> is sensitive conversation-derived content; it flows to
/// the aggregate ONLY on the edit command's version (its legitimate write-path home) and never onto the safe evidence,
/// outcome, rejection, or any read view. <see cref="ApproverPolicy"/> is the snapshotted Confirmation-mode Approver Policy
/// re-resolved live against current dependencies + freshness (fail closed on any uncertainty — AD-12).
/// </remarks>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The interaction's tenant scope (the envelope scope and the approver-resolution tenant).</param>
/// <param name="AgentInteractionId">The deterministic interaction id (the proposal's aggregate id) — the edited version id is derived from it + source version + edit attempt (AD-13).</param>
/// <param name="ProposalId">The deterministic proposal id created in Story 3.1 (recorded on the edit evidence; AD-13).</param>
/// <param name="SourceConversationId">The snapshot-recorded source Conversation reference the proposal is linked to (opaque — AD-6).</param>
/// <param name="SourceVersionId">The id of the version being edited from (its provenance; drives the edited-version id derivation).</param>
/// <param name="EditedContent">The Approver's edited content (sensitive — carried to the aggregate's edit version only; AD-14).</param>
/// <param name="EditorPartyId">The authoring Approver's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="EditAttemptId">The deterministic edit attempt id so a retried edit reuses the same edited-version id (AD-13).</param>
/// <param name="ApproverPolicy">The snapshotted Confirmation-mode Approver Policy to re-resolve against current dependencies (null fails closed).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (recorded as the AC4 policy basis version).</param>
/// <param name="ContentSafetyPolicyVersion">The content-safety policy version snapshotted at request time (carried onto the edited version for audit).</param>
/// <param name="SourceProviderId">The source version's safe provider id (inherited onto the edited version for provenance — AD-9).</param>
/// <param name="SourceModelId">The source version's safe model id (inherited onto the edited version for provenance — AD-9).</param>
/// <param name="SourceProviderCapabilityVersion">The source version's provider capability version (inherited onto the edited version for provenance).</param>
/// <param name="ActorUserId">The authenticated caller (the command-envelope user).</param>
/// <param name="ClientCorrelationId">An optional opaque client correlation reference for the caller's own tracing.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved trust keys are stripped).</param>
public sealed record AgentInteractionProposalEditRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentInteractionId,
    string ProposalId,
    string SourceConversationId,
    string SourceVersionId,
    string EditedContent,
    string EditorPartyId,
    string EditAttemptId,
    AgentApproverPolicy? ApproverPolicy,
    int ApproverPolicyVersion,
    int ContentSafetyPolicyVersion,
    string SourceProviderId,
    string SourceModelId,
    int SourceProviderCapabilityVersion,
    string ActorUserId,
    string? ClientCorrelationId = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Server-internal outcome of the Confirmation-mode proposal-edit orchestration (Story 3.3; AC1, AC2, AC4). It carries
/// ONLY the safe edited-version id, the decided <see cref="AgentInteractionStatus"/>, and the safe
/// <see cref="NotEditableReason"/> classification — never the edited content, an EventStore stream name,
/// provider/Conversations detail, raw error text, or any evidence payload (AD-14). On the happy path the status is computed
/// by the shared <c>AgentProposalEditPolicy</c> from the same result the command dispatched, so the reported status cannot
/// drift from the aggregate's recorded decision (AD-3); a fail-closed authorization denial returns no edited version and
/// dispatches nothing.
/// </summary>
/// <param name="EditedVersionId">The deterministic edited version id (empty on a fail-closed denial — no edit happened).</param>
/// <param name="Status">The decided edit status — <see cref="AgentInteractionStatus.ProposalEdited"/> on success, or <see cref="AgentInteractionStatus.Unknown"/> on a no-dispatch denial.</param>
/// <param name="NotEditableReason">The safe denial classification (<see cref="AgentProposedReplyNotEditableReason.NotAuthorized"/> on a fail-closed authorization denial; <see cref="AgentProposedReplyNotEditableReason.Unknown"/> otherwise).</param>
public sealed record AgentInteractionProposalEditOutcomeResult(
    string EditedVersionId,
    AgentInteractionStatus Status,
    AgentProposedReplyNotEditableReason NotEditableReason = AgentProposedReplyNotEditableReason.Unknown);
