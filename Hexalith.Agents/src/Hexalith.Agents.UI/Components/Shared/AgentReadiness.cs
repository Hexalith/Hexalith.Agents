using System.Collections.Generic;
using System.Linq;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;

using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// The canonical UX readiness state of a governed Agent (<c>hexa</c>) derived from its safe
/// <see cref="AgentStatusView"/> (AC2; UX-DR20/25). Lifecycle (active) and callability are deliberately distinct:
/// <see cref="Callable"/> is the only state that may use the Success role, and only when the Agent is
/// <see cref="AgentLifecycleStatus.Active"/> with no <see cref="AgentActivationBlocker"/>.
/// </summary>
public enum AgentReadinessState
{
    /// <summary>UI-only transient while a readiness (re)evaluation is in flight — no contract field backs it.</summary>
    Checking,

    /// <summary>Active lifecycle AND no activation blockers — the only Success state.</summary>
    Callable,

    /// <summary>The Agent is disabled.</summary>
    Disabled,

    /// <summary>No valid Party identity is linked.</summary>
    MissingPartyIdentity,

    /// <summary>No Provider/model is selected, or the selected one is unavailable.</summary>
    ProviderUnavailable,

    /// <summary>A required configuration field is missing or invalid.</summary>
    InvalidConfiguration,

    /// <summary>An ambiguous/unresolved dependency must be resolved before any side effect (the Important role).</summary>
    Unresolved,

    /// <summary>Readiness cannot be determined from the available state.</summary>
    Unknown,
}

/// <summary>
/// The canonical UX status of a provider/model catalog entry derived from its safe
/// <see cref="ProviderCatalogEntryView"/> (AC3; UX-DR21/26).
/// </summary>
public enum ProviderReadinessState
{
    /// <summary>Enabled and configured — the only Success state.</summary>
    Enabled,

    /// <summary>No safe configuration reference is associated with the entry.</summary>
    NotConfigured,

    /// <summary>Disabled and never configured — not selectable for new active use.</summary>
    Disabled,

    /// <summary>Disabled but previously configured — a quiet, non-actionable historical selection.</summary>
    HistoricalSelection,

    /// <summary>Degraded (reserved runtime-health state).</summary>
    Degraded,

    /// <summary>Failed (reserved runtime-health state).</summary>
    Failed,

    /// <summary>Status cannot be determined from the available state.</summary>
    Unknown,
}

/// <summary>
/// Pure, dependency-free mapping from the safe display contracts to the canonical readiness/provider states and
/// their Fluent semantic roles + localization keys (AC2, AC3, AC5). Bind status to a role, never to a hex value
/// (DESIGN Colors). Success is used ONLY for a proven-callable Agent / enabled+configured provider (UX-DR11). The
/// mapping never derives or exposes any secret, id, or PII (AD-14).
/// </summary>
public static class AgentReadiness
{
    /// <summary>The activation blockers that map to the generic "invalid configuration" readiness state.</summary>
    private static readonly HashSet<AgentActivationBlocker> _invalidConfigurationBlockers =
    [
        AgentActivationBlocker.MissingDisplayName,
        AgentActivationBlocker.MissingInstructions,
        AgentActivationBlocker.InvalidInstructions,
        AgentActivationBlocker.MissingResponseMode,
        AgentActivationBlocker.MissingApproverPolicy,
        AgentActivationBlocker.MissingContentSafetyPolicy,
    ];

    /// <summary>
    /// Maps an Agent status view to its canonical readiness state. The active-vs-callable distinction is never
    /// collapsed: <see cref="AgentReadinessState.Callable"/> requires <see cref="AgentLifecycleStatus.Active"/> AND
    /// an empty blocker set (UX-DR20).
    /// </summary>
    /// <param name="agent">The safe Agent status view.</param>
    /// <returns>The canonical readiness state.</returns>
    public static AgentReadinessState MapState(AgentStatusView agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        if (agent.Lifecycle == AgentLifecycleStatus.Disabled)
        {
            return AgentReadinessState.Disabled;
        }

        IReadOnlyList<AgentActivationBlocker> blockers = agent.ActivationBlockers;

        if (agent.Lifecycle == AgentLifecycleStatus.Active && blockers.Count == 0)
        {
            return AgentReadinessState.Callable;
        }

        if (blockers.Contains(AgentActivationBlocker.MissingPartyIdentity))
        {
            return AgentReadinessState.MissingPartyIdentity;
        }

        if (blockers.Contains(AgentActivationBlocker.MissingProviderSelection)
            || blockers.Contains(AgentActivationBlocker.ProviderUnavailable))
        {
            return AgentReadinessState.ProviderUnavailable;
        }

        // The ambiguous/unresolved dependency surfaces as the Important role (resolve before side effects).
        if (blockers.Contains(AgentActivationBlocker.ApproverPolicyUnresolvable))
        {
            return AgentReadinessState.Unresolved;
        }

        if (blockers.Any(_invalidConfigurationBlockers.Contains))
        {
            return AgentReadinessState.InvalidConfiguration;
        }

        // No recognized blocker but not active (e.g. a fully-configured draft, or an unrecognized blocker).
        return AgentReadinessState.Unknown;
    }

