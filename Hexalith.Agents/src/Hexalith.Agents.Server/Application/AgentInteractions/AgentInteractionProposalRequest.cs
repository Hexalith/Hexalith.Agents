using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Server-internal request driving the Confirmation-mode proposal-creation orchestration (Story 3.1; AC1–AC4; AD-3, AD-5,
/// AD-13, AD-18). It carries the already-authenticated caller context plus the snapshot-recorded identity references and
/// policy-snapshot versions the orchestration needs to read the selected generated version, read the optional expiry, and
/// assemble the proposal. The orchestration assembles a server-trusted <c>AgentProposalCreationResult</c> and dispatches a
/// <c>CreateProposedAgentReply</c> command — any client-supplied value is discarded and the reserved trust extensions are
/// stripped from <see cref="ClientSuppliedExtensions"/>. It carries NO generated content (AD-14).
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The interaction's tenant scope (the envelope scope and every read's tenant).</param>
/// <param name="AgentInteractionId">The deterministic interaction id (the proposal's aggregate id; reused from Story 2.1) — the deterministic proposal id is derived from it + the selected version (AD-13).</param>
/// <param name="AgentId">The snapshot-recorded Agent reference (drives the optional expiry read).</param>
/// <param name="SourceConversationId">The snapshot-recorded source Conversation reference the proposal is linked to (opaque — AD-6).</param>
/// <param name="ResponseMode">The snapshot-recorded response mode (only <see cref="AgentResponseMode.Confirmation"/> creates a proposal on this path; FR-6).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (recorded on the proposal for the later policy basis; Story 3.5 enforces it).</param>
/// <param name="ContentSafetyPolicyVersion">The content-safety policy version snapshotted at request time the generated content passed.</param>
/// <param name="ActorUserId">The authenticated caller (the command-envelope user).</param>
/// <param name="ClientCorrelationId">An optional opaque client correlation reference for the caller's own tracing.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved trust keys are stripped).</param>
public sealed record AgentInteractionProposalRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentInteractionId,
    string AgentId,
    string SourceConversationId,
    AgentResponseMode ResponseMode,
    int ApproverPolicyVersion,
    int ContentSafetyPolicyVersion,
    string ActorUserId,
    string? ClientCorrelationId = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Server-internal outcome of the Confirmation-mode proposal-creation orchestration (Story 3.1; AC1–AC4). It carries ONLY
/// the safe interaction id and the decided <see cref="AgentInteractionStatus"/> — never the generated content, an EventStore
/// stream name, provider/Conversations detail, raw error text, or any evidence payload (AD-14). The status is computed by
/// the shared <c>AgentProposalCreationPolicy</c> from the same result the command dispatched, so the reported status cannot
/// drift from the aggregate's recorded decision (AD-3).
/// </summary>
/// <param name="AgentInteractionId">The deterministic interaction id the creation evaluated.</param>
/// <param name="Status">The decided creation status — <see cref="AgentInteractionStatus.ProposalCreated"/> or <see cref="AgentInteractionStatus.ProposalCreationFailed"/>; the defensive non-Confirmation short-circuit returns the unchanged <see cref="AgentInteractionStatus.Generated"/> (it does not create a proposal).</param>
public sealed record AgentInteractionProposalOutcomeResult(
    string AgentInteractionId,
    AgentInteractionStatus Status);
