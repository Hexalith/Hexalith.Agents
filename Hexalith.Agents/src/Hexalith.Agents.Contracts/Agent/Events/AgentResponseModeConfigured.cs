namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that an Agent (<c>hexa</c>) chose its Response Mode (AC1; FR-6). Choosing/changing the mode is a
/// configuration change, so <see cref="ConfigurationVersion"/> is bumped. Lifecycle is deliberately unchanged — a
/// chosen mode clears the <see cref="AgentActivationBlocker.MissingResponseMode"/> readiness gate but does not by
/// itself make the Agent <see cref="AgentLifecycleStatus.Active"/> (the Story 1.3 lifecycle/readiness invariant).
/// </summary>
/// <remarks>
/// This durable event <b>is</b> the AC1 audit evidence of the mode choice: it records the safe <see cref="Mode"/>
/// and the resulting configuration version. No wall-clock timestamp is carried (AD-3); occurrence time comes from
/// EventStore event metadata. A changed mode emits a new append-only event and bumps the version; prior events are
/// never rewritten, so the change is future-only (AC1; AD-4).
/// </remarks>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
/// <param name="Mode">The recorded Response Mode.</param>
/// <param name="ConfigurationVersion">The bumped configuration version after the mode change.</param>
public record AgentResponseModeConfigured(
    string AgentId,
    AgentResponseMode Mode,
    int ConfigurationVersion) : IEventPayload;
