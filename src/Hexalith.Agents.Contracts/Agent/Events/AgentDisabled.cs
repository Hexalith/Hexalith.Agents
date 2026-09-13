namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that an Agent (<c>hexa</c>) was disabled (AC3; FR-3). Disabling advances the configuration version and
/// changes the lifecycle to <see cref="AgentLifecycleStatus.Disabled"/>; it never deletes or rewrites prior identity, instructions,
/// configuration, or (in later epics) Audit Evidence, Proposed Agent Replies, or Conversation Messages — the
/// append-only aggregate preserves all history structurally. No wall-clock timestamp is carried (AD-3);
/// occurrence time comes from EventStore event metadata.
/// </summary>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
public record AgentDisabled(string AgentId) : IEventPayload
{
    /// <summary>
    /// Gets the configuration version after disabling, or zero when replaying a legacy event that predates
    /// lifecycle versioning.
    /// </summary>
    public int ConfigurationVersion { get; init; }
}
