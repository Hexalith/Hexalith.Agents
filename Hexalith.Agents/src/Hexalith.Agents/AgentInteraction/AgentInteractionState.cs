using System;

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
