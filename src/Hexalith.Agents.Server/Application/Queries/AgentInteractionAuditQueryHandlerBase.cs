using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.Serialization;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Shared fail-closed base for AgentInteraction audit query handlers.</summary>
public abstract class AgentInteractionAuditQueryHandlerBase(
    IAgentInteractionAuditStateReader reader,
    ITenantAccessReader tenantAccessReader,
    IAgentInteractionAuditAuthority? auditAuthority = null) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        // The fallback factory must precede the string converter: options.Converters wins over a type-level
        // [JsonConverter], so without it every contract enum declaring the Unknown = 0 degradation would be
        // silently downgraded back to the throwing converter here.
        Converters = { new UnknownFallbackEnumConverterFactory(), new JsonStringEnumConverter() },
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

        if (string.IsNullOrWhiteSpace(query.UserId) || string.IsNullOrWhiteSpace(query.TenantId)
            || string.IsNullOrWhiteSpace(query.AggregateId))
        {
            return QueryResult.Failure("Forbidden");
        }

        var target = new AgentAuditReadTarget(query.TenantId, query.UserId, query.AggregateId, QueryType);
        AgentInteractionAuditGrant? grant = await ReadGrantAsync(target, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (grant is null || !grant.Authorizes(target))
        {
            return QueryResult.FromPayload(ToElement(CreateNotAuthorizedPayload(query.CorrelationId)));
        }

        AgentInteractionAuditStateReadResult read;
        try
        {
            read = await _reader.ReadAsync(query.TenantId, query.AggregateId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            read = AgentInteractionAuditStateReadResult.Unavailable;
        }
        cancellationToken.ThrowIfCancellationRequested();

        if (!grant.Validity.IsCurrent)
        {
            return QueryResult.FromPayload(ToElement(CreateNotAuthorizedPayload(query.CorrelationId)));
        }

        // A scoped request does not prove the returned stream's scope. Verify authoritative provenance before
        // inspecting state or rendering any ids, evidence, counts, or availability from a misrouted read.
        if (!read.CanLoad || !string.Equals(read.SourceTenantId, query.TenantId, StringComparison.Ordinal)
            || !string.Equals(read.SourceAggregateId, query.AggregateId, StringComparison.Ordinal)
            || (read.State is { } state && !string.Equals(state.AgentInteractionId, query.AggregateId, StringComparison.Ordinal)))
        {
            return QueryResult.FromPayload(ToElement(CreateUnavailablePayload(query.CorrelationId)));
        }

        if (read.State is not null && !grant.Includes(read.State))
        {
            return QueryResult.FromPayload(ToElement(CreateNotAuthorizedPayload(query.CorrelationId)));
        }

        // A caller's own-call grant covers only the outcome. The full status view contains configuration,
        // routing and identity metadata reserved for operations users. Use the contract's empty defaults.
        object payload = grant.Audience == AgentAuditAudience.Caller && read.State is not null
            ? AgentInteractionAuditInspection.GetStatus(new AgentInteractionState
            {
                IsRequested = read.State.IsRequested,
                AgentInteractionId = read.State.AgentInteractionId,
                Status = read.State.Status,
            }, isAuthorized: true)
            : CreatePayload(read.State, query.CorrelationId, read.IsFresh);
        JsonElement rendered = ToElement(payload);
        return QueryResult.FromPayload(grant.Validity.IsCurrent ? rendered : ToElement(CreateNotAuthorizedPayload(query.CorrelationId)));
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

    private async Task<AgentInteractionAuditGrant?> ReadGrantAsync(AgentAuditReadTarget target, CancellationToken cancellationToken)
    {
        try
        {
            if (auditAuthority is not null)
            {
                return await auditAuthority.AuthorizeAsync(target, cancellationToken).ConfigureAwait(false);
            }

            // The built-in projection can establish tenant administration only. All other audiences
            // require current resource-scoped evidence supplied by the host.
            if (!AgentInteractionAuditGrant.IsOperationalQuery(target.QueryType))
            {
                return null;
            }

            TenantAccessReadResult access = await _tenantAccessReader
                .ReadAsync(target.TenantId, target.Subject, callerPartyId: string.Empty, cancellationToken)
                .ConfigureAwait(false);
            return access is { HasFreshGrant: true, Authority: { } validity, CurrentActor: { } actor }
                && actor.Subject == target.Subject && actor.TenantId == target.TenantId
                ? new(target, AgentAuditAudience.TenantOperations, validity)
                : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
    }
}
