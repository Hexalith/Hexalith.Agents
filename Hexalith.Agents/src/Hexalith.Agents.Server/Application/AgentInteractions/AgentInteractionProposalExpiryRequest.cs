using System;
using System.Collections.Generic;

using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Server-internal request driving the Confirmation-mode proposal-expiry orchestration (Story 3.6; AC3; AD-3, AD-5, AD-13).
/// It carries the snapshot-recorded identity references, the recorded <see cref="ExpiresAt"/> (the optional proposal expiry
/// metadata Story 3.1 records), and the <see cref="EvaluationTimestamp"/> — the <b>trusted "now"</b> supplied on the request
/// so determinism is unit-testable and the aggregate never reads the clock (AD-3). Expiry is system policy: there are NO
/// approver fields. The orchestration compares the recorded expiry to the trusted timestamp and dispatches
/// <c>ExpireProposedAgentReply</c> ONLY when the expiry has elapsed.
/// </summary>
/// <remarks>
/// The recorded <see cref="ExpiresAt"/> is the primary authority; when absent it is read fail-closed via
/// <c>IProposalExpiryPolicyReader</c> for the tenant + Agent (the deferred default returns no expiry — AD-18). A null/absent
/// expiry yields a deterministic no-transition (the proposal is unbounded) and dispatches nothing.
/// </remarks>
/// <param name="MessageId">The command idempotency key (ULID).</param>
/// <param name="CorrelationId">The correlation id for tracing.</param>
/// <param name="TenantId">The interaction's tenant scope (the envelope scope and the expiry-reader tenant — FR-19).</param>
/// <param name="AgentInteractionId">The deterministic interaction id (the proposal's aggregate id).</param>
/// <param name="ProposalId">The deterministic proposal id created in Story 3.1 (recorded on the expiry evidence; AD-13).</param>
/// <param name="ProposalState">The trusted current proposal sub-state (drives the structural expirable guard).</param>
/// <param name="AgentId">The Agent whose configured expiry policy is read when no recorded <see cref="ExpiresAt"/> is supplied.</param>
/// <param name="SourceConversationId">The snapshot-recorded source Conversation reference the proposal is linked to (opaque — AD-6).</param>
/// <param name="ExpiresAt">The recorded ISO-8601 expiry timestamp (the optional Story 3.1 metadata; <see langword="null"/> when unbounded).</param>
/// <param name="EvaluationTimestamp">The trusted evaluation "now" the recorded expiry is compared against (supplied, never read inside the aggregate — AD-3).</param>
/// <param name="ActorUserId">The actor user id stamped on the command envelope (the durable owner/system actor).</param>
/// <param name="ClientCorrelationId">An optional opaque client correlation reference.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved trust keys are stripped).</param>
public sealed record AgentInteractionProposalExpiryRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentInteractionId,
    string ProposalId,
    ProposedAgentReplyState ProposalState,
    string AgentId,
    string SourceConversationId,
    string? ExpiresAt,
    DateTimeOffset EvaluationTimestamp,
    string ActorUserId,
    string? ClientCorrelationId = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Server-internal outcome of the Confirmation-mode proposal-expiry orchestration (Story 3.6; AC3). It carries ONLY the safe
/// <see cref="AgentProposalExpiryOutcome"/> classification and the decided <see cref="AgentInteractionStatus"/> — never any
/// version content, an EventStore stream name, or any payload (AD-14). On the elapsed-expiry path the status comes from the
/// shared <c>AgentProposalExpiryPolicy</c> (AD-3 no-drift); every no-transition path (no expiry, expiry not reached, not
/// pending) returns <see cref="AgentInteractionStatus.Unknown"/> and dispatches nothing.
/// </summary>
/// <param name="Outcome">The safe expiry outcome classification (only <see cref="AgentProposalExpiryOutcome.Expired"/> dispatches a transition).</param>
/// <param name="Status">The decided expiry status — <see cref="AgentInteractionStatus.ProposalExpired"/> on an elapsed expiry, otherwise <see cref="AgentInteractionStatus.Unknown"/>.</param>
public sealed record AgentInteractionProposalExpiryOutcomeResult(
    AgentProposalExpiryOutcome Outcome,
    AgentInteractionStatus Status);
