namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Defines the active Content Safety Policy (and any stricter mode-specific overrides) for an Agent (<c>hexa</c>)
/// (Story 1.7 AC1, AC3; FR-26). The command payload carries only the safe <see cref="Configuration"/> value; the
/// Agent identifier comes from the command envelope. No policy version is on the payload — the aggregate assigns the
/// monotonic <c>ContentSafetyPolicyVersion</c> when the change is accepted.
/// </summary>
/// <remarks>
/// Defining the policy performs structural validation and normalization only; content-safety configuration is
/// self-contained Agent state, so it reads no Parties/Tenants/Conversations/provider dependency and carries no verdict
/// (AD-3) — unlike the party/provider/approver gates. Defining the policy bumps both the policy version (AC1) and the
/// configuration version (AD-4); the change applies only to future Agent Calls. Re-asserting an equal configuration is
/// a deterministic no-op (AD-13).
/// </remarks>
/// <param name="Configuration">The safe Content Safety configuration (active policy + optional mode overrides) to record.</param>
public record ConfigureAgentContentSafetyPolicy(AgentContentSafetyConfiguration Configuration);
