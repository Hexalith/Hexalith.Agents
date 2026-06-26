using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.FrontComposer.Shell.Components.Icons;

using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// Pure, dependency-free mapping from every canonical operational status (the shipped Story 4.1
/// <see cref="AgentReadinessStatus"/>, <see cref="ProviderModelReadinessStatus"/>,
/// <see cref="AgentCallOperationStatus"/>, <see cref="ProposalOperationStatus"/>, and
/// <see cref="AuditAvailabilityStatus"/>) onto its <see cref="RecoveryActionGroup"/>, Fluent semantic
/// <see cref="BadgeColor"/> role, curated <see cref="FcFluentIcons"/> icon, and whole-string label/guidance keys
/// (Story 4.3 AC1). Recovery guidance is grouped by the operator <em>action</em>, never the raw subsystem (UX-DR9), and
/// every guidance/label is a localized whole string with no runtime-assembled fragments (UX-DR14). Status binds to a
/// role, never a hex value (DESIGN Colors); <see cref="BadgeColor.Success"/> is used ONLY for a proven/posted/
/// approved-completion/audit-available/active state (UX-DR11) — never for pending/checking/posting-pending/audit-pending.
/// The mapping derives only the coarse content-free classification and never exposes any prompt, generated content,
/// secret, id, or PII (AD-14). Mirrors <see cref="AgentReadiness"/>/<see cref="AgentCallStatusPresentation"/>/
/// <see cref="ProposedAgentReplyStatePresentation"/> and is unit-testable in isolation (no bUnit). Every switch is
/// <b>total</b> so the <c>Unknown</c> sentinels render through a safe default.
/// </summary>
public static class OperationalStatusPresentation
{
    /// <summary>The whole-string localization key for a recovery-action group heading (the action name; UX-DR9/14).</summary>
    /// <param name="group">The recovery-action group.</param>
    /// <returns>The resource key.</returns>
    public static string GroupLabelKeyFor(RecoveryActionGroup group)
        => $"Agents.OperationalStatus.Group.{group}";

    /// <summary>The whole-string localization key for a recovery-action group's guidance sentence (names the action, not the subsystem; UX-DR9/14).</summary>
    /// <param name="group">The recovery-action group.</param>
    /// <returns>The resource key.</returns>
    public static string GuidanceKeyFor(RecoveryActionGroup group)
        => $"Agents.OperationalStatus.Recovery.{group}";

    // ---- Agent readiness (AgentReadinessStatus) ----------------------------------------------------------------

    /// <summary>Maps an Agent readiness status to its recovery-action group (blockers grouped by action; UX-DR9).</summary>
    /// <param name="status">The canonical Agent readiness status.</param>
    /// <returns>The recovery-action group.</returns>
    public static RecoveryActionGroup GroupFor(AgentReadinessStatus status)
        => status switch
        {
            AgentReadinessStatus.Callable => RecoveryActionGroup.None,
            AgentReadinessStatus.Checking => RecoveryActionGroup.None,
            AgentReadinessStatus.MissingPartyIdentity => RecoveryActionGroup.LinkPartyIdentity,
            AgentReadinessStatus.ProviderUnavailable => RecoveryActionGroup.ConfigureProvider,
            AgentReadinessStatus.InvalidConfiguration => RecoveryActionGroup.FixPolicy,
            AgentReadinessStatus.Disabled => RecoveryActionGroup.FixPolicy,
            _ => RecoveryActionGroup.None,
        };

    /// <summary>Maps an Agent readiness status to its Fluent badge role. Success is used ONLY for <see cref="AgentReadinessStatus.Callable"/>.</summary>
    /// <param name="status">The canonical Agent readiness status.</param>
    /// <returns>The Fluent badge color role.</returns>
    public static BadgeColor ColorFor(AgentReadinessStatus status)
        => status switch
        {
            AgentReadinessStatus.Callable => BadgeColor.Success,
            AgentReadinessStatus.Checking => BadgeColor.Informative,
            AgentReadinessStatus.MissingPartyIdentity => BadgeColor.Severe,
            AgentReadinessStatus.ProviderUnavailable => BadgeColor.Severe,
            AgentReadinessStatus.InvalidConfiguration => BadgeColor.Severe,
            AgentReadinessStatus.Disabled => BadgeColor.Severe,
            _ => BadgeColor.Subtle,
        };

