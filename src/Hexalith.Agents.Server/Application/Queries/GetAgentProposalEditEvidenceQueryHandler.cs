using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Serves safe proposal edit evidence reads.</summary>
public sealed class GetAgentProposalEditEvidenceQueryHandler(IAgentInteractionAuditStateReader reader, ITenantAccessReader tenantAccessReader)
    : AgentInteractionAuditQueryHandlerBase(reader, tenantAccessReader)
{
    /// <inheritdoc />
    public override string QueryType => GetAgentProposalEditEvidenceQuery.QueryType;

    /// <inheritdoc />
    protected override object CreatePayload(AgentInteractionState? state, string correlationId, bool isFresh)
        => AgentInteractionAuditInspection.GetProposalEditEvidence(state, isAuthorized: true);

    /// <inheritdoc />
    protected override object CreateNotAuthorizedPayload(string correlationId)
        => AgentProposalEditEvidenceResult.NotAuthorized();

    /// <inheritdoc />
    protected override object CreateUnavailablePayload(string correlationId)
        => AgentProposalEditEvidenceResult.NotFound();
}
