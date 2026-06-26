namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// An explicit latency target for one Agent Response Mode (Story 4.4 AC3; FR-28; PRD §7 "Performance"). Both
/// <see cref="AgentResponseMode.Automatic"/> and <see cref="AgentResponseMode.Confirmation"/> modes must carry an
/// explicit latency target before production-like generation is enabled.
/// </summary>
/// <remarks>
/// V1 records a single deterministic scalar <see cref="TargetMilliseconds"/>; concrete SLO values are deferred per
/// OQ-5, so this is intentionally not a percentile/distribution type. Latency targets are safe governance data
/// (never secrets or content; AD-14). Runtime latency enforcement is deferred — this records the target structure,
/// not a runtime guard. <see cref="AgentResponseMode.Unknown"/> is the not-yet-configured sentinel and is rejected
/// at recording time, so a recorded target always names a concrete mode.
/// </remarks>
/// <param name="Mode">The Response Mode the latency target applies to (Automatic or Confirmation).</param>
/// <param name="TargetMilliseconds">The target latency for the mode, in milliseconds.</param>
public record ResponseModeLatencyTarget(
    AgentResponseMode Mode,
    int TargetMilliseconds);