    /// <summary>Maps an Agent readiness status to a curated icon.</summary>
    /// <param name="status">The canonical Agent readiness status.</param>
    /// <returns>The Fluent icon.</returns>
    public static Icon IconFor(AgentReadinessStatus status)
        => status switch
        {
            AgentReadinessStatus.Callable => FcFluentIcons.Checkmark16(),
            AgentReadinessStatus.Checking => FcFluentIcons.ArrowSync16(),
            AgentReadinessStatus.Disabled => FcFluentIcons.SubtractCircle16(),
            AgentReadinessStatus.MissingPartyIdentity => FcFluentIcons.Warning16(),
            AgentReadinessStatus.ProviderUnavailable => FcFluentIcons.Warning16(),
            AgentReadinessStatus.InvalidConfiguration => FcFluentIcons.Warning16(),
            _ => FcFluentIcons.QuestionCircle16(),
        };

    /// <summary>The whole-string localization key for an Agent readiness status label (UX-DR14).</summary>
    /// <param name="status">The canonical Agent readiness status.</param>
    /// <returns>The resource key.</returns>
    public static string LabelKeyFor(AgentReadinessStatus status)
        => $"Agents.OperationalStatus.AgentReadiness.{status}";

    /// <summary>Maps a readiness status to the canonical UI readiness state so the shipped <see cref="AgentReadinessBadge"/> can render it.</summary>
    /// <param name="status">The canonical Agent readiness status.</param>
    /// <returns>The UI readiness state.</returns>
    public static AgentReadinessState ToReadinessState(AgentReadinessStatus status)
        => status switch
        {
            AgentReadinessStatus.Callable => AgentReadinessState.Callable,
            AgentReadinessStatus.Checking => AgentReadinessState.Checking,
            AgentReadinessStatus.InvalidConfiguration => AgentReadinessState.InvalidConfiguration,
            AgentReadinessStatus.MissingPartyIdentity => AgentReadinessState.MissingPartyIdentity,
            AgentReadinessStatus.ProviderUnavailable => AgentReadinessState.ProviderUnavailable,
            AgentReadinessStatus.Disabled => AgentReadinessState.Disabled,
            _ => AgentReadinessState.Unknown,
        };

    // ---- Activation blockers (AgentActivationBlocker) ----------------------------------------------------------

    /// <summary>Maps an activation blocker to its recovery-action group (group by action, not subsystem; UX-DR9).</summary>
    /// <param name="blocker">The activation blocker.</param>
    /// <returns>The recovery-action group.</returns>
    public static RecoveryActionGroup GroupForBlocker(AgentActivationBlocker blocker)
        => blocker switch
        {
            AgentActivationBlocker.MissingPartyIdentity => RecoveryActionGroup.LinkPartyIdentity,
            AgentActivationBlocker.MissingProviderSelection => RecoveryActionGroup.ConfigureProvider,
            AgentActivationBlocker.ProviderUnavailable => RecoveryActionGroup.ConfigureProvider,
            AgentActivationBlocker.MissingApproverPolicy => RecoveryActionGroup.FixPolicy,
            AgentActivationBlocker.ApproverPolicyUnresolvable => RecoveryActionGroup.FixPolicy,
            AgentActivationBlocker.MissingDisplayName => RecoveryActionGroup.FixPolicy,
            AgentActivationBlocker.MissingInstructions => RecoveryActionGroup.FixPolicy,
            AgentActivationBlocker.InvalidInstructions => RecoveryActionGroup.FixPolicy,
            AgentActivationBlocker.MissingResponseMode => RecoveryActionGroup.FixPolicy,
            AgentActivationBlocker.MissingContentSafetyPolicy => RecoveryActionGroup.FixPolicy,
            _ => RecoveryActionGroup.FixPolicy,
        };

    // ---- Provider/model readiness (ProviderModelReadinessStatus) -----------------------------------------------

