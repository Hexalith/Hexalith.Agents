using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The cohesive launch-readiness decision a Release Operator records for an Agent (<c>hexa</c>) before
/// production-like generation can be enabled (Story 4.4 AC1, AC2, AC3; FR-28). It bundles the launch
/// <see cref="Metrics"/>, the per-mode <see cref="LatencyTargets"/>, the <see cref="CostPosture"/>, and the
/// in-force <see cref="ContextPolicyReference"/> as one unit the command, event, durable state, and read path all
/// reuse — modeled on <see cref="AgentContentSafetyConfiguration"/>.
/// </summary>
/// <remarks>
/// <para>
/// This value object carries only safe governance data — metric/latency/cost descriptors and a context-policy
/// reference. It carries NO secrets, raw payloads, provider SDK types, or Party PII (AD-14). The free-text metric
/// descriptors are safe governance data but are kept out of telemetry dimensions.
/// </para>
/// <para>
/// <see cref="ContextPolicyReference"/> confirms the in-force Conversation Context Policy (V1 default
/// <c>full-conversation-v1</c> per AD-4/AD-11); it does not re-implement context bounding (already done in Story
/// 2.3). The launch-readiness gate is a pure state check over these recorded values — an empty/invalid readiness is
/// rejected at recording time, so a recorded readiness is exactly a valid one.
/// </para>
/// </remarks>
/// <param name="Metrics">The launch metric definitions (numerator/denominator/target/window/cohort + classification; AC2).</param>
/// <param name="LatencyTargets">The explicit per-mode latency targets (Automatic + Confirmation; AC3).</param>
/// <param name="CostPosture">The recorded cost-control posture (AC3).</param>
/// <param name="CostPostureNote">An optional safe governance note about the cost posture (e.g. why a launch risk was accepted).</param>
/// <param name="ContextPolicyReference">The reference confirming the in-force Conversation Context Policy (V1 default <c>full-conversation-v1</c>).</param>
public record AgentLaunchReadiness(
    IReadOnlyList<LaunchMetricDefinition> Metrics,
    IReadOnlyList<ResponseModeLatencyTarget> LatencyTargets,
    CostControlPosture CostPosture,
    string? CostPostureNote,
    string ContextPolicyReference);
