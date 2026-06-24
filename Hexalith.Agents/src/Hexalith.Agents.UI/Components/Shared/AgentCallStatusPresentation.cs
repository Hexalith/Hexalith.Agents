using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.FrontComposer.Shell.Components.Icons;

using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// Pure, dependency-free mapping from the safe durable <see cref="AgentInteractionStatus"/> (and a transient in-flight
/// hint) to the canonical <see cref="AgentCallStatus"/> UX state and its Fluent semantic role + icon + whole-string
/// localization keys (AC1, AC2, AC3). Bind status to a role, never to a hex value (DESIGN Colors). Success is used ONLY
/// for the proven-complete <see cref="AgentCallStatus.Posted"/> state (UX-DR11); Brand is never a status. The mapping
/// derives only the coarse, content-free classification and never exposes any prompt, generated content, secret, id, or
/// PII (AD-14). Mirrors <see cref="AgentReadiness"/> and is unit-testable in isolation (no bUnit).
/// </summary>
public static class AgentCallStatusPresentation
{
    /// <summary>
    /// Maps a durable Agent Call status to its canonical UX state. The mapping is total and never invents a transient
    /// state: <c>ContextReady</c> resolves to <see cref="AgentCallStatus.Generating"/> because, once context is ready,
    /// generation is the next in-flight step. Use <see cref="Derive"/> to render the UI-only transient states.
    /// </summary>
    /// <param name="status">The safe durable Agent Call status.</param>
    /// <returns>The canonical UX call-status state.</returns>
    public static AgentCallStatus MapStatus(AgentInteractionStatus status)
        => status switch
        {
            AgentInteractionStatus.Requested => AgentCallStatus.Requested,
            AgentInteractionStatus.Authorized => AgentCallStatus.Authorized,
            AgentInteractionStatus.Denied => AgentCallStatus.Denied,
            AgentInteractionStatus.Blocked => AgentCallStatus.Blocked,
            AgentInteractionStatus.ContextReady => AgentCallStatus.Generating,
            AgentInteractionStatus.ContextBlocked => AgentCallStatus.ContextBlocked,
            AgentInteractionStatus.Generated => AgentCallStatus.Generated,
            AgentInteractionStatus.GenerationFailed => AgentCallStatus.GenerationFailed,
            AgentInteractionStatus.SafetyFailed => AgentCallStatus.SafetyFailed,
            AgentInteractionStatus.Posted => AgentCallStatus.Posted,
            AgentInteractionStatus.PostingFailed => AgentCallStatus.PostingFailed,
            _ => AgentCallStatus.Unknown,
        };

    /// <summary>
    /// Derives the canonical UX state including the UI-only transient states while the next durable milestone is
    /// pending (AD-3). When <paramref name="inFlight"/> is set, <c>Authorized</c> renders the transient
    /// <see cref="AgentCallStatus.ContextLoading"/> and a <c>Generated</c> + <see cref="AgentResponseMode.Automatic"/>
    /// call renders the transient <see cref="AgentCallStatus.PostingPending"/>. <c>ContextReady</c> already maps to the
    /// transient-looking <see cref="AgentCallStatus.Generating"/> via <see cref="MapStatus(AgentInteractionStatus)"/>.
    /// The derivation is explicit — it never silently invents a state when no milestone is pending.
    /// </summary>
    /// <param name="status">The safe durable Agent Call status.</param>
    /// <param name="inFlight">Whether the next milestone after <paramref name="status"/> is currently in flight.</param>
    /// <param name="responseMode">The snapshotted Response Mode (only Automatic derives <see cref="AgentCallStatus.PostingPending"/>).</param>
    /// <returns>The canonical UX call-status state, including transient in-flight states.</returns>
    public static AgentCallStatus Derive(AgentInteractionStatus status, bool inFlight, AgentResponseMode responseMode)
    {
        if (!inFlight)
        {
            return MapStatus(status);
        }

        return status switch
        {
            AgentInteractionStatus.Authorized => AgentCallStatus.ContextLoading,
            AgentInteractionStatus.Generated when responseMode == AgentResponseMode.Automatic => AgentCallStatus.PostingPending,
            _ => MapStatus(status),
        };
    }

    /// <summary>
    /// Maps a UX call-status to its Fluent semantic badge role. Success is used ONLY for <see cref="AgentCallStatus.Posted"/>
    /// (proven complete; UX-DR11); in-flight/progress is Informative; blocked-but-not-failed is Severe; failures are
    /// Danger; the sentinel is Subtle. Brand is never used for a status.
    /// </summary>
    /// <param name="state">The canonical UX call-status state.</param>
    /// <returns>The Fluent badge color role.</returns>
    public static BadgeColor ColorFor(AgentCallStatus state)
        => state switch
        {
            AgentCallStatus.Posted => BadgeColor.Success,
            AgentCallStatus.Requested => BadgeColor.Informative,
            AgentCallStatus.Authorized => BadgeColor.Informative,
            AgentCallStatus.ContextLoading => BadgeColor.Informative,
            AgentCallStatus.Generating => BadgeColor.Informative,
            AgentCallStatus.Generated => BadgeColor.Informative,
            AgentCallStatus.PostingPending => BadgeColor.Informative,
            AgentCallStatus.Blocked => BadgeColor.Severe,
            AgentCallStatus.ContextBlocked => BadgeColor.Severe,
            AgentCallStatus.Denied => BadgeColor.Danger,
            AgentCallStatus.GenerationFailed => BadgeColor.Danger,
            AgentCallStatus.SafetyFailed => BadgeColor.Danger,
            AgentCallStatus.PostingFailed => BadgeColor.Danger,
            AgentCallStatus.Unknown => BadgeColor.Subtle,
            _ => BadgeColor.Subtle,
        };