    /// <summary>Maps a provider/model readiness status to its recovery-action group (always configure provider).</summary>
    /// <param name="status">The canonical provider/model readiness status.</param>
    /// <returns>The recovery-action group.</returns>
    public static RecoveryActionGroup GroupFor(ProviderModelReadinessStatus status)
        => status switch
        {
            ProviderModelReadinessStatus.Enabled => RecoveryActionGroup.None,
            _ => RecoveryActionGroup.ConfigureProvider,
        };

    /// <summary>Maps a provider/model readiness status to its Fluent badge role. Success is used ONLY for <see cref="ProviderModelReadinessStatus.Enabled"/>.</summary>
    /// <param name="status">The canonical provider/model readiness status.</param>
    /// <returns>The Fluent badge color role.</returns>
    public static BadgeColor ColorFor(ProviderModelReadinessStatus status)
        => status switch
        {
            ProviderModelReadinessStatus.Enabled => BadgeColor.Success,
            ProviderModelReadinessStatus.Degraded => BadgeColor.Warning,
            ProviderModelReadinessStatus.Failed => BadgeColor.Danger,
            ProviderModelReadinessStatus.Disabled => BadgeColor.Severe,
            ProviderModelReadinessStatus.NotConfigured => BadgeColor.Severe,
            _ => BadgeColor.Subtle,
        };

    /// <summary>Maps a provider/model readiness status to a curated icon.</summary>
    /// <param name="status">The canonical provider/model readiness status.</param>
    /// <returns>The Fluent icon.</returns>
    public static Icon IconFor(ProviderModelReadinessStatus status)
        => status switch
        {
            ProviderModelReadinessStatus.Enabled => FcFluentIcons.Checkmark16(),
            ProviderModelReadinessStatus.Disabled => FcFluentIcons.SubtractCircle16(),
            ProviderModelReadinessStatus.Degraded => FcFluentIcons.Warning16(),
            ProviderModelReadinessStatus.Failed => FcFluentIcons.Warning16(),
            ProviderModelReadinessStatus.NotConfigured => FcFluentIcons.Warning16(),
            _ => FcFluentIcons.QuestionCircle16(),
        };

    /// <summary>The whole-string localization key for a provider/model readiness status label (UX-DR14).</summary>
    /// <param name="status">The canonical provider/model readiness status.</param>
    /// <returns>The resource key.</returns>
    public static string LabelKeyFor(ProviderModelReadinessStatus status)
        => $"Agents.OperationalStatus.ProviderReadiness.{status}";

    // ---- Agent call outcomes (AgentCallOperationStatus) --------------------------------------------------------

    /// <summary>Maps an Agent Call status to its recovery-action group (authorization → fix policy; context block → start new call; generation failure → retry).</summary>
    /// <param name="status">The canonical Agent Call status.</param>
    /// <returns>The recovery-action group.</returns>
    public static RecoveryActionGroup GroupFor(AgentCallOperationStatus status)
        => status switch
        {
            AgentCallOperationStatus.Denied => RecoveryActionGroup.FixPolicy,
            AgentCallOperationStatus.ContextBlocked => RecoveryActionGroup.StartNewCall,
            AgentCallOperationStatus.GenerationFailed => RecoveryActionGroup.RetryGeneration,
            _ => RecoveryActionGroup.None,
        };

    /// <summary>Maps an Agent Call status to its Fluent badge role. In-flight is Informative; failures Danger; blocks Severe; no Success in this enum.</summary>
    /// <param name="status">The canonical Agent Call status.</param>
    /// <returns>The Fluent badge color role.</returns>
    public static BadgeColor ColorFor(AgentCallOperationStatus status)
        => status switch
        {
            AgentCallOperationStatus.Requested => BadgeColor.Informative,
            AgentCallOperationStatus.Authorized => BadgeColor.Informative,
            AgentCallOperationStatus.ContextLoading => BadgeColor.Informative,
            AgentCallOperationStatus.Generating => BadgeColor.Informative,
            AgentCallOperationStatus.Generated => BadgeColor.Informative,
            AgentCallOperationStatus.ContextBlocked => BadgeColor.Severe,
            AgentCallOperationStatus.Denied => BadgeColor.Danger,
            AgentCallOperationStatus.GenerationFailed => BadgeColor.Danger,
            _ => BadgeColor.Subtle,
        };

