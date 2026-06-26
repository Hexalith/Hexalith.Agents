using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Serves safe AgentInteraction status reads.</summary>
public sealed class GetAgentInteractionStatusQueryHandler(IAgentInteractionAuditStateReader reader, ITenantAccessReader tenantAccessReader)
    : AgentInteractionAuditQueryHandlerBase(reader, tenantAccessReader)
{
    /// <inheritdoc />
    public override string QueryType => GetAgentInteractionStatusQuery.QueryType;

    /// <inheritdoc />
    protected override object CreatePayload(AgentInteractionState? state, string correlationId, bool isFresh)
        => AgentInteractionAuditInspection.GetStatus(state, isAuthorized: true);

    /// <inheritdoc />
    protected override object CreateNotAuthorizedPayload(string correlationId)
        => AgentInteractionInspectionResult.NotAuthorized();

    /// <inheritdoc />
    protected override object CreateUnavailablePayload(string correlationId)
        => AgentInteractionInspectionResult.NotFound();
}
