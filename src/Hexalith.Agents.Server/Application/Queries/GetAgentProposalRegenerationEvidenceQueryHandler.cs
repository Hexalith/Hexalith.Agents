using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Serves safe proposal regeneration evidence reads.</summary>
public sealed class GetAgentProposalRegenerationEvidenceQueryHandler(IAgentInteractionAuditStateReader reader, ITenantAccessReader tenantAccessReader,
    IAgentInteractionAuditAuthority? auditAuthority = null)
    : AgentInteractionAuditQueryHandlerBase(reader, tenantAccessReader, auditAuthority)
{
    /// <inheritdoc />
    public override string QueryType => GetAgentProposalRegenerationEvidenceQuery.QueryType;

    /// <inheritdoc />
    protected override object CreatePayload(AgentInteractionState? state, string correlationId, bool isFresh)
    {
        AgentProposalRegenerationEvidenceResult result = AgentInteractionAuditInspection.GetProposalRegenerationEvidence(state, isAuthorized: true);
        return !isFresh && result.Evidence is { AttemptContext: not null } evidence
            ? result with { Evidence = evidence with { AttemptContext = null } }
            : result;
    }

    /// <inheritdoc />
    protected override object CreateNotAuthorizedPayload(string correlationId)
        => AgentProposalRegenerationEvidenceResult.NotAuthorized();

    /// <inheritdoc />
    protected override object CreateUnavailablePayload(string correlationId)
        => AgentProposalRegenerationEvidenceResult.NotFound();
}