    /// <summary>Maps an Agent Call status to a curated icon.</summary>
    /// <param name="status">The canonical Agent Call status.</param>
    /// <returns>The Fluent icon.</returns>
    public static Icon IconFor(AgentCallOperationStatus status)
        => status switch
        {
            AgentCallOperationStatus.Requested => FcFluentIcons.Play16(),
            AgentCallOperationStatus.Authorized => FcFluentIcons.ArrowSync16(),
            AgentCallOperationStatus.ContextLoading => FcFluentIcons.ArrowSync16(),
            AgentCallOperationStatus.Generating => FcFluentIcons.ArrowSync16(),
            AgentCallOperationStatus.Generated => FcFluentIcons.Checkmark16(),
            AgentCallOperationStatus.Denied => FcFluentIcons.Key16(),
            AgentCallOperationStatus.ContextBlocked => FcFluentIcons.SubtractCircle16(),
            AgentCallOperationStatus.GenerationFailed => FcFluentIcons.Warning16(),
            _ => FcFluentIcons.QuestionCircle16(),
        };

    /// <summary>The whole-string localization key for an Agent Call status label (UX-DR14).</summary>
    /// <param name="status">The canonical Agent Call status.</param>
    /// <returns>The resource key.</returns>
    public static string LabelKeyFor(AgentCallOperationStatus status)
        => $"Agents.OperationalStatus.CallStatus.{status}";

    // ---- Proposal/posting outcomes (ProposalOperationStatus) ---------------------------------------------------

    /// <summary>Maps a proposal/posting status to its recovery-action group (pending → wait; terminal → start new call; posting failure → retry).</summary>
    /// <param name="status">The canonical proposal/posting status.</param>
    /// <returns>The recovery-action group.</returns>
    public static RecoveryActionGroup GroupFor(ProposalOperationStatus status)
        => status switch
        {
            ProposalOperationStatus.PendingApproval => RecoveryActionGroup.WaitForApproval,
            ProposalOperationStatus.Rejected => RecoveryActionGroup.StartNewCall,
            ProposalOperationStatus.Abandoned => RecoveryActionGroup.StartNewCall,
            ProposalOperationStatus.Expired => RecoveryActionGroup.StartNewCall,
            ProposalOperationStatus.PostingFailed => RecoveryActionGroup.RetryGeneration,
            _ => RecoveryActionGroup.None,
        };

    /// <summary>Maps a proposal/posting status to its Fluent badge role. Success is used ONLY for <see cref="ProposalOperationStatus.Posted"/>; <see cref="ProposalOperationStatus.Approved"/> is Important (approved ≠ posted; AD-5).</summary>
    /// <param name="status">The canonical proposal/posting status.</param>
    /// <returns>The Fluent badge color role.</returns>
    public static BadgeColor ColorFor(ProposalOperationStatus status)
        => status switch
        {
            ProposalOperationStatus.Posted => BadgeColor.Success,
            ProposalOperationStatus.Approved => BadgeColor.Important,
            ProposalOperationStatus.Generated => BadgeColor.Informative,
            ProposalOperationStatus.Edited => BadgeColor.Informative,
            ProposalOperationStatus.Regenerated => BadgeColor.Informative,
            ProposalOperationStatus.PendingApproval => BadgeColor.Informative,
            ProposalOperationStatus.PostingPending => BadgeColor.Informative,
            ProposalOperationStatus.Expired => BadgeColor.Severe,
            ProposalOperationStatus.Rejected => BadgeColor.Danger,
            ProposalOperationStatus.PostingFailed => BadgeColor.Danger,
            ProposalOperationStatus.Abandoned => BadgeColor.Subtle,
            _ => BadgeColor.Subtle,
        };

