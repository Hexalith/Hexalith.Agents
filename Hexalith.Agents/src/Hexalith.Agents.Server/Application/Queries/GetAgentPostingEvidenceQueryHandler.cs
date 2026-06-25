using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Serves safe AgentInteraction posting evidence reads.</summary>
public sealed class GetAgentPostingEvidenceQueryHandler(IAgentInteractionAuditStateReader reader, ITenantAccessReader tenantAccessReader)
    : AgentInteractionAuditQueryHandlerBase(reader, tenantAccessReader)
{
    /// <inheritdoc />
    public override string QueryType => GetAgentPostingEvidenceQuery.QueryType;

    /// <inheritdoc />
    protected override object CreatePayload(AgentInteractionState? state, string correlationId, bool isFresh)
        => EvidenceOperation(AgentInteractionAuditInspection.GetPostingEvidence(state, isAuthorized: true), correlationId);

    /// <inheritdoc />
    protected override object CreateNotAuthorizedPayload(string correlationId)
        => AgentOperationResult<AgentPostedMessageEvidence>.Failed(AgentOperationErrorCode.NotAuthorized, correlationId);

    /// <inheritdoc />
    protected override object CreateUnavailablePayload(string correlationId)
        => AgentOperationResult<AgentPostedMessageEvidence>.Unavailable(correlationId);
}
