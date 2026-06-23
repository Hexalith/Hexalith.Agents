namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that an Agent (<c>hexa</c>) passed this story's activation gates and transitioned to
/// <see cref="AgentLifecycleStatus.Active"/> (AC2; FR-3). Full callability accretes across the epic as later
/// stories add their gates; this event marks only that the lifecycle is active. No wall-clock timestamp is
/// carried (AD-3); occurrence time comes from EventStore event metadata.
/// </summary>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
public record AgentActivated(string AgentId) : IEventPayload;
