namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// An update/activate/disable command targeted an Agent that does not exist (AC2). Carries only the Agent
/// identity — no instructions or unrelated tenant data.
/// </summary>
/// <param name="AgentId">The Agent aggregate identifier the command targeted.</param>
public record AgentNotFoundRejection(string AgentId) : IRejectionEvent;
