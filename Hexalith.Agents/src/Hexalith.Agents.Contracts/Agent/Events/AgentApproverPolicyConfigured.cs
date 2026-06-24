namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that an Agent (<c>hexa</c>) configured its Approver Policy (AC2, AC4; FR-7). Configuring the policy is a
/// configuration change, so both the monotonic <see cref="ApproverPolicyVersion"/> and the
/// <see cref="ConfigurationVersion"/> are bumped. Lifecycle is deliberately unchanged — a stored policy contributes
/// to Confirmation-mode readiness but does not by itself make the Agent <see cref="AgentLifecycleStatus.Active"/>
/// (the Story 1.3 lifecycle/readiness invariant).
/// </summary>
/// <remarks>
/// This durable event <b>is</b> the AC4 record: the configured <see cref="Policy"/> (the ordered approver sources +
/// the disclosure category), the new monotonic <see cref="ApproverPolicyVersion"/>, and the bumped configuration
/// version — enough safe basis for consistent policy-basis reporting later (Epic 3). The policy carries only safe
/// references and enums; no Party display name, contact value, personal identifier, or secret is carried here
/// (AD-7, AD-14). No wall-clock timestamp is carried (AD-3); occurrence time comes from EventStore event metadata.
/// A changed policy emits a new append-only event and bumps the version; prior events are never rewritten, so the
/// change is future-only (AC4; AD-4).
/// </remarks>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
/// <param name="Policy">The configured safe Approver Policy value (sources + disclosure category).</param>
/// <param name="ApproverPolicyVersion">The bumped monotonic approver-policy version after this change (AC4).</param>
/// <param name="ConfigurationVersion">The bumped configuration version after this change.</param>
public record AgentApproverPolicyConfigured(
    string AgentId,
    AgentApproverPolicy Policy,
    int ApproverPolicyVersion,
    int ConfigurationVersion) : IEventPayload;
