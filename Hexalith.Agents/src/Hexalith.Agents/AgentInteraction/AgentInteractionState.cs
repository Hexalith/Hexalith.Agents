using System;
using System.Collections.Generic;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Replay state for one Agent Call (<c>AgentInteraction</c>) aggregate (AD-2 aggregate boundary; aggregate id =
/// <see cref="AgentInteractionId"/> = the command envelope's aggregate id, the deterministic interaction id).
/// State changes only through the <c>Apply</c> methods (AD-3); no-op <c>Apply</c> methods for the rejection events
/// keep replay total so a persisted rejection never breaks rehydration.
/// </summary>
/// <remarks>
/// <see cref="IsRequested"/> distinguishes a never-requested interaction (e.g. a stream containing only a persisted
/// validation/conflict rejection) from one whose request was recorded. <see cref="Prompt"/> is sensitive
/// Conversation-derived content (AD-14) — held here as the durable source of truth but never surfaced on the status
/// view, the status reference, rejections, logs, or audit summaries, mirroring the Agent <c>Instructions</c> field.
/// </remarks>
public sealed class AgentInteractionState
{
    /// <summary>Gets or sets a value indicating whether the Agent Call request has been recorded.</summary>
    public bool IsRequested { get; set; }

    /// <summary>Gets or sets the deterministic Agent Call identifier (the aggregate id).</summary>
    public string AgentInteractionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the target Agent identifier captured at request time.</summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>Gets or sets the caller's stable Party reference (a reference, not PII — AD-7).</summary>
    public string CallerPartyId { get; set; } = string.Empty;

    /// <summary>Gets or sets the source Conversation reference (an opaque reference — AD-6).</summary>
    public string SourceConversationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the AD-4 configuration snapshot frozen at request time (<see langword="null"/> until requested).</summary>
    public AgentInteractionSnapshot? Snapshot { get; set; }

    /// <summary>Gets or sets the caller's prompt (sensitive — durable here only; AD-14; never surfaced).</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>Gets or sets the caller idempotency metadata recorded for the deterministic-id derivation (AD-13).</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the coarse Agent Call status (Story 2.2 records the terminal gate decision; Story 2.1 leaves it at <see cref="AgentInteractionStatus.Requested"/>).</summary>
    public AgentInteractionStatus Status { get; set; } = AgentInteractionStatus.Unknown;

    /// <summary>Gets or sets the safe blocker verdicts recorded when the gate failed (Audit Evidence; FR-24, AD-14). <see langword="null"/> until a gate decision is recorded.</summary>
    public IReadOnlyList<AgentInvocationGateVerdict>? GateVerdicts { get; set; }

    /// <summary>Gets or sets the safe Conversation context evidence recorded when the context decision was made (Audit Evidence; FR-24, AD-14). <see langword="null"/> until a context decision is recorded.</summary>
    public AgentInteractionContextEvidence? ContextEvidence { get; set; }

    /// <summary>Gets or sets the safe block-reason classification recorded when context could not be built within safe bounds (FR-25, AD-12). <see langword="null"/> until a context-blocked decision is recorded.</summary>
    public AgentInteractionContextBlockReason? ContextBlockReason { get; set; }

    /// <summary>Gets or sets the append-only generated version history (AD-5; sole durable home of generated content alongside the success event, AD-14). A list so Epic 3 regeneration can append. <see langword="null"/> until the first successful generation.</summary>
    public IReadOnlyList<AgentGeneratedVersion>? GeneratedVersions { get; set; }

    /// <summary>Gets or sets the safe generation failure-reason classification recorded when generation failed closed (FR-24, AD-12). <see langword="null"/> until a generation-failed/safety-failed decision is recorded.</summary>
    public AgentOutputGenerationFailureReason? GenerationFailureReason { get; set; }

    /// <summary>Gets or sets the safe posted-message evidence recorded when the posting decision was made (Audit Evidence; FR-24, AD-14; ids only, never content). <see langword="null"/> until a posting decision is recorded.</summary>
    public AgentPostedMessageEvidence? PostingEvidence { get; set; }

    /// <summary>Gets or sets the safe posting failure-reason classification recorded when posting failed closed (FR-24, AD-12). <see langword="null"/> until a posting-failed decision is recorded.</summary>
    public AgentResponsePostingFailureReason? PostingFailureReason { get; set; }

    /// <summary>Gets or sets the Confirmation-mode proposal sub-state (<see cref="ProposedAgentReplyState.Pending"/> after creation; Story 3.1; AD-5). <see langword="null"/> until a proposal is created.</summary>
    public ProposedAgentReplyState? ProposalState { get; set; }

    /// <summary>Gets or sets the safe proposal evidence recorded when the proposal-creation decision was made (Audit Evidence; FR-24, AD-14; ids only, never content). <see langword="null"/> until a proposal-creation decision is recorded.</summary>
    public AgentProposedReplyEvidence? ProposalEvidence { get; set; }

    /// <summary>Gets or sets the safe proposal-creation failure-reason classification recorded when creation failed closed (FR-24, AD-12). <see langword="null"/> until a creation-failed decision is recorded.</summary>
    public AgentProposalCreationFailureReason? ProposalCreationFailureReason { get; set; }

    /// <summary>Gets or sets the safe proposal-edit evidence recorded when the latest edit decision was made (Audit Evidence; FR-24, AD-14; ids + policy basis only, never content). <see langword="null"/> until an edit decision is recorded (Story 3.3).</summary>
    public AgentProposedReplyEditEvidence? ProposalEditEvidence { get; set; }

    /// <summary>Gets or sets the safe proposal-edit failure-reason classification recorded when an edit failed closed (FR-24, AD-12). <see langword="null"/> until an edit-failed decision is recorded (Story 3.3).</summary>
    public AgentProposalEditFailureReason? ProposalEditFailureReason { get; set; }

    /// <summary>Gets or sets the safe proposal-regeneration evidence recorded when the latest regeneration decision was made (Audit Evidence; FR-24, AD-14; ids + provider/policy basis only, never content). <see langword="null"/> until a regeneration decision is recorded (Story 3.4).</summary>
    public AgentProposedReplyRegenerationEvidence? ProposalRegenerationEvidence { get; set; }

    /// <summary>Gets or sets the safe proposal-regeneration failure-reason classification recorded when a regeneration failed closed (FR-24, AD-12). <see langword="null"/> until a regeneration-failed decision is recorded (Story 3.4).</summary>
    public AgentProposalRegenerationFailureReason? ProposalRegenerationFailureReason { get; set; }

    /// <summary>Gets or sets the approved proposal version id frozen for posting (Story 3.5).</summary>
    public string ApprovedVersionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the approving Party reference (Story 3.5).</summary>
    public string ApproverPartyId { get; set; } = string.Empty;

    /// <summary>Gets or sets the deterministic posting message id for the approved version (Story 3.5).</summary>
    public string ApprovalPostingMessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the deterministic posting idempotency key for the approved version (Story 3.5).</summary>
    public string ApprovalPostingIdempotencyKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the safe approval/posting evidence (Story 3.5).</summary>
    public AgentProposedReplyApprovalEvidence? ProposalApprovalEvidence { get; set; }

    /// <summary>Gets or sets the safe approval failure reason (Story 3.5).</summary>
    public AgentProposalApprovalFailureReason? ProposalApprovalFailureReason { get; set; }

    /// <summary>Gets or sets the safe approved-version posting failure reason (Story 3.5).</summary>
    public AgentProposalApprovalFailureReason? ProposalPostingFailureReason { get; set; }

    /// <summary>Gets or sets the safe proposal-rejection evidence recorded when the proposal was rejected (Audit Evidence; FR-24, AD-14; ids + policy basis + rationale code only, never content). <see langword="null"/> until a rejection is recorded (Story 3.6).</summary>
    public AgentProposedReplyRejectionEvidence? ProposalRejectionEvidence { get; set; }

    /// <summary>Gets or sets the safe proposal-abandonment evidence recorded when the proposal was abandoned (Audit Evidence; FR-24, AD-14; ids + policy basis only, never content). <see langword="null"/> until an abandonment is recorded (Story 3.6).</summary>
    public AgentProposedReplyAbandonmentEvidence? ProposalAbandonmentEvidence { get; set; }

    /// <summary>Gets or sets the safe proposal-expiry evidence recorded when the proposal expired (Audit Evidence; FR-24, AD-14; ids + recorded expiry only, never content). <see langword="null"/> until an expiry is recorded (Story 3.6).</summary>
    public AgentProposedReplyExpiryEvidence? ProposalExpiryEvidence { get; set; }

    /// <summary>Applies the Agent Call request: the interaction exists and freezes its configuration snapshot (AC1).</summary>
    /// <param name="e">The event.</param>
    public void Apply(InteractionRequested e)
    {
        ArgumentNullException.ThrowIfNull(e);
        IsRequested = true;
        AgentInteractionId = e.AgentInteractionId;
        AgentId = e.AgentId;
        CallerPartyId = e.CallerPartyId;
        SourceConversationId = e.SourceConversationId;
        Snapshot = e.Snapshot;
        Prompt = e.Prompt;
        IdempotencyKey = e.IdempotencyKey;
        Status = AgentInteractionStatus.Requested;
    }

    /// <summary>Applies the passed-gate outcome: the interaction is authorized to proceed to context building (AC1; Story 2.2).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentInteractionAuthorized e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.Authorized;
    }

    /// <summary>Applies the failed-gate outcome: records the terminal denied/blocked decision and its safe blocker evidence (AC1, AC2, AC4; Story 2.2).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentInteractionGateFailed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = e.Decision;
        GateVerdicts = e.Blockers;
    }

    /// <summary>No-op replay handler — the gate-not-evaluable rejection carries no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentInteractionGateNotEvaluableRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>Applies the context-ready outcome: the interaction built its context within safe bounds and may proceed to generation (AC2, AC4; Story 2.3).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentInteractionContextReady e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ContextReady;
        ContextEvidence = e.Evidence;
    }

    /// <summary>Applies the context-blocked outcome: records the terminal fail-closed context decision and its safe evidence (AC3; Story 2.3).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentInteractionContextBlocked e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ContextBlocked;
        ContextEvidence = e.Evidence;
        ContextBlockReason = e.Reason;
    }

    /// <summary>No-op replay handler — the context-not-buildable rejection carries no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentInteractionContextNotBuildableRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>Applies the generated outcome: records the success status and appends the approvable version to the append-only history (AC2, AC4; Story 2.4; AD-5, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentOutputGenerated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.Generated;
        var versions = GeneratedVersions is null
            ? new List<AgentGeneratedVersion>()
            : new List<AgentGeneratedVersion>(GeneratedVersions);
        versions.Add(e.Version);
        GeneratedVersions = versions;
    }

    /// <summary>Applies the generation-failed outcome: records the terminal fail-closed decision and its safe reason (AC3; Story 2.4; AD-5, AD-12).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentOutputGenerationFailed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = e.Decision;
        GenerationFailureReason = e.Reason;
    }

    /// <summary>No-op replay handler — the not-generatable rejection carries no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentOutputNotGeneratableRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>Applies the posted outcome: records the terminal success status and the safe posted-message evidence (AC1, AC2; Story 2.5; AD-7, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentResponsePosted e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.Posted;
        PostingEvidence = e.Evidence;
    }

    /// <summary>Applies the posting-failed outcome: records the terminal fail-closed decision, its safe reason, and the attempted evidence (AC4; Story 2.5; AD-12, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentResponsePostingFailed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.PostingFailed;
        PostingFailureReason = e.Reason;
        PostingEvidence = e.Evidence;
    }

    /// <summary>No-op replay handler — the not-postable rejection carries no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentResponseNotPostableRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>Applies the proposal-created outcome: records the terminal success status, the initial <see cref="ProposedAgentReplyState.Pending"/> proposal sub-state, and the safe proposal evidence (AC1, AC2; Story 3.1; AD-5, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalCreated;
        ProposalState = ProposedAgentReplyState.Pending;
        ProposalEvidence = e.Evidence;
    }

    /// <summary>Applies the proposal-creation-failed outcome: records the terminal fail-closed decision, its safe reason, and the attempted evidence — no proposal exists, so <see cref="ProposalState"/> stays null (AC3, AC4; Story 3.1; AD-12, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyCreationFailed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalCreationFailed;
        ProposalCreationFailureReason = e.Reason;
        ProposalEvidence = e.Evidence;
    }

    /// <summary>No-op replay handler — the not-creatable rejection carries no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProposedAgentReplyNotCreatableRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>Applies the proposal-edited outcome: appends the new immutable edited version to the append-only history (preserving prior versions), records the <see cref="ProposedAgentReplyState.Edited"/> sub-state, and records the safe edit evidence (AC1, AC4; Story 3.3; AD-5, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyEdited e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalEdited;
        ProposalState = ProposedAgentReplyState.Edited;
        ProposalEditEvidence = e.Evidence;
        var versions = GeneratedVersions is null
            ? new List<AgentGeneratedVersion>()
            : new List<AgentGeneratedVersion>(GeneratedVersions);
        versions.Add(e.EditedVersion);
        GeneratedVersions = versions;
    }

    /// <summary>Applies the proposal-edit-failed outcome: records the terminal fail-closed decision, its safe reason, and the attempted evidence — no new version is appended and the prior <see cref="ProposalState"/> is preserved (AC2, AC4; Story 3.3; AD-12, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyEditFailed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalEditFailed;
        ProposalEditFailureReason = e.Reason;
        ProposalEditEvidence = e.Evidence;
    }

    /// <summary>No-op replay handler — the not-editable rejection carries no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProposedAgentReplyNotEditableRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>Applies the proposal-regenerated outcome: appends the new immutable regenerated version to the append-only history (preserving prior versions), records the <see cref="ProposedAgentReplyState.Regenerated"/> sub-state, and records the safe regeneration evidence (AC1, AC2, AC4; Story 3.4; AD-5, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyRegenerated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalRegenerated;
        ProposalState = ProposedAgentReplyState.Regenerated;
        ProposalRegenerationEvidence = e.Evidence;
        var versions = GeneratedVersions is null
            ? new List<AgentGeneratedVersion>()
            : new List<AgentGeneratedVersion>(GeneratedVersions);
        versions.Add(e.RegeneratedVersion);
        GeneratedVersions = versions;
    }

    /// <summary>Applies the proposal-regeneration-failed outcome: records the terminal fail-closed decision, its safe reason, and the attempted evidence — no new version is appended and the prior <see cref="ProposalState"/> is preserved so the proposal stays retryable (AC3, AC4; Story 3.4; AD-12, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyRegenerationFailed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalRegenerationFailed;
        ProposalRegenerationFailureReason = e.Reason;
        ProposalRegenerationEvidence = e.Evidence;
    }

    /// <summary>Applies the approved outcome: exactly one selected version is frozen for posting.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyApproved e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalApproved;
        ProposalState = ProposedAgentReplyState.Approved;
        ProposalApprovalEvidence = e.Evidence;
        ApprovedVersionId = e.Evidence.ApprovedVersionId;
        ApproverPartyId = e.Evidence.ApproverPartyId;
        ApprovalPostingMessageId = e.Evidence.MessageId;
        ApprovalPostingIdempotencyKey = e.Evidence.IdempotencyKey;
    }

    /// <summary>Applies the posting-pending outcome for an approved proposal version.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyPostingPending e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalPostingPending;
        ProposalState = ProposedAgentReplyState.PostingPending;
        ProposalApprovalEvidence = e.Evidence;
    }

    /// <summary>Applies the posted outcome for an approved proposal version.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyPosted e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalPosted;
        ProposalState = ProposedAgentReplyState.Posted;
        ProposalApprovalEvidence = e.Evidence;
    }

    /// <summary>Applies an approval failure; no version is frozen for posting.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyApprovalFailed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalApprovalFailed;
        ProposalApprovalFailureReason = e.Reason;
        ProposalApprovalEvidence = e.Evidence;
    }

    /// <summary>Applies a posting failure for an approved proposal version.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyPostingFailed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalPostingFailed;
        ProposalState = ProposedAgentReplyState.PostingFailed;
        ProposalPostingFailureReason = e.Reason;
        ProposalApprovalEvidence = e.Evidence;
    }

    /// <summary>No-op replay handler — the not-regeneratable rejection carries no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProposedAgentReplyNotRegeneratableRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — the not-approvable rejection carries no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProposedAgentReplyNotApprovableRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>Applies the rejected outcome: records the terminal <see cref="AgentInteractionStatus.ProposalRejected"/> status, the <see cref="ProposedAgentReplyState.Rejected"/> sub-state, and the safe rejection evidence — every prior version is preserved (the version history is NOT touched) (AC1, AC4; Story 3.6; AD-5, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyRejected e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalRejected;
        ProposalState = ProposedAgentReplyState.Rejected;
        ProposalRejectionEvidence = e.Evidence;
    }

    /// <summary>Applies the abandoned outcome: records the terminal <see cref="AgentInteractionStatus.ProposalAbandoned"/> status, the <see cref="ProposedAgentReplyState.Abandoned"/> sub-state, and the safe abandonment evidence — every prior version is preserved (the version history is NOT touched) (AC2, AC4; Story 3.6; AD-5, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyAbandoned e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalAbandoned;
        ProposalState = ProposedAgentReplyState.Abandoned;
        ProposalAbandonmentEvidence = e.Evidence;
    }

    /// <summary>Applies the expired outcome: records the terminal <see cref="AgentInteractionStatus.ProposalExpired"/> status, the <see cref="ProposedAgentReplyState.Expired"/> sub-state, and the safe expiry evidence — every prior version is preserved (the version history is NOT touched) (AC3; Story 3.6; AD-3, AD-5, AD-14).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyExpired e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsRequested)
        {
            return;
        }

        Status = AgentInteractionStatus.ProposalExpired;
        ProposalState = ProposedAgentReplyState.Expired;
        ProposalExpiryEvidence = e.Evidence;
    }

    /// <summary>No-op replay handler — a fail-closed rejection-failed decision is durable Audit Evidence in the stream but leaves the pending proposal unchanged so an authorized Approver can retry (Story 3.6).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyRejectionFailed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — a fail-closed abandonment-failed decision is durable Audit Evidence in the stream but leaves the pending proposal unchanged so an authorized Approver can retry (Story 3.6).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProposedAgentReplyAbandonmentFailed e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — the not-rejectable rejection carries no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProposedAgentReplyNotRejectableRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — the not-abandonable rejection carries no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProposedAgentReplyNotAbandonableRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — the not-expirable rejection carries no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProposedAgentReplyNotExpirableRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(InvalidAgentInteractionRequestRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentInteractionAlreadyRequestedRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    private void MarkReplayOnlyEventHandled() => _ = AgentInteractionId;
}
