using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, display-ready projection of ONE Proposed Agent Reply for the Story 3.7 approval-detail workspace (AC1, AC2;
/// FR-13, FR-15, FR-16, FR-17). It aggregates the queue-row metadata shape of <see cref="PendingProposalView"/> (proposal
/// /interaction/version ids, the Source-Conversation and caller references, the Agent id, policy versions, coarse
/// state/responsibility flags, and optional ISO-8601 timestamps), the AD-4 interaction-snapshot values the workspace
/// surfaces (<see cref="ResponseMode"/>, <see cref="ProviderId"/>, <see cref="ModelId"/>), the posting outcome
/// (<see cref="ApprovedVersionId"/>/<see cref="ApprovedAt"/>/<see cref="PostedAt"/>), and the safe append-only
/// <see cref="Versions"/> history (AD-5). It exposes ONLY safe ids/enums/version-numbers/timestamps and deliberately
/// <b>NEVER</b> the generated/edited content, a raw provider/Conversations payload, an EventStore stream name, a stack
/// trace, or a secret (AD-9, AD-14): a proposed reply is never rendered as a Conversation Message by construction. The
/// generated content's sole durable home stays the Story 2.4/3.3 success events; this view references only the version
/// <em>ids</em>, and the content body renders only through the authorized durable version reader.
/// </summary>
/// <remarks>
/// The snapshot values surfaced here (<see cref="ResponseMode"/>, <see cref="ProviderId"/>, <see cref="ModelId"/>,
/// <see cref="ApproverPolicyVersion"/>, <see cref="ContentSafetyPolicyVersion"/>) are the request-time AD-4 snapshot, not
/// live config, so a later admin edit affects only future interactions (AD-4). <see cref="ExpiresAt"/> is the snapshotted
/// expiry (the live expiry firing trigger / policy reader is deferred to Epic 4 — this view <em>displays</em> the
/// snapshotted value, it does not fire expiry). The live computation binding this view to the EventStore read path is
/// the deferred Epic-4 read-model story; the stable contract lands here. All member names are kept clear of the forbidden
/// secret tokens (<c>Secret</c>/<c>ApiKey</c>/<c>Credential</c>/<c>Password</c>/<c>ConnectionString</c>) and of the
/// content-bearing member names the read-view guard forbids.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate-id row handle).</param>
/// <param name="ProposalId">The deterministic proposal identifier created in Story 3.1 (AD-13).</param>
/// <param name="State">The proposal sub-state driving act-on affordances and the state badge.</param>
/// <param name="InteractionStatus">The coarse durable Agent Call status (the broader lifecycle signal).</param>
/// <param name="SourceConversationId">The source Conversation reference (an opaque reference — AD-6).</param>
/// <param name="CallerPartyId">The caller's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="AgentId">The target Agent identifier captured at request time.</param>
/// <param name="NeedsCurrentUserAction">The server-computed "needs my action" flag for the current Approver.</param>
/// <param name="SelectedVersionId">The currently selected generated version <em>id</em> (the editor/approver target; no content — AD-14).</param>
/// <param name="ResponseMode">The AD-4 response mode snapshotted at request time (surfaced read-only).</param>
/// <param name="ProviderId">The safe provider identifier from the interaction snapshot (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The safe model identifier from the interaction snapshot (a reference, not a secret — AD-9).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time.</param>
/// <param name="ContentSafetyPolicyVersion">The content-safety policy version snapshotted at request time.</param>
/// <param name="ExpiresAt">The optional ISO-8601 expiry timestamp snapshotted at request time (<see langword="null"/> when no expiry policy is configured — AC1).</param>
/// <param name="CreatedAt">The optional ISO-8601 creation timestamp from EventStore event metadata (<see langword="null"/> when unavailable).</param>
/// <param name="ApprovedVersionId">The id of the approved version once approval is recorded (<see langword="null"/> when no version is approved yet; AD-5 — approval selects exactly one version).</param>
/// <param name="ApprovedAt">The optional ISO-8601 approval timestamp from EventStore event metadata (<see langword="null"/> when not approved).</param>
/// <param name="PostedAt">The optional ISO-8601 posting timestamp from EventStore event metadata (<see langword="null"/> until the approved version is posted — never imply "posted" while posting is pending/failed).</param>
/// <param name="Versions">The append-only, content-free version history (AD-5) — every generated/edited/regenerated version preserved for audit, ordered oldest-first.</param>
public record ProposalDetailView(
    string AgentInteractionId,
    string ProposalId,
    ProposedAgentReplyState State,
    AgentInteractionStatus InteractionStatus,
    string SourceConversationId,
    string CallerPartyId,
    string AgentId,
    bool NeedsCurrentUserAction,
    string SelectedVersionId,
    AgentResponseMode ResponseMode,
    string ProviderId,
    string ModelId,
    int ApproverPolicyVersion,
    int ContentSafetyPolicyVersion,
    string? ExpiresAt,
    string? CreatedAt,
    string? ApprovedVersionId,
    string? ApprovedAt,
    string? PostedAt,
    IReadOnlyList<ProposalVersionSummary> Versions);
