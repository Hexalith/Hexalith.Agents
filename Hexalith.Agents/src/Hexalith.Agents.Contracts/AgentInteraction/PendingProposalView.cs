namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, display-ready projection of one Proposed Agent Reply for the authorized proposal-queue discovery surface
/// (AC1, AC2, AC3, AC4; FR-13, FR-14). It exposes ONLY safe references — proposal/interaction/version ids, the
/// Source-Conversation reference, the caller Party reference, the Agent id, policy-version numbers, optional ISO-8601
/// expiry/created timestamps, and the coarse <see cref="State"/>/<see cref="InteractionStatus"/>/
/// <see cref="NeedsCurrentUserAction"/> flags — and deliberately <b>NEVER</b> the generated/edited content, a raw
/// provider/Conversations payload, an EventStore stream name, a stack trace, or a secret (AD-14). It mirrors
/// <see cref="AgentInteractionStatusView"/>: safe ids/enums/version-numbers only, no content field at all, so AC1
/// (a proposed reply is never rendered as a Conversation Message) holds by construction.
/// </summary>
/// <remarks>
/// The generated content's sole durable home stays the Story 2.4 <c>AgentOutputGenerated</c> event/state; this view
/// references only the proposal/version <em>ids</em>. The AC1 "current responsibility" column is <em>derived in UI
/// presentation</em> from <see cref="State"/>/<see cref="InteractionStatus"/> (V1: <c>Pending</c> → "awaiting approval";
/// the <see cref="NeedsCurrentUserAction"/> flag refines it to "awaiting your approval"), NOT a separate contract field —
/// this avoids a premature responsibility enum for states Stories 3.3–3.6 do not ship yet. The live computation of
/// <see cref="NeedsCurrentUserAction"/>/age/freshness (resolving the snapshotted <see cref="ApproverPolicyVersion"/> +
/// current user identity + current dependency availability against the read path) is part of the deferred Epic-4
/// read-model binding (AD-8/AD-12); the stable contract field lands here. All member names are kept clear of the
/// forbidden secret tokens (<c>Secret</c>/<c>ApiKey</c>/<c>Credential</c>/<c>Password</c>/<c>ConnectionString</c>).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate-id row handle).</param>
/// <param name="ProposalId">The deterministic proposal identifier created in Story 3.1 (derived from interaction + proposed version; AD-13).</param>
/// <param name="State">The proposal sub-state (<c>Pending</c> in V1; reserved 3.3–3.6 states render through a total default).</param>
/// <param name="InteractionStatus">The coarse durable Agent Call status (<c>ProposalCreated</c> in V1; the broader lifecycle signal).</param>
/// <param name="SourceConversationId">The source Conversation reference (an opaque reference — AD-6).</param>
/// <param name="CallerPartyId">The caller's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="AgentId">The target Agent identifier captured at request time.</param>
/// <param name="NeedsCurrentUserAction">The server-computed "needs my action" flag — drives the AC2 filter and the AC1 "current responsibility" column.</param>
/// <param name="ProposedVersionId">The selected generated version <em>id</em> held in the proposal (no content; AD-14).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time.</param>
/// <param name="ContentSafetyPolicyVersion">The content-safety policy version snapshotted at request time.</param>
/// <param name="ExpiresAt">The optional ISO-8601 expiry timestamp (<see langword="null"/> when no expiry policy is configured — AC1).</param>
/// <param name="CreatedAt">The optional ISO-8601 creation timestamp sourced from EventStore event metadata — the UI computes "age" from it (<see langword="null"/> when unavailable).</param>
public record PendingProposalView(
    string AgentInteractionId,
    string ProposalId,
    ProposedAgentReplyState State,
    AgentInteractionStatus InteractionStatus,
    string SourceConversationId,
    string CallerPartyId,
    string AgentId,
    bool NeedsCurrentUserAction,
    string ProposedVersionId,
    int ApproverPolicyVersion,
    int ContentSafetyPolicyVersion,
    string? ExpiresAt,
    string? CreatedAt);
