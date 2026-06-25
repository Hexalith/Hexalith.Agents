namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// A record-launch-readiness command carried structurally invalid readiness — e.g. an incomplete metric definition
/// (a missing numerator/denominator/window/cohort or an unset classification), a malformed latency target (an
/// unspecified mode, a non-positive target, or duplicate modes), or an unspecified cost-control posture (Story 4.4
/// AC1, AC2, AC3). The <paramref name="Reason"/> is a safe, display-friendly classification and never echoes the
/// offending value (AD-14).
/// </summary>
/// <param name="AgentId">The Agent aggregate identifier the command targeted.</param>
/// <param name="Reason">Safe classification of why the readiness was rejected (never raw input).</param>
public record AgentLaunchReadinessRejection(
    string AgentId,
    string Reason) : IRejectionEvent;
