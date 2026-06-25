using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Shared fail-closed base for AgentInteraction audit query handlers.</summary>
public abstract class AgentInteractionAuditQueryHandlerBase(
    IAgentInteractionAuditStateReader reader,
    ITenantAccessReader tenantAccessReader) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IAgentInteractionAuditStateReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    private readonly ITenantAccessReader _tenantAccessReader = tenantAccessReader ?? throw new ArgumentNullException(nameof(tenantAccessReader));

    /// <inheritdoc />
    public string Domain => GetAgentInteractionStatusQuery.Domain;

    /// <inheritdoc />
    public abstract string QueryType { get; }

    /// <inheritdoc />
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return QueryResult.Failure("Forbidden");
        }

        bool isAuthorized = query.IsGlobalAdmin || await HasTenantAccessAsync(query, cancellationToken).ConfigureAwait(false);
        if (!isAuthorized)
        {
            return QueryResult.FromPayload(ToElement(CreateNotAuthorizedPayload(query.CorrelationId)));
        }

        AgentInteractionAuditStateReadResult read = await _reader
            .ReadAsync(query.TenantId, query.AggregateId, cancellationToken)
            .ConfigureAwait(false);

        if (!read.CanLoad)
        {
            return QueryResult.FromPayload(ToElement(CreateUnavailablePayload(query.CorrelationId)));
        }

        return QueryResult.FromPayload(ToElement(CreatePayload(read.State, query.CorrelationId, read.IsFresh)));
    }

    /// <summary>Creates the typed success payload from the rehydrated state.</summary>
    protected abstract object CreatePayload(AgentInteractionState? state, string correlationId, bool isFresh);

    /// <summary>Creates the typed unauthorized payload without reading state.</summary>
    protected abstract object CreateNotAuthorizedPayload(string correlationId);

    /// <summary>Creates the typed unavailable payload when the read dependency fails closed.</summary>
    protected abstract object CreateUnavailablePayload(string correlationId);

    /// <summary>Serializes the payload with contract enum names.</summary>
    protected static JsonElement ToElement<T>(T value)
        => JsonSerializer.SerializeToElement(value, s_jsonOptions);

    /// <summary>Maps a missing typed evidence object to a safe not-found operation result.</summary>
    protected static AgentOperationResult<T> EvidenceOperation<T>(T? value, string correlationId)
        where T : class
        => value is null
            ? AgentOperationResult<T>.Failed(AgentOperationErrorCode.NotFound, correlationId)
            : AgentOperationResult<T>.Succeeded(value, correlationId: correlationId);

    private async Task<bool> HasTenantAccessAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        TenantAccessReadResult access = await _tenantAccessReader
            .ReadAsync(query.TenantId, query.UserId, callerPartyId: string.Empty, cancellationToken)
            .ConfigureAwait(false);
        return access is { Outcome: AgentInteractionGateOutcome.Satisfied, IsFresh: true };
    }
}
