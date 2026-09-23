namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// Historical rejection retained so existing Agent event streams can be replayed. Current activate and disable
/// commands return an already-applied result when the requested lifecycle state is already set.
/// Carries only safe lifecycle classification, with no instructions or unrelated tenant data.
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
