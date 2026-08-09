using System;
using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;

using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// The persisted Agent setup read model (Story 5.2). It is a safe mirror of the replayed Agent state: every field
/// the safe status view and the activation/launch-readiness gates are derived from, plus the freshness metadata a
/// caller compares against an accepted configuration version.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sensitive content (AD-14):</b> the Agent Instructions text and the Content Safety Policy content are folded in
/// memory but deliberately never persisted here. Only the presence/validity/version signals the safe view already
/// exposes are stored (<see cref="HasInstructions"/>, <see cref="InstructionsValid"/>,
/// <see cref="HasContentSafetyPolicy"/> and the per-mode override flags), so a read-model dump can never leak a
/// prompt or a policy body.
/// </para>
/// <para>
/// <see cref="PartyId"/>, <see cref="ApproverPolicySources"/>, and <see cref="ProviderCapabilityVersion"/> are stable
/// safe references (never Party PII, AD-7; never a secret, AD-9). They stay server-internal — the public setup view
/// exposes only presence — but they are required so an incremental delivery rehydrates exactly the state a full
/// replay would produce.
/// </para>
/// </remarks>
public sealed class AgentSetupReadModel : IReadModelFreshness
{
    /// <summary>Gets or sets a value indicating whether the Agent record exists.</summary>
    public bool IsCreated { get; set; }

    /// <summary>Gets or sets the stable Agent identifier (the aggregate id).</summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant scope captured at create.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the safe display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional safe description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a value indicating whether Agent Instructions are present (never the text; AD-14).</summary>
    public bool HasInstructions { get; set; }

    /// <summary>Gets or sets a value indicating whether the present Agent Instructions are valid (never the text; AD-14).</summary>
    public bool InstructionsValid { get; set; }

    /// <summary>Gets or sets the instructions version.</summary>
    public int InstructionsVersion { get; set; }

    /// <summary>Gets or sets the current lifecycle state.</summary>
    public AgentLifecycleStatus Lifecycle { get; set; } = AgentLifecycleStatus.Unknown;

    /// <summary>Gets or sets the monotonic configuration version.</summary>
    public int ConfigurationVersion { get; set; }

    /// <summary>Gets or sets the linked Party identity reference, or <see langword="null"/> when none is linked.</summary>
    public string? PartyId { get; set; }

    /// <summary>Gets or sets the selected safe provider identifier.</summary>
    public string? ProviderId { get; set; }

    /// <summary>Gets or sets the selected safe model identifier.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the captured provider capability version of the current selection.</summary>
    public int? ProviderCapabilityVersion { get; set; }

    /// <summary>Gets or sets the configured Response Mode.</summary>
    public AgentResponseMode ResponseMode { get; set; } = AgentResponseMode.Unknown;

    /// <summary>Gets or sets the configured approver sources (safe references only; AD-7).</summary>
    public IReadOnlyList<ApproverPolicySource> ApproverPolicySources { get; set; } = [];

    /// <summary>Gets or sets the configured disclosure category.</summary>
    public ApproverPolicyBasisDisclosure ApproverPolicyDisclosure { get; set; }

    /// <summary>Gets or sets the monotonic approver-policy version.</summary>
    public int ApproverPolicyVersion { get; set; }

    /// <summary>Gets or sets a value indicating whether an active Content Safety Policy is configured (never its content; AD-14).</summary>
    public bool HasContentSafetyPolicy { get; set; }

    /// <summary>Gets or sets a value indicating whether a stricter Automatic-mode override is configured.</summary>
    public bool HasAutomaticContentSafetyOverride { get; set; }

    /// <summary>Gets or sets a value indicating whether a stricter Confirmation-mode override is configured.</summary>
    public bool HasConfirmationContentSafetyOverride { get; set; }

    /// <summary>Gets or sets the monotonic content-safety policy version.</summary>
    public int ContentSafetyPolicyVersion { get; set; }

    /// <summary>Gets or sets the recorded launch-readiness decision (safe governance descriptors only; AD-14).</summary>
    public AgentLaunchReadiness? LaunchReadiness { get; set; }

    /// <summary>Gets or sets the monotonic launch-readiness version.</summary>
    public int LaunchReadinessVersion { get; set; }

    /// <summary>Gets or sets a value indicating whether production-like generation has been enabled.</summary>
    public bool ProductionLikeGenerationEnabled { get; set; }

    /// <summary>Gets or sets the highest aggregate sequence number folded into this read model.</summary>
    public long LastSequenceNumber { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ProjectedAt { get; set; }

    /// <inheritdoc />
    public string? ProjectionVersion { get; set; }
}
