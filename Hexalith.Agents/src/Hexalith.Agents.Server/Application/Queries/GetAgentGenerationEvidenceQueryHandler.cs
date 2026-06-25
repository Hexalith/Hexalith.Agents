using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Serves safe AgentInteraction generation evidence reads.</summary>
public sealed class GetAgentGenerationEvidenceQueryHandler(IAgentInteractionAuditStateReader reader, ITenantAccessReader tenantAccessReader)
    : AgentInteractionAuditQueryHandlerBase(reader, tenantAccessReader)
{
    /// <inheritdoc />
    public override string QueryType => GetAgentGenerationEvidenceQuery.QueryType;

    /// <inheritdoc />
    protected override object CreatePayload(AgentInteractionState? state, string correlationId, bool isFresh)
        => EvidenceOperation(AgentInteractionAuditInspection.GetGenerationEvidence(state, isAuthorized: true), correlationId);

    /// <inheritdoc />
    protected override object CreateNotAuthorizedPayload(string correlationId)
        => AgentOperationResult<AgentGenerationAttemptEvidence>.Failed(AgentOperationErrorCode.NotAuthorized, correlationId);

    /// <inheritdoc />
    protected override object CreateUnavailablePayload(string correlationId)
        => AgentOperationResult<AgentGenerationAttemptEvidence>.Unavailable(correlationId);
}
