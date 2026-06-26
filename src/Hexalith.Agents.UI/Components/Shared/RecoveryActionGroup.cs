namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// The canonical "group by recovery action" set for the operational-status surface (Story 4.3 AC1; EXPERIENCE
/// operational-status-panel). Readiness blockers and runtime failures are grouped by the <em>action</em> an operator
/// takes to recover — never by the raw subsystem that emitted the status (UX-DR9). Ordered from setup → workflow →
/// recovery so the panel renders a coherent operational-setup-to-recovery flow. <see cref="None"/> is the terminal
/// "no recovery action needed" group for proven/healthy/in-flight states; the enum is deliberately
/// <c>Unknown</c>-free (a closed UI vocabulary).
/// </summary>
public enum RecoveryActionGroup
{
    /// <summary>Select/enable a Provider and model for <c>hexa</c> (provider/model readiness blockers).</summary>
    ConfigureProvider,

    /// <summary>Link a valid Party identity to <c>hexa</c> (missing-party-identity blocker).</summary>
    LinkPartyIdentity,

    /// <summary>Review the Agent configuration and approver policy (configuration/policy blockers, authorization denials).</summary>
    FixPolicy,

    /// <summary>Wait for an approver to act on a pending proposal (pending-approval states).</summary>
    WaitForApproval,

    /// <summary>Retry generation or posting after a generation/content-safety/posting failure.</summary>
    RetryGeneration,

    /// <summary>Inspect the support-safe audit evidence (audit availability states, posting failures).</summary>
    InspectAudit,

    /// <summary>Start a new Agent Call (context-policy blocks, terminal proposals that can no longer post).</summary>
    StartNewCall,

    /// <summary>No recovery action is required — a proven/healthy/in-flight state.</summary>
    None,
}
