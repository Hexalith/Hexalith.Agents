using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Serves safe AgentInteraction context evidence reads.</summary>
public sealed class GetAgentInteractionContextEvidenceQueryHandler(IAgentInteractionAuditStateReader reader, ITenantAccessReader tenantAccessReader,
    IAgentInteractionAuditAuthority? auditAuthority = null)
    : AgentInteractionAuditQueryHandlerBase(reader, tenantAccessReader, auditAuthority)
{
    /// <inheritdoc />
    public override string QueryType => GetAgentInteractionContextEvidenceQuery.QueryType;

    /// <inheritdoc />
    protected override object CreatePayload(AgentInteractionState? state, string correlationId, bool isFresh)
    {
        AgentInteractionContextEvidenceResult result = AgentInteractionAuditInspection.GetContextEvidence(state, isAuthorized: true);
        return !isFresh && result.Evidence is { GenerationAttemptContext: not null } evidence
            ? result with { Evidence = evidence with { GenerationAttemptContext = null } }
            : result;
    }

    /// <inheritdoc />
    protected override object CreateNotAuthorizedPayload(string correlationId)
        => AgentInteractionContextEvidenceResult.NotAuthorized();

    /// <inheritdoc />
    protected override object CreateUnavailablePayload(string correlationId)
        => AgentInteractionContextEvidenceResult.NotFound();
}
