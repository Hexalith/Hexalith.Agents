using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Safe, display/audit-ready projection of a governed Agent's (<c>hexa</c>) launch readiness for authorized status
/// inspection (Story 4.4 AC2, AC3, AC4; FR-28). Exposes the recorded launch governance data (metrics, per-mode latency
/// targets, cost posture) plus presence flags and the current launch-readiness <see cref="Blockers"/> so the readiness
/// surface can render and explain the production-like-enablement gate.
/// </summary>
/// <remarks>
/// The metric/latency/cost values are safe governance data (AD-14) — there are NO secrets, raw provider payloads, or
/// Party PII here. The free-text metric descriptors are safe to display but are kept out of telemetry dimensions. The
/// <see cref="Blockers"/> are computed by the same pure policy the enablement gate uses, so the view's blockers stay in
/// lock-step with what an enablement attempt would reject. A non-empty <see cref="Blockers"/> means production-like
/// generation cannot be enabled as currently recorded.
/// </remarks>
/// <param name="Metrics">The recorded launch metric definitions (numerator/denominator/target/window/cohort + classification; AC2).</param>
/// <param name="LatencyTargets">The recorded per-mode latency targets (AC3).</param>
/// <param name="CostPosture">The recorded cost-control posture (<see cref="CostControlPosture.Unknown"/> when none recorded; AC3).</param>
/// <param name="LaunchReadinessVersion">The monotonic launch-readiness version (0 until readiness is recorded; AC1).</param>
/// <param name="HasContentSafetyPolicy">Whether an active Content Safety Policy is configured (presence only — never the policy content; AC1).</param>
/// <param name="HasContextPolicy">Whether a Conversation Context Policy is in force (presence only; AC1).</param>
/// <param name="ProductionLikeGenerationEnabled">Whether production-like generation has been enabled behind the gate (AC4).</param>
/// <param name="Blockers">The specific launch-readiness blockers preventing production-like enablement (empty when none; AC4).</param>
public record AgentLaunchReadinessView(
    IReadOnlyList<LaunchMetricDefinition> Metrics,
    IReadOnlyList<ResponseModeLatencyTarget> LatencyTargets,
    CostControlPosture CostPosture,
    int LaunchReadinessVersion,
    bool HasContentSafetyPolicy,
    bool HasContextPolicy,
    bool ProductionLikeGenerationEnabled,
    IReadOnlyList<AgentLaunchReadinessBlocker> Blockers);
