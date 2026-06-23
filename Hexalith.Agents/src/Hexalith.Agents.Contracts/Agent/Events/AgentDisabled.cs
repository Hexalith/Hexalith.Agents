namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that an Agent (<c>hexa</c>) was disabled (AC3; FR-3). Disabling is a lifecycle flag flip to
/// <see cref="AgentLifecycleStatus.Disabled"/> only: it never deletes or rewrites prior identity, instructions,
/// configuration, or (in later epics) Audit Evidence, Proposed Agent Replies, or Conversation Messages — the
/// append-only aggregate preserves all history structurally. No wall-clock timestamp is carried (AD-3);
/// occurrence time comes from EventStore event metadata.
/// </summary>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
public record AgentDisabled(string AgentId) : IEventPayload;
