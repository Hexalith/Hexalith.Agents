namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// A create command targeted an Agent that already exists with a conflicting configuration (AC1). An exact
/// re-create is a deterministic no-op instead; this rejection signals only a genuine conflict and carries only
/// the Agent identity — never instructions or prior/new field values.
/// </summary>
/// <param name="AgentId">The Agent aggregate identifier the command targeted.</param>
public record AgentAlreadyExistsRejection(string AgentId) : IRejectionEvent;
