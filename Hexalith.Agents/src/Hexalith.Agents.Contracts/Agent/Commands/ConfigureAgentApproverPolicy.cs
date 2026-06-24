namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Configures the Approver Policy for a Confirmation-mode Agent (<c>hexa</c>) (AC2, AC4; FR-7). The command payload
/// carries only the safe <see cref="Policy"/> value (the ordered approver sources + the disclosure category); the
/// Agent identifier comes from the command envelope. No policy version is on the payload — the aggregate assigns
/// the monotonic <c>ApproverPolicyVersion</c> when the change is accepted.
/// </summary>
/// <remarks>
/// Storing the policy performs structural validation only and records the configured sources; it never reads
/// Parties/Tenants/Conversations (AD-3) — resolving the sources against their dependencies is the activation
/// readiness concern, not configuration. Storing bumps both the policy version (AC4) and the configuration version
/// (AD-4); the change applies only to future Agent Calls. Re-asserting an equal policy (same ordered sources +
/// disclosure) is a deterministic no-op (AD-13).
/// </remarks>
/// <param name="Policy">The safe Approver Policy value (sources + disclosure category) to record.</param>
public record ConfigureAgentApproverPolicy(AgentApproverPolicy Policy);
