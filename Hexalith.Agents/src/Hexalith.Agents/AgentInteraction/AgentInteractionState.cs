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
