using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Server-internal request driving the Confirmation-mode proposal-regeneration orchestration (Story 3.4; AC1–AC4; AD-3,
/// AD-5, AD-9, AD-12, AD-13). It carries the already-authenticated caller context, the trusted current proposal sub-state
/// (so AC4's terminal-proposal check happens BEFORE any provider invocation), the snapshot-recorded identity references and
/// policy-snapshot versions the orchestration needs to resolve regeneration-time approver authorization, re-read the SAME
/// Source Conversation, read the Provider budget, re-invoke the provider behind its adapter, and resolve + evaluate the
/// effective Content Safety Policy, plus the deterministic <see cref="RegenerationAttemptId"/> seed. The orchestration
/// assembles a server-trusted <c>AgentProposalRegenerationResult</c> and dispatches a <c>RegenerateProposedAgentReply</c>
/// command — any client-supplied value is discarded and the reserved trust extensions are stripped from
/// <see cref="ClientSuppliedExtensions"/>.
/// </summary>
/// <remarks>
/// <b>AC4 terminal-proposal guard:</b> <see cref="ProposalState"/> is the server-trusted current proposal sub-state; the
/// orchestration denies a non-retryable (terminal) proposal WITHOUT reading the conversation, invoking the provider, or
/// dispatching — no provider invocation occurs. <b>Configuration confinement (FR-16):</b> the regeneration reuses the same
/// snapshotted <see cref="ProviderId"/>/<see cref="ModelId"/>/<see cref="ProviderCapabilityVersion"/>/content-safety policy;
/// there is no caller-supplied configuration on this request, so a configuration-version change cannot be smuggled in.
/// <b>Content confinement (AD-14):</b> no content rides on this request — the freshly generated content is read transiently
/// inside the orchestration and flows to the aggregate only on the success command's version. <see cref="ApproverPolicy"/> is
/// the snapshotted Confirmation-mode Approver Policy re-resolved live against current dependencies + freshness (fail closed
/// on any uncertainty — AD-12).
/// </remarks>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The interaction's tenant scope (the envelope scope and every read's tenant).</param>
/// <param name="AgentInteractionId">The deterministic interaction id (the proposal's aggregate id) — the regeneration ids are derived from it + source conversation + regeneration attempt (AD-13).</param>
/// <param name="ProposalId">The deterministic proposal id created in Story 3.1 (recorded on the regeneration evidence; AD-13).</param>
/// <param name="ProposalState">The server-trusted current proposal sub-state used for the AC4 terminal-proposal guard (non-retryable ⇒ no provider invocation, no dispatch).</param>
/// <param name="AgentId">The snapshot-recorded Agent reference (drives the effective Content Safety Policy read).</param>
/// <param name="SourceConversationId">The snapshot-recorded source Conversation reference the regeneration re-reads (opaque — AD-6).</param>
/// <param name="ProviderId">The snapshot-recorded provider reference (drives the budget read + provider invocation; AD-9).</param>
/// <param name="ModelId">The snapshot-recorded model reference (drives the budget read + provider invocation; AD-9).</param>
/// <param name="ProviderCapabilityVersion">The snapshot-recorded provider capability version (recorded on the regenerated version/evidence; AD-4, AD-10).</param>
/// <param name="ContentSafetyPolicyVersion">The snapshot-recorded content-safety policy version (carried for audit symmetry; the live effective version is read for the gate).</param>
/// <param name="ResponseMode">The snapshot-recorded response mode (selects the mode-specific content-safety override, if any; FR-26).</param>
/// <param name="RequesterPartyId">The requesting Approver's stable Party reference (a reference, not PII — AD-7; recorded on the regeneration evidence).</param>
/// <param name="RegenerationAttemptId">The deterministic regeneration attempt seed so a retried regeneration reuses the same ids (AD-13).</param>
/// <param name="ApproverPolicy">The snapshotted Confirmation-mode Approver Policy to re-resolve against current dependencies (null fails closed).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (recorded as the AC4 policy basis version).</param>
/// <param name="ActorUserId">The authenticated caller (the command-envelope user and the Conversations authorized-read principal).</param>
/// <param name="ClientCorrelationId">An optional opaque client correlation reference for the caller's own tracing.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved trust keys are stripped).</param>
public sealed record AgentInteractionProposalRegenerationRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentInteractionId,
    string ProposalId,
    ProposedAgentReplyState ProposalState,
    string AgentId,
    string SourceConversationId,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int ContentSafetyPolicyVersion,
    AgentResponseMode ResponseMode,
    string RequesterPartyId,
    string RegenerationAttemptId,
    AgentApproverPolicy? ApproverPolicy,
    int ApproverPolicyVersion,
    string ActorUserId,
    string? ClientCorrelationId = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Server-internal outcome of the Confirmation-mode proposal-regeneration orchestration (Story 3.4; AC1–AC4). It carries
/// ONLY the safe regenerated-version id, the decided <see cref="AgentInteractionStatus"/>, and the safe
/// <see cref="NotRegeneratableReason"/> classification — never the regenerated content, an EventStore stream name,
/// provider/Conversations detail, raw error text, or any evidence payload (AD-9, AD-14). On the happy path the status is
/// computed by the shared <c>AgentProposalRegenerationPolicy</c> from the same result the command dispatched, so the reported
/// status cannot drift from the aggregate's recorded decision (AD-3); a fail-closed terminal-proposal or authorization denial
/// returns no regenerated version and dispatches nothing (no provider invocation).
/// </summary>
/// <param name="RegeneratedVersionId">The deterministic regenerated version id (empty on a fail-closed no-dispatch denial — no regeneration happened).</param>
/// <param name="Status">The decided regeneration status — <see cref="AgentInteractionStatus.ProposalRegenerated"/> or <see cref="AgentInteractionStatus.ProposalRegenerationFailed"/> on dispatch, or <see cref="AgentInteractionStatus.Unknown"/> on a no-dispatch denial.</param>
/// <param name="NotRegeneratableReason">The safe denial classification (<see cref="AgentProposedReplyNotRegeneratableReason.ProposalNotPending"/> for a terminal proposal, <see cref="AgentProposedReplyNotRegeneratableReason.NotAuthorized"/> for a fail-closed authorization denial; <see cref="AgentProposedReplyNotRegeneratableReason.Unknown"/> otherwise).</param>
public sealed record AgentInteractionProposalRegenerationOutcomeResult(
    string RegeneratedVersionId,
    AgentInteractionStatus Status,
    AgentProposedReplyNotRegeneratableReason NotRegeneratableReason = AgentProposedReplyNotRegeneratableReason.Unknown);
