namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe Agent Call status reference returned to callers when a <see cref="Commands.RequestAgentInteraction"/>
/// is accepted (AC2; FR-8). It carries only the deterministic interaction identifier and a coarse
/// <see cref="AgentInteractionStatus"/> — never an EventStore stream name, provider SDK detail, internal projection
/// identifier, the prompt, or any Conversation content (AC2, AC4; AD-14).
/// </summary>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (a safe handle for later status reads).</param>
/// <param name="Status">The coarse Agent Call status (<see cref="AgentInteractionStatus.Requested"/> on acceptance).</param>
public record AgentInteractionReference(
    string AgentInteractionId,
    AgentInteractionStatus Status);
