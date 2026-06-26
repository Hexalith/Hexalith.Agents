namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that an Agent (<c>hexa</c>) selected an enabled Provider/model from the governed catalog (AC1; FR-5).
/// Selecting/changing the Provider/model is a configuration change, so <see cref="ConfigurationVersion"/> is bumped.
/// Lifecycle is deliberately unchanged — a ready Provider clears the
/// <see cref="AgentActivationBlocker.MissingProviderSelection"/>/<see cref="AgentActivationBlocker.ProviderUnavailable"/>
/// readiness gates but does not by itself make the Agent <see cref="AgentLifecycleStatus.Active"/> (the Story 1.3
/// lifecycle/readiness invariant).
/// </summary>
/// <remarks>
/// This durable event <b>is</b> the AC1 Audit Evidence: it records the safe Provider/model identity, the captured
/// provider capability version, and the resulting configuration version — enough safe identity for future Audit
/// Evidence. <see cref="ProviderId"/>/<see cref="ModelId"/> are stable safe identifiers and
/// <see cref="ProviderCapabilityVersion"/> is a plain int; no secret value, configuration reference, capability
/// metadata blob, or provider SDK type is carried here (AD-9, AD-14). No wall-clock timestamp is carried (AD-3);
/// occurrence time comes from EventStore event metadata. A changed selection deterministically overwrites the
/// single recorded selection; prior events are append-only and never rewritten (AC3).
/// </remarks>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
/// <param name="ProviderId">The selected stable safe provider identifier (a reference, not a secret).</param>
/// <param name="ModelId">The selected stable safe model identifier (a reference, not a secret).</param>
/// <param name="ProviderCapabilityVersion">The provider capability version captured at selection time (AC1).</param>
/// <param name="ConfigurationVersion">The bumped configuration version after the selection.</param>
public record AgentProviderModelSelected(
    string AgentId,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int ConfigurationVersion) : IEventPayload;
