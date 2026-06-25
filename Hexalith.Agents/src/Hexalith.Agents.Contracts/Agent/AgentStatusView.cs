using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Safe, display/audit-ready projection of a governed Agent (<c>hexa</c>) for authorized status inspection
/// (AC1, AC3; FR-3). Exposes identity metadata, lifecycle state, configuration version, and the current
/// activation blockers so the later overview and <c>agent-readiness-badge</c> can render readiness.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sensitive content (AD-14):</b> the raw Agent Instructions text is deliberately absent from this view. The
/// view exposes only instruction <em>presence</em> (<see cref="HasInstructions"/>), <em>validity</em>
/// (<see cref="InstructionsValid"/>), and <see cref="InstructionsVersion"/> — never the instruction text — so
/// status, badges, logs, and accessible names cannot leak the prompt. The Content Safety Policy content (prompt
/// constraints, blocked/restricted output categories) is treated identically (1.7 AC2): the view exposes only
/// <see cref="HasContentSafetyPolicy"/>, <see cref="ContentSafetyPolicyVersion"/>, and which modes carry a stricter
/// override (<see cref="HasAutomaticContentSafetyOverride"/>/<see cref="HasConfirmationContentSafetyOverride"/>) —
/// never the policy content itself.
/// </para>
/// <para>
/// <see cref="Lifecycle"/> and <see cref="ActivationBlockers"/> are kept distinct: an Agent can be
/// <see cref="AgentLifecycleStatus.Disabled"/> (a lifecycle flag, AC3) independently of which configuration
/// blockers remain. A non-empty <see cref="ActivationBlockers"/> means the Agent is not activatable as configured.
/// </para>
/// </remarks>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
/// <param name="TenantId">Tenant scope captured at create.</param>
/// <param name="DisplayName">Safe display name (may be empty while the Agent is an incomplete draft).</param>
/// <param name="Description">Optional safe description.</param>
/// <param name="Lifecycle">Current lifecycle state.</param>
/// <param name="ConfigurationVersion">Monotonic configuration version (starts at 1, bumps on each accepted change).</param>
/// <param name="HasInstructions">Whether Agent Instructions are present (never the text itself).</param>
/// <param name="InstructionsValid">Whether the present Agent Instructions meet validity requirements.</param>
/// <param name="InstructionsVersion">Version of the Agent Instructions (bumps only when the instructions text changes).</param>
/// <param name="HasPartyIdentity">Whether a valid Party identity is linked (presence only — never the Party id or any Party PII; AC4).</param>
/// <param name="HasProviderSelection">Whether a Provider/model has been selected (presence only; 1.5 AC1).</param>
/// <param name="SelectedProviderId">The selected stable safe provider identifier, or <see langword="null"/> when none (a reference, not a secret — AD-9).</param>
/// <param name="SelectedModelId">The selected stable safe model identifier, or <see langword="null"/> when none (a reference, not a secret — AD-9).</param>
/// <param name="ResponseMode">The configured Response Mode (<see cref="AgentResponseMode.Unknown"/> until a mode is chosen; 1.6 AC1).</param>
/// <param name="HasApproverPolicy">Whether at least one approver source is configured (presence only — never the source list or any Party PII; 1.6 AC2).</param>
/// <param name="ApproverPolicyDisclosure">The configured FR-7 disclosure category (safe metadata; 1.6 AC4).</param>
/// <param name="ApproverPolicyVersion">The monotonic approver-policy version (0 until a policy is configured; 1.6 AC4).</param>
/// <param name="HasContentSafetyPolicy">Whether an active Content Safety Policy is configured (presence only — never the policy content; 1.7 AC2).</param>
/// <param name="ContentSafetyPolicyVersion">The monotonic content-safety policy version (0 until a policy is configured; 1.7 AC1).</param>
/// <param name="HasAutomaticContentSafetyOverride">Whether a stricter Automatic-mode content-safety override is configured (presence only; 1.7 AC3).</param>
/// <param name="HasConfirmationContentSafetyOverride">Whether a stricter Confirmation-mode content-safety override is configured (presence only; 1.7 AC3).</param>
/// <param name="ActivationBlockers">The specific blockers preventing activation as currently configured (empty when none).</param>
/// <param name="LaunchReadinessBlockers">The specific blockers preventing production-like generation as currently recorded (empty when none; 4.4 AC4). Appended after <paramref name="ActivationBlockers"/> per AD-17.</param>
/// <param name="ProductionLikeGenerationEnabled">Whether production-like generation has been enabled behind the launch-readiness gate (4.4 AC4).</param>
public record AgentStatusView(
    string AgentId,
    string TenantId,
    string DisplayName,
    string? Description,
    AgentLifecycleStatus Lifecycle,
    int ConfigurationVersion,
    bool HasInstructions,
    bool InstructionsValid,
    int InstructionsVersion,
    bool HasPartyIdentity,
    bool HasProviderSelection,
    string? SelectedProviderId,
    string? SelectedModelId,
    AgentResponseMode ResponseMode,
    bool HasApproverPolicy,
    ApproverPolicyBasisDisclosure ApproverPolicyDisclosure,
    int ApproverPolicyVersion,
    bool HasContentSafetyPolicy,
    int ContentSafetyPolicyVersion,
    bool HasAutomaticContentSafetyOverride,
    bool HasConfirmationContentSafetyOverride,
    IReadOnlyList<AgentActivationBlocker> ActivationBlockers,
    IReadOnlyList<AgentLaunchReadinessBlocker> LaunchReadinessBlockers,
    bool ProductionLikeGenerationEnabled);
