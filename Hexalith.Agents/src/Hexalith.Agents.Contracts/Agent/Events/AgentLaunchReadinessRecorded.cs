namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that an Agent (<c>hexa</c>) recorded its launch-readiness decision (Story 4.4 AC1; FR-28). Recording
/// readiness is a configuration change, so both the monotonic <see cref="LaunchReadinessVersion"/> and the
/// <see cref="ConfigurationVersion"/> are bumped. Lifecycle is deliberately unchanged — recorded readiness clears the
/// launch-readiness gate inputs but does not by itself enable production-like generation (that is the separate
/// <see cref="AgentProductionLikeGenerationEnabled"/> gate).
/// </summary>
/// <remarks>
/// This durable event <b>is</b> the AC1 audit evidence of the readiness decision: the recorded <see cref="Readiness"/>
/// (metrics + per-mode latency targets + cost posture + context-policy reference), the new monotonic
/// <see cref="LaunchReadinessVersion"/>, and the bumped configuration version. The readiness carries only safe
/// governance descriptors and enums; no secret, raw payload, or Party PII is carried here (AD-14). No wall-clock
/// timestamp is carried (AD-3); occurrence time comes from EventStore event metadata. A changed readiness emits a new
/// append-only event and bumps the version; prior events are never rewritten, so the change is future-only (AD-4).
/// </remarks>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
/// <param name="Readiness">The recorded safe launch-readiness decision.</param>
/// <param name="LaunchReadinessVersion">The bumped monotonic launch-readiness version after this change (AC1).</param>
/// <param name="ConfigurationVersion">The bumped configuration version after this change.</param>
public record AgentLaunchReadinessRecorded(
    string AgentId,
    AgentLaunchReadiness Readiness,
    int LaunchReadinessVersion,
    int ConfigurationVersion) : IEventPayload;
