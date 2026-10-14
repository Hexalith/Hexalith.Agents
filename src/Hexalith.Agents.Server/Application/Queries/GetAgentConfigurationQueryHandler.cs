using Hexalith.Agents.Contracts.Agent.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Serves the live <see cref="GetAgentConfigurationQuery"/> from the persisted setup read model.</summary>
public sealed class GetAgentConfigurationQueryHandler(
    IReadModelStore readModelStore,
    IOptions<AgentSetupReadModelOptions> options,
    ITenantAccessReader tenantAccessReader,
    IApproverPolicyResolver? approverResolver = null)
    : AgentSetupQueryHandlerBase(readModelStore, options, tenantAccessReader, approverResolver)
{
    /// <inheritdoc />
    public override string QueryType => GetAgentConfigurationQuery.QueryType;
}
