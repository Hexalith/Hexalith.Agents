namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that an Agent (<c>hexa</c>) defined its Content Safety Policy (Story 1.7 AC1; FR-26). Defining the policy
/// is a configuration change, so both the monotonic <see cref="ContentSafetyPolicyVersion"/> and the
/// <see cref="ConfigurationVersion"/> are bumped. Lifecycle is deliberately unchanged — a defined policy clears the
/// content-safety activation gate but does not by itself make the Agent <see cref="AgentLifecycleStatus.Active"/>
/// (the Story 1.3 lifecycle/readiness invariant).
/// </summary>
/// <remarks>
/// This durable event <b>is</b> the AC1 audit evidence of the policy choice: the configured <see cref="Configuration"/>
/// (active policy + any mode overrides), the new monotonic <see cref="ContentSafetyPolicyVersion"/>, and the bumped
/// configuration version. The configuration carries only safe references/enums and governance descriptor strings; no
/// secret, Party PII, or conversation content is carried here (AD-9, AD-14) — but the descriptor/category text is
/// policy content kept off the status surface, rejections, logs, and audit summaries (AC2; AD-14). No wall-clock
/// timestamp is carried (AD-3); occurrence time comes from EventStore event metadata. A changed policy emits a new
/// append-only event and bumps the version; prior events are never rewritten, so the change is future-only (AC1; AD-4).
/// </remarks>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
/// <param name="Configuration">The configured safe Content Safety configuration (active policy + optional mode overrides).</param>
/// <param name="ContentSafetyPolicyVersion">The bumped monotonic content-safety policy version after this change (AC1).</param>
/// <param name="ConfigurationVersion">The bumped configuration version after this change.</param>
public record AgentContentSafetyPolicyConfigured(
    string AgentId,
    AgentContentSafetyConfiguration Configuration,
    int ContentSafetyPolicyVersion,
    int ConfigurationVersion) : IEventPayload;
