using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Serves safe proposal regeneration evidence reads.</summary>
public sealed class GetAgentProposalRegenerationEvidenceQueryHandler(IAgentInteractionAuditStateReader reader, ITenantAccessReader tenantAccessReader)
    : AgentInteractionAuditQueryHandlerBase(reader, tenantAccessReader)
{
    /// <inheritdoc />
    public override string QueryType => GetAgentProposalRegenerationEvidenceQuery.QueryType;

    /// <inheritdoc />
    protected override object CreatePayload(AgentInteractionState? state, string correlationId, bool isFresh)
        => AgentInteractionAuditInspection.GetProposalRegenerationEvidence(state, isAuthorized: true);

    /// <inheritdoc />
    protected override object CreateNotAuthorizedPayload(string correlationId)
        => AgentProposalRegenerationEvidenceResult.NotAuthorized();

    /// <inheritdoc />
    protected override object CreateUnavailablePayload(string correlationId)
        => AgentProposalRegenerationEvidenceResult.NotFound();
}
