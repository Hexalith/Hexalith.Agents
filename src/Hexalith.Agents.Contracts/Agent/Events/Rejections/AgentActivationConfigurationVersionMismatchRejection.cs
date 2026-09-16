namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// An activation targeted a projected configuration version that is no longer the aggregate's current version.
/// </summary>
/// <param name="AgentId">The Agent aggregate identifier.</param>
/// <param name="ExpectedConfigurationVersion">The version retained with the activation intent.</param>
/// <param name="ActualConfigurationVersion">The aggregate version observed during command execution.</param>
public sealed record AgentActivationConfigurationVersionMismatchRejection(
    string AgentId,
    int ExpectedConfigurationVersion,
    int ActualConfigurationVersion) : IRejectionEvent;
