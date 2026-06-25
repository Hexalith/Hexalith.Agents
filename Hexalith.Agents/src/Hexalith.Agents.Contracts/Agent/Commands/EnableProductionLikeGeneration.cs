namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Requests enabling production-like generation for an existing Agent (<c>hexa</c>) (Story 4.4 AC1, AC4; FR-28).
/// Enablement evaluates the launch-readiness gate (Content Safety Policy + Context Policy + launch metrics + per-mode
/// latency targets + cost-control posture + resolved audit governance). On any blocker the command is rejected with
/// the specific launch-readiness blockers and generation stays disabled — a blocked enablement is never success
/// (fail-closed; AD-12). The Agent identifier comes from the command envelope.
/// </summary>
/// <remarks>
/// This is the higher, distinct gate above the baseline <c>ActivateAgent</c> activation (dev/staging): launch
/// readiness gates production-like generation. The gate is a pure state check over recorded launch-readiness values
/// (no resolver, no trusted verdict), so it cannot be bypassed by a direct-gateway command.
/// </remarks>
public record EnableProductionLikeGeneration();
