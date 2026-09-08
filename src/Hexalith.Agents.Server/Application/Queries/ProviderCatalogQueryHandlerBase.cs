using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>
/// Shared authorize-before-disclosure base for the live provider-catalog queries (Story 5.3 AC2, AC4).
/// Authorization runs before the read model is addressed, and a denied caller gets the same shaped
/// not-authorized result whether or not the catalog has entries.
/// </summary>
public abstract class ProviderCatalogQueryHandlerBase(
    IReadModelStore readModelStore,
    IOptions<ProviderCatalogReadModelOptions> options,
    ITenantAccessReader tenantAccessReader) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IReadModelStore _readModelStore = readModelStore ?? throw new ArgumentNullException(nameof(readModelStore));
    private readonly IOptions<ProviderCatalogReadModelOptions> _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ITenantAccessReader _tenantAccessReader = tenantAccessReader ?? throw new ArgumentNullException(nameof(tenantAccessReader));

    /// <inheritdoc />
    public string Domain => ProviderCatalogReadModelAddresses.Domain;

    /// <inheritdoc />
    public abstract string QueryType { get; }

    /// <inheritdoc />
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(query.UserId)
            || string.IsNullOrWhiteSpace(query.TenantId)
            || string.IsNullOrWhiteSpace(query.AggregateId))
        {
            return QueryResult.FromPayload(ToElement(ProviderCatalogInspectionResult.NotAuthorized()));
        }

        if (!await IsProviderAdminAsync(query, cancellationToken).ConfigureAwait(false))
        {
            return QueryResult.FromPayload(ToElement(ProviderCatalogInspectionResult.NotAuthorized()));
        }

        ReadModelEntry<ProviderCatalogReadModel> entry;
        try
        {
            entry = await _readModelStore
                .GetAsync<ProviderCatalogReadModel>(
                    _options.Value.StateStoreName,
                    ProviderCatalogReadModelAddresses.Detail(query.TenantId),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return QueryResult.FromPayload(ToElement(ProviderCatalogInspectionResult.Unavailable()));
        }

        return QueryResult.FromPayload(ToElement(CreateResult(entry.Value, query)));
    }

    /// <summary>Builds the authorized inspection result from the persisted read model.</summary>
    /// <param name="model">The persisted catalog read model, or <see langword="null"/>.</param>
    /// <param name="query">The query envelope.</param>
    /// <returns>The structured inspection result.</returns>
    protected abstract ProviderCatalogInspectionResult CreateResult(ProviderCatalogReadModel? model, QueryEnvelope query);

    /// <summary>Serializes the payload with contract enum names.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="value">The payload.</param>
    /// <returns>The serialized element.</returns>
    protected static JsonElement ToElement<T>(T value)
        => JsonSerializer.SerializeToElement(value, _jsonOptions);

    /// <summary>Deserializes a query payload, or returns <see langword="null"/> when it is missing or corrupt.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="payload">The raw payload.</param>
    /// <returns>The deserialized payload, or <see langword="null"/>.</returns>
    protected static T? ReadPayload<T>(byte[]? payload)
        where T : class
    {
        if (payload is null or { Length: 0 })
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<bool> IsProviderAdminAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        if (query.IsGlobalAdmin)
        {
            return true;
        }

        TenantAccessReadResult access = await _tenantAccessReader
            .ReadAsync(query.TenantId, query.UserId, callerPartyId: string.Empty, cancellationToken)
            .ConfigureAwait(false);
        return access is { Outcome: AgentInteractionGateOutcome.Satisfied, IsFresh: true };
    }
}
