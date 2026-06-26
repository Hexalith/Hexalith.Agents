using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Serves canonical audit availability reads.</summary>
public sealed class GetAgentAuditAvailabilityQueryHandler(IAgentInteractionAuditStateReader reader, ITenantAccessReader tenantAccessReader)
    : AgentInteractionAuditQueryHandlerBase(reader, tenantAccessReader)
{
    /// <inheritdoc />
    public override string QueryType => GetAgentAuditAvailabilityQuery.QueryType;

    /// <inheritdoc />
    protected override object CreatePayload(AgentInteractionState? state, string correlationId, bool isFresh)
        => AgentOperationResult<AuditAvailabilityStatus>.Succeeded(
            AgentInteractionAuditInspection.GetAuditAvailability(state, canLoadState: true, isFresh),
            correlationId: correlationId);

    /// <inheritdoc />
    protected override object CreateNotAuthorizedPayload(string correlationId)
        => AgentOperationResult<AuditAvailabilityStatus>.Failed(AgentOperationErrorCode.NotAuthorized, correlationId);

    /// <inheritdoc />
    protected override object CreateUnavailablePayload(string correlationId)
        => AgentOperationResult<AuditAvailabilityStatus>.Succeeded(AuditAvailabilityStatus.AuditUnavailable, correlationId: correlationId);
}
