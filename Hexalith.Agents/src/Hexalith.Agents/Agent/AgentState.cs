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

    /// <summary>
    /// Gets or sets the single linked Party identity reference (<see langword="null"/> = no linked identity). This
    /// is the only new durable field for the Party link (AC1) — a stable Parties-owned id, never Party PII (AD-7).
    /// </summary>
    public string? PartyId { get; set; }

    /// <summary>
    /// Gets or sets the selected stable safe provider identifier (<see langword="null"/> = no selection). A safe
    /// reference, never a secret (AD-9); one of the only new durable selection fields (Story 1.5 AC1).
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Gets or sets the selected stable safe model identifier (<see langword="null"/> = no selection). A safe
    /// reference, never a secret (AD-9).
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Gets or sets the captured provider capability version of the current selection (<see langword="null"/> = no
    /// selection). A plain int — never a capability-metadata blob (AC1; AD-9, AD-14).
    /// </summary>
    public int? ProviderCapabilityVersion { get; set; }

    /// <summary>
    /// Gets or sets the configured Response Mode (default <see cref="AgentResponseMode.Unknown"/> = not configured,
    /// which fails the activation gate). A safe enum, no secret/content (Story 1.6 AC1; AD-9, AD-14).
    /// </summary>
    public AgentResponseMode ResponseMode { get; set; } = AgentResponseMode.Unknown;

    /// <summary>
    /// Gets or sets the configured approver sources (<see langword="null"/> = no policy configured). Safe references
    /// only — never Party PII (Story 1.6 AC2; AD-7, AD-14).
    /// </summary>
    public IReadOnlyList<ApproverPolicySource>? ApproverPolicySources { get; set; }

    /// <summary>Gets or sets the configured FR-7 disclosure category (safe metadata; Story 1.6 AC4).</summary>
    public ApproverPolicyBasisDisclosure ApproverPolicyDisclosure { get; set; }

    /// <summary>Gets or sets the monotonic approver-policy version (0 until a policy is configured; Story 1.6 AC4).</summary>
    public int ApproverPolicyVersion { get; set; }

    /// <summary>
    /// Gets or sets the configured Content Safety configuration (<see langword="null"/> = no policy configured, which
    /// fails the activation gate). The policy text is policy/governance content held here as the durable source of
    /// truth but never surfaced on the status view, rejections, logs, or audit summaries (Story 1.7 AC2; AD-9, AD-14)
    /// — mirroring the <see cref="Instructions"/> field.
    /// </summary>
    public AgentContentSafetyConfiguration? ContentSafety { get; set; }

    /// <summary>Gets or sets the monotonic content-safety policy version (0 until a policy is configured; Story 1.7 AC1).</summary>
    public int ContentSafetyPolicyVersion { get; set; }

    /// <summary>
    /// Gets or sets the recorded launch-readiness decision (<see langword="null"/> = none recorded, which fails the
    /// launch-readiness gate). Safe governance descriptors/enums only — never secrets, raw payloads, or Party PII
    /// (Story 4.4 AC1; AD-14).
    /// </summary>
    public AgentLaunchReadiness? LaunchReadiness { get; set; }

    /// <summary>Gets or sets the monotonic launch-readiness version (0 until readiness is recorded; Story 4.4 AC1).</summary>
    public int LaunchReadinessVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether production-like generation has been enabled behind the launch-readiness
    /// gate (default <see langword="false"/> = disabled, the fail-closed state). The higher gate distinct from baseline
    /// activation (Story 4.4 AC1, AC4; AD-12).
    /// </summary>
    public bool ProductionLikeGenerationEnabled { get; set; }

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

    /// <summary>Applies a Party-identity link: stores only the stable id reference and bumps the configuration version (AC1).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentPartyIdentityLinked e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsCreated)
        {
            return;
        }

        PartyId = e.PartyId;
        ConfigurationVersion = e.ConfigurationVersion;
    }

    /// <summary>Applies a Party-identity replacement: the single active id reference becomes the new one (AC3).</summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentPartyIdentityReplaced e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsCreated)
        {
            return;
        }

        PartyId = e.PartyId;
        ConfigurationVersion = e.ConfigurationVersion;
    }

    /// <summary>
    /// Applies a Provider/model selection: stores only the safe identifiers + captured capability version and
    /// bumps the configuration version (AC1). Lifecycle is unchanged (Story 1.3 invariant). A changed selection
    /// deterministically overwrites the single recorded selection; prior events are never rewritten (AC3).
    /// </summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentProviderModelSelected e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsCreated)
        {
            return;
        }

        ProviderId = e.ProviderId;
        ModelId = e.ModelId;
        ProviderCapabilityVersion = e.ProviderCapabilityVersion;
        ConfigurationVersion = e.ConfigurationVersion;
    }

    /// <summary>
    /// Applies a Response Mode choice: records the mode and bumps the configuration version (Story 1.6 AC1).
    /// Lifecycle is unchanged (Story 1.3 invariant); a changed mode is future-only (prior events never rewritten).
    /// </summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentResponseModeConfigured e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsCreated)
        {
            return;
        }

        ResponseMode = e.Mode;
        ConfigurationVersion = e.ConfigurationVersion;
    }

    /// <summary>
    /// Applies an Approver Policy configuration: records the safe sources + disclosure category and bumps both the
    /// approver-policy version and the configuration version (Story 1.6 AC2, AC4). Lifecycle is unchanged; a changed
    /// policy is future-only (prior events never rewritten).
    /// </summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentApproverPolicyConfigured e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsCreated)
        {
            return;
        }

        ApproverPolicySources = e.Policy.Sources;
        ApproverPolicyDisclosure = e.Policy.DisclosureCategory;
        ApproverPolicyVersion = e.ApproverPolicyVersion;
        ConfigurationVersion = e.ConfigurationVersion;
    }

    /// <summary>
    /// Applies a Content Safety Policy configuration: records the safe configuration (active policy + optional mode
    /// overrides) and bumps both the content-safety policy version and the configuration version (Story 1.7 AC1).
    /// Lifecycle is unchanged; a changed policy is future-only (prior events never rewritten).
    /// </summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentContentSafetyPolicyConfigured e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsCreated)
        {
            return;
        }

        ContentSafety = e.Configuration;
        ContentSafetyPolicyVersion = e.ContentSafetyPolicyVersion;
        ConfigurationVersion = e.ConfigurationVersion;
    }

    /// <summary>
    /// Applies a launch-readiness recording: records the safe readiness decision (metrics + per-mode latency targets +
    /// cost posture + context-policy reference) and bumps both the launch-readiness version and the configuration
    /// version (Story 4.4 AC1). Lifecycle is unchanged; a changed readiness is future-only (prior events never rewritten).
    /// </summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentLaunchReadinessRecorded e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsCreated)
        {
            return;
        }

        LaunchReadiness = e.Readiness;
        LaunchReadinessVersion = e.LaunchReadinessVersion;
        ConfigurationVersion = e.ConfigurationVersion;
    }

    /// <summary>
    /// Applies enabling production-like generation: flips the gate flag and bumps the configuration version (Story 4.4
    /// AC1, AC4). The higher gate distinct from baseline activation; lifecycle is unchanged.
    /// </summary>
    /// <param name="e">The event.</param>
    public void Apply(AgentProductionLikeGenerationEnabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!IsCreated)
        {
            return;
        }

        ProductionLikeGenerationEnabled = true;
        ConfigurationVersion = e.ConfigurationVersion;
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentLaunchReadinessRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentProductionLikeGenerationBlockedRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentProviderModelSelectionRejected e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentPartyIdentityLinkRejected e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(AgentPartyIdentityAlreadyLinkedRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
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
