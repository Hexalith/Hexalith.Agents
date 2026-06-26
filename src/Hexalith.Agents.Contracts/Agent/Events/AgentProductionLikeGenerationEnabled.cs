namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that an Agent (<c>hexa</c>) passed the launch-readiness gate and had production-like generation enabled
/// (Story 4.4 AC1, AC4; FR-28). Enabling is a configuration change, so the <see cref="ConfigurationVersion"/> is
/// bumped. This is the higher gate distinct from baseline activation; prior interactions are unaffected. No wall-clock
/// timestamp is carried (AD-3); occurrence time comes from EventStore event metadata.
/// </summary>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
/// <param name="ConfigurationVersion">The bumped configuration version after this change.</param>
public record AgentProductionLikeGenerationEnabled(
    string AgentId,
    int ConfigurationVersion) : IEventPayload;