    /// <summary>Maps a proposal/posting status to a curated icon.</summary>
    /// <param name="status">The canonical proposal/posting status.</param>
    /// <returns>The Fluent icon.</returns>
    public static Icon IconFor(ProposalOperationStatus status)
        => status switch
        {
            ProposalOperationStatus.Generated => FcFluentIcons.Checkmark16(),
            ProposalOperationStatus.Edited => FcFluentIcons.Edit16(),
            ProposalOperationStatus.Regenerated => FcFluentIcons.ArrowSync16(),
            ProposalOperationStatus.PendingApproval => FcFluentIcons.ArrowSync16(),
            ProposalOperationStatus.Approved => FcFluentIcons.Checkmark16(),
            ProposalOperationStatus.PostingPending => FcFluentIcons.ArrowSync16(),
            ProposalOperationStatus.Posted => FcFluentIcons.Checkmark16(),
            ProposalOperationStatus.PostingFailed => FcFluentIcons.Warning16(),
            ProposalOperationStatus.Rejected => FcFluentIcons.SubtractCircle16(),
            ProposalOperationStatus.Abandoned => FcFluentIcons.SubtractCircle16(),
            ProposalOperationStatus.Expired => FcFluentIcons.Warning16(),
            _ => FcFluentIcons.QuestionCircle16(),
        };

    /// <summary>The whole-string localization key for a proposal/posting status label (UX-DR14).</summary>
    /// <param name="status">The canonical proposal/posting status.</param>
    /// <returns>The resource key.</returns>
    public static string LabelKeyFor(ProposalOperationStatus status)
        => $"Agents.OperationalStatus.ProposalStatus.{status}";

    // ---- Audit availability (AuditAvailabilityStatus) ----------------------------------------------------------

    /// <summary>Maps an audit availability status to its recovery-action group (always inspect audit, for non-trivial states).</summary>
    /// <param name="status">The canonical audit availability status.</param>
    /// <returns>The recovery-action group.</returns>
    public static RecoveryActionGroup GroupFor(AuditAvailabilityStatus status)
        => status switch
        {
            AuditAvailabilityStatus.Unknown => RecoveryActionGroup.None,
            _ => RecoveryActionGroup.InspectAudit,
        };

    /// <summary>Maps an audit availability status to its Fluent badge role. Success is used ONLY for <see cref="AuditAvailabilityStatus.AuditAvailable"/> — pending/delayed/unavailable are never success (AD-5).</summary>
    /// <param name="status">The canonical audit availability status.</param>
    /// <returns>The Fluent badge color role.</returns>
    public static BadgeColor ColorFor(AuditAvailabilityStatus status)
        => status switch
        {
            AuditAvailabilityStatus.AuditAvailable => BadgeColor.Success,
            AuditAvailabilityStatus.AuditPending => BadgeColor.Informative,
            AuditAvailabilityStatus.AuditDelayed => BadgeColor.Warning,
            AuditAvailabilityStatus.AuditUnavailable => BadgeColor.Severe,
            _ => BadgeColor.Subtle,
        };

    /// <summary>Maps an audit availability status to a curated icon.</summary>
    /// <param name="status">The canonical audit availability status.</param>
    /// <returns>The Fluent icon.</returns>
    public static Icon IconFor(AuditAvailabilityStatus status)
        => status switch
        {
            AuditAvailabilityStatus.AuditAvailable => FcFluentIcons.Checkmark16(),
            AuditAvailabilityStatus.AuditPending => FcFluentIcons.ArrowSync16(),
            AuditAvailabilityStatus.AuditDelayed => FcFluentIcons.Warning16(),
            AuditAvailabilityStatus.AuditUnavailable => FcFluentIcons.SubtractCircle16(),
            _ => FcFluentIcons.QuestionCircle16(),
        };

    /// <summary>The whole-string localization key for an audit availability status label (shared with <see cref="AuditAvailabilityBadge"/>; UX-DR14).</summary>
    /// <param name="status">The canonical audit availability status.</param>
    /// <returns>The resource key.</returns>
    public static string LabelKeyFor(AuditAvailabilityStatus status)
        => $"Agents.Audit.Availability.{status}";
}
