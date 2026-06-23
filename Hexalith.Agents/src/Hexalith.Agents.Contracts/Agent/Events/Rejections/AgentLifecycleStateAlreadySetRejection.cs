namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// An activate/disable command requested the lifecycle state the Agent is already in (AC2, AC3). A deterministic,
/// structured rejection rather than a silent re-emission. Carries only safe lifecycle classification — no
/// instructions or unrelated tenant data.
/// </summary>
/// <param name="AgentId">The Agent aggregate identifier the command targeted.</param>
/// <param name="CurrentStatus">The Agent's current lifecycle state.</param>
/// <param name="RequestedStatus">The lifecycle state the command requested.</param>
/// <param name="CommandName">The attempted command.</param>
public record AgentLifecycleStateAlreadySetRejection(
    string AgentId,
    AgentLifecycleStatus CurrentStatus,
    AgentLifecycleStatus RequestedStatus,
    string CommandName) : IRejectionEvent;