    /// <summary>
    /// Maps a UX call-status to its icon, composed ONLY from the curated <see cref="FcFluentIcons"/> factory. There is
    /// no error/dismiss glyph in the curated set, so failures reuse <see cref="FcFluentIcons.Warning16"/>, exactly as the
    /// readiness/provider badges do.
    /// </summary>
    /// <param name="state">The canonical UX call-status state.</param>
    /// <returns>The Fluent icon for the state.</returns>
    public static Icon IconFor(AgentCallStatus state)
        => state switch
        {
            AgentCallStatus.Requested => FcFluentIcons.Play16(),
            AgentCallStatus.Authorized => FcFluentIcons.ArrowSync16(),
            AgentCallStatus.ContextLoading => FcFluentIcons.ArrowSync16(),
            AgentCallStatus.Generating => FcFluentIcons.ArrowSync16(),
            AgentCallStatus.PostingPending => FcFluentIcons.ArrowSync16(),
            AgentCallStatus.Generated => FcFluentIcons.Checkmark16(),
            AgentCallStatus.Posted => FcFluentIcons.Checkmark16(),
            AgentCallStatus.Denied => FcFluentIcons.Key16(),
            AgentCallStatus.Blocked => FcFluentIcons.SubtractCircle16(),
            AgentCallStatus.ContextBlocked => FcFluentIcons.SubtractCircle16(),
            AgentCallStatus.GenerationFailed => FcFluentIcons.Warning16(),
            AgentCallStatus.SafetyFailed => FcFluentIcons.Warning16(),
            AgentCallStatus.PostingFailed => FcFluentIcons.Warning16(),
            _ => FcFluentIcons.QuestionCircle16(),
        };

    /// <summary>The whole-string localization key for a call-status label (UX-DR14).</summary>
    /// <param name="state">The canonical UX call-status state.</param>
    /// <returns>The resource key.</returns>
    public static string LabelKeyFor(AgentCallStatus state)
        => $"Agents.CallStatus.Status.{state}";

    /// <summary>
    /// The coarse, status-level safe reason key for the failure/blocked states (AC3 primary path). It needs only the
    /// coarse <c>Status</c> the safe status view already carries — never a fine-grained reason, raw error, content,
    /// secret, or id (AD-14). Returns <see langword="null"/> for non-failure states, which carry no reason.
    /// </summary>
    /// <param name="state">The canonical UX call-status state.</param>
    /// <returns>The safe whole-string reason key, or <see langword="null"/> when the state has no reason.</returns>
    public static string? ReasonKeyFor(AgentCallStatus state)
        => state switch
        {
            AgentCallStatus.Denied => $"Agents.CallStatus.Reason.{AgentCallStatus.Denied}",
            AgentCallStatus.Blocked => $"Agents.CallStatus.Reason.{AgentCallStatus.Blocked}",
            AgentCallStatus.ContextBlocked => $"Agents.CallStatus.Reason.{AgentCallStatus.ContextBlocked}",
            AgentCallStatus.SafetyFailed => $"Agents.CallStatus.Reason.{AgentCallStatus.SafetyFailed}",
            AgentCallStatus.GenerationFailed => $"Agents.CallStatus.Reason.{AgentCallStatus.GenerationFailed}",
            AgentCallStatus.PostingFailed => $"Agents.CallStatus.Reason.{AgentCallStatus.PostingFailed}",
            _ => null,
        };

    /// <summary>
    /// The fine-grained safe reason key for a context-block reason — usable WHEN a richer reason is available (e.g. a
    /// future read model that projects a safe reason onto the view). Derives no secret/id/PII (AD-14).
    /// </summary>
    /// <param name="reason">The safe context-block reason.</param>
    /// <returns>The safe whole-string reason key.</returns>
    public static string ReasonKeyFor(AgentInteractionContextBlockReason reason)
        => $"Agents.CallStatus.Reason.{reason}";

    /// <summary>The fine-grained safe reason key for a generation-failure reason (additive; AD-14).</summary>
    /// <param name="reason">The safe generation-failure reason.</param>
    /// <returns>The safe whole-string reason key.</returns>
    public static string ReasonKeyFor(AgentOutputGenerationFailureReason reason)
        => $"Agents.CallStatus.Reason.{reason}";

    /// <summary>The fine-grained safe reason key for a posting-failure reason (additive; AD-14).</summary>
    /// <param name="reason">The safe posting-failure reason.</param>
    /// <returns>The safe whole-string reason key.</returns>
    public static string ReasonKeyFor(AgentResponsePostingFailureReason reason)
        => $"Agents.CallStatus.Reason.{reason}";

    /// <summary>The fine-grained safe reason key for an invocation-gate outcome (additive; AD-14).</summary>
    /// <param name="outcome">The safe gate outcome.</param>
    /// <returns>The safe whole-string reason key.</returns>
    public static string ReasonKeyFor(AgentInteractionGateOutcome outcome)
        => $"Agents.CallStatus.Reason.{outcome}";

    /// <summary>The fine-grained safe reason key for an invocation-gate check (additive; AD-14).</summary>
    /// <param name="check">The safe gate check.</param>
    /// <returns>The safe whole-string reason key.</returns>
    public static string ReasonKeyFor(AgentInteractionGateCheck check)
        => $"Agents.CallStatus.Reason.{check}";
}