    /// <summary>Whether the Agent is callable for the tenant (active lifecycle AND no activation blockers; AC2).</summary>
    /// <param name="agent">The safe Agent status view.</param>
    /// <returns><see langword="true"/> only when active with no blockers.</returns>
    public static bool IsCallable(AgentStatusView agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        return agent.Lifecycle == AgentLifecycleStatus.Active && agent.ActivationBlockers.Count == 0;
    }

    /// <summary>Maps a readiness state to its Fluent semantic badge role. Success is used ONLY for callable (UX-DR11).</summary>
    /// <param name="state">The canonical readiness state.</param>
    /// <returns>The Fluent badge color role.</returns>
    public static BadgeColor ColorFor(AgentReadinessState state)
        => state switch
        {
            AgentReadinessState.Callable => BadgeColor.Success,
            AgentReadinessState.Checking => BadgeColor.Informative,
            AgentReadinessState.Unresolved => BadgeColor.Important,
            AgentReadinessState.Disabled => BadgeColor.Severe,
            AgentReadinessState.MissingPartyIdentity => BadgeColor.Severe,
            AgentReadinessState.ProviderUnavailable => BadgeColor.Severe,
            AgentReadinessState.InvalidConfiguration => BadgeColor.Severe,
            AgentReadinessState.Unknown => BadgeColor.Subtle,
            _ => BadgeColor.Subtle,
        };

    /// <summary>The whole-string localization key for a readiness state label (UX-DR14).</summary>
    /// <param name="state">The canonical readiness state.</param>
    /// <returns>The resource key.</returns>
    public static string LabelKeyFor(AgentReadinessState state)
        => $"Agents.Readiness.State.{state}";

    /// <summary>The whole-string localization key for an activation-blocker reason (one key per blocker; UX-DR14).</summary>
    /// <param name="blocker">The activation blocker.</param>
    /// <returns>The resource key.</returns>
    public static string BlockerKeyFor(AgentActivationBlocker blocker)
        => $"Agents.Readiness.Blocker.{blocker}";

    /// <summary>
    /// Maps a provider/model entry view to its canonical provider status. Runtime-health states win over the
    /// configured/enabled gate; a disabled-but-configured entry is treated as a quiet historical selection
    /// (UX-DR21/26).
    /// </summary>
    /// <param name="entry">The safe provider/model entry view.</param>
    /// <returns>The canonical provider status.</returns>
    public static ProviderReadinessState MapProviderState(ProviderCatalogEntryView entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return entry.Status switch
        {
            ProviderModelStatus.Failed => ProviderReadinessState.Failed,
            ProviderModelStatus.Degraded => ProviderReadinessState.Degraded,
            ProviderModelStatus.Disabled => entry.ConfigurationState == ProviderConfigurationState.Configured
                ? ProviderReadinessState.HistoricalSelection
                : ProviderReadinessState.Disabled,
            _ when entry.ConfigurationState != ProviderConfigurationState.Configured => ProviderReadinessState.NotConfigured,
            ProviderModelStatus.Enabled => ProviderReadinessState.Enabled,
            _ => ProviderReadinessState.Unknown,
        };
    }

    /// <summary>Maps a provider status to its Fluent semantic badge role. Success is used ONLY for enabled+configured.</summary>
    /// <param name="state">The canonical provider status.</param>
    /// <returns>The Fluent badge color role.</returns>
    public static BadgeColor ColorFor(ProviderReadinessState state)
        => state switch
        {
            ProviderReadinessState.Enabled => BadgeColor.Success,
            ProviderReadinessState.NotConfigured => BadgeColor.Severe,
            ProviderReadinessState.Disabled => BadgeColor.Severe,
            ProviderReadinessState.Degraded => BadgeColor.Warning,
            ProviderReadinessState.Failed => BadgeColor.Danger,
            ProviderReadinessState.HistoricalSelection => BadgeColor.Subtle,
            ProviderReadinessState.Unknown => BadgeColor.Subtle,
            _ => BadgeColor.Subtle,
        };

    /// <summary>The whole-string localization key for a provider status label (UX-DR14).</summary>
    /// <param name="state">The canonical provider status.</param>
    /// <returns>The resource key.</returns>
    public static string LabelKeyFor(ProviderReadinessState state)
        => $"Agents.ProviderCatalog.Status.{state}";
}
