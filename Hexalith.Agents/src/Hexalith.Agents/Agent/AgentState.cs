using System;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

namespace Hexalith.Agents.Agent;

/// <summary>
/// Replay state for one governed Agent (<c>hexa</c>) aggregate (AD-2 aggregate boundary; aggregate id =
/// <see cref="AgentId"/> = the command envelope's aggregate id). State changes only through the <c>Apply</c>
/// methods (AD-3); no-op <c>Apply</c> methods for the rejection events keep replay total so a persisted rejection
/// never breaks rehydration.
/// </summary>
/// <remarks>
/// <see cref="IsCreated"/> distinguishes a never-created Agent from one whose stream contains only a persisted
/// pre-create rejection (e.g. an unauthorized create attempt): a non-null rehydrated state with
/// <see cref="IsCreated"/> = <see langword="false"/> is treated as "no Agent" by the handlers and the inspection.
/// <see cref="Instructions"/> is sensitive content (AD-14) — it is held here as the durable source of truth but is
/// never surfaced on the public status view, rejections, logs, or audit summaries.
/// </remarks>
public sealed class AgentState
{
    /// <summary>Gets or sets a value indicating whether the Agent record has been created.</summary>
    public bool IsCreated { get; set; }

    /// <summary>Gets or sets the stable Agent identifier (the aggregate id).</summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant scope captured at create (FR-1).</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the safe display name (may be empty for an incomplete draft).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional safe description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the Agent Instructions text (sensitive — AD-14; never exposed on the status surface).</summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>Gets or sets the current lifecycle state.</summary>
    public AgentLifecycleStatus Lifecycle { get; set; } = AgentLifecycleStatus.Unknown;

    /// <summary>Gets or sets the monotonic configuration version (1 at create, bumped on each accepted change).</summary>
    public int ConfigurationVersion { get; set; }

    /// <summary>Gets or sets the instructions version (bumped only when the instructions text changes).</summary>
    public int InstructionsVersion { get; set; }

    /// <summary>Applies the Agent creation: the record exists and starts in <see cref="AgentLifecycleStatus.Draft"/>.</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        IsCreated = true;
        AgentId = e.AgentId;
        TenantId = e.TenantId;
        DisplayName = e.DisplayName;
        Description = e.Description;
        Instructions = e.Instructions;
        Lifecycle = AgentLifecycleStatus.Draft;
        ConfigurationVersion = e.ConfigurationVersion;
        InstructionsVersion = e.InstructionsVersion;
    }

    /// <summary>Applies an accepted configuration update (lifecycle unchanged).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentConfigurationUpdated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsCreated)
        {
            return;
        }

        DisplayName = e.DisplayName;
        Description = e.Description;
        Instructions = e.Instructions;
        ConfigurationVersion = e.ConfigurationVersion;
        InstructionsVersion = e.InstructionsVersion;
    }

    /// <summary>Applies an activation (lifecycle becomes <see cref="AgentLifecycleStatus.Active"/>).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentActivated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (IsCreated)
        {
            Lifecycle = AgentLifecycleStatus.Active;
        }
    }

    /// <summary>Applies a disable (lifecycle flag flip only; all prior state preserved, AC3).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentDisabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (IsCreated)
        {
            Lifecycle = AgentLifecycleStatus.Disabled;
        }
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentAdministrationDeniedRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentNotFoundRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentAlreadyExistsRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentActivationBlockedRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentLifecycleStateAlreadySetRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(InvalidAgentConfigurationRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    private void MarkReplayOnlyEventHandled() => _ = AgentId;
}
