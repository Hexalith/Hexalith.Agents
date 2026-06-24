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
/// status, badges, logs, and accessible names cannot leak the prompt.
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
/// <param name="ActivationBlockers">The specific blockers preventing activation as currently configured (empty when none).</param>
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
    IReadOnlyList<AgentActivationBlocker> ActivationBlockers);
