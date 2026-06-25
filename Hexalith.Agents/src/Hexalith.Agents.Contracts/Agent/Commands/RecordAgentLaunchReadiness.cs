namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Records the launch-readiness decision (metrics, per-mode latency targets, cost-control posture, and the in-force
/// context-policy reference) for an Agent (<c>hexa</c>) (Story 4.4 AC1, AC2, AC3; FR-28). The command payload carries
/// only the safe <see cref="Readiness"/> value; the Agent identifier comes from the command envelope. No version is on
/// the payload — the aggregate assigns the monotonic <c>LaunchReadinessVersion</c> when the change is accepted.
/// </summary>
/// <remarks>
/// Recording readiness performs structural validation and normalization only; launch-readiness values are
/// self-contained Agent state, so it reads no sibling-module dependency and carries no verdict (AD-3) — mirroring
/// <see cref="ConfigureAgentContentSafetyPolicy"/>. Recording readiness bumps both the launch-readiness version and the
/// configuration version (AD-4) so future <c>AgentInteraction</c> snapshots pick up the new posture. Re-asserting an
/// equal readiness is a deterministic no-op (AD-13).
/// </remarks>
/// <param name="Readiness">The safe launch-readiness decision to record.</param>
public record RecordAgentLaunchReadiness(AgentLaunchReadiness Readiness);
