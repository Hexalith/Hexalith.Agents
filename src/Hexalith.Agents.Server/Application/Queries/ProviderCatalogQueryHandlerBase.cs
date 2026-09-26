using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.Serialization;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.ProviderCatalog;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Gateway;
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
    ITenantAccessReader tenantAccessReader,
    IAgentAdministrationContextProvider contextProvider,
    IEventStoreGatewayClient? gateway = null,
    TimeProvider? clock = null) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        // The fallback factory must precede the string converter: options.Converters wins over a type-level
        // [JsonConverter], so without it every contract enum declaring the Unknown = 0 degradation would be
        // silently downgraded back to the throwing converter here.
        Converters = { new UnknownFallbackEnumConverterFactory(), new JsonStringEnumConverter() },
    };

    private readonly IReadModelStore _readModelStore = readModelStore ?? throw new ArgumentNullException(nameof(readModelStore));
    private readonly IOptions<ProviderCatalogReadModelOptions> _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ITenantAccessReader _tenantAccessReader = tenantAccessReader ?? throw new ArgumentNullException(nameof(tenantAccessReader));
    private readonly IAgentAdministrationContextProvider _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
    private readonly IEventStoreGatewayClient? _gateway = gateway;
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

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

        bool platformQuery = string.Equals(query.TenantId, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal);
        if (!await IsProviderAdminAsync(query, platformQuery, cancellationToken).ConfigureAwait(false))
        {
            return QueryResult.FromPayload(ToElement(ProviderCatalogInspectionResult.NotAuthorized()));
        }

        ReadModelEntry<ProviderCatalogReadModel> entry;
        TenantProviderEnablementReadModel? tenant = null;
        try
        {
            entry = await _readModelStore
                .GetAsync<ProviderCatalogReadModel>(
                    _options.Value.StateStoreName,
                    ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId),
                    cancellationToken)
                .ConfigureAwait(false);
            if (!platformQuery)
            {
                tenant = (await _readModelStore.GetAsync<TenantProviderEnablementReadModel>(
                    _options.Value.StateStoreName,
                    TenantProviderEnablementReadModelAddresses.Detail(query.TenantId),
                    cancellationToken).ConfigureAwait(false)).Value;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return QueryResult.FromPayload(ToElement(ProviderCatalogInspectionResult.Unavailable()));
        }

        (string? providerId, string? modelId) = GetRequestedEntry(query);
        bool current = platformQuery
            ? await ProviderCatalogReadFreshness.IsPlatformCurrentAsync(_gateway, entry.Value,
                providerId, modelId, cancellationToken).ConfigureAwait(false)
            : await ProviderCatalogReadFreshness.IsTenantCurrentAsync(_gateway, query.TenantId,
                entry.Value, tenant, providerId, modelId, cancellationToken).ConfigureAwait(false);
        if (current && !platformQuery)
        {
            // Freshness confirmed that a missing tenant read model has no enablement stream.
            tenant ??= new TenantProviderEnablementReadModel();
        }

        // Freshness reads may cross the exclusive grace deadline. Build eligibility from server time
        // after those reads, including when the result must be returned as pending.
        object result = platformQuery
            ? CreateResult(entry.Value, query)
            : CreateTenantResult(entry.Value, tenant, query, _clock.GetUtcNow());
        if (!current || platformQuery && providerId is null)
        {
            result = platformQuery
                ? ((ProviderCatalogInspectionResult)result) with
                {
                    Status = ProviderCatalogInspectionStatus.Success,
                    Entries = current && providerId is null
                        ? ((ProviderCatalogInspectionResult)result).Entries : [],
                    Freshness = AgentSetupFreshness.Stale,
                    TruthState = AgentSetupTruthState.AuthoritativePending,
                    ProjectedCommandMessageIds = null,
                }
                : ((TenantProviderCatalogInspectionResult)result) with
                {
                    Status = ProviderCatalogInspectionStatus.Success,
                    Entries = [],
                    Freshness = AgentSetupFreshness.Stale,
                    TruthState = AgentSetupTruthState.AuthoritativePending,
                    ProjectedCommandMessageIds = null,
                };
        }
        return QueryResult.FromPayload(JsonSerializer.SerializeToElement(result, result.GetType(), _jsonOptions));
    }

    /// <summary>Builds the authorized inspection result from the persisted read model.</summary>
    /// <param name="model">The persisted catalog read model, or <see langword="null"/>.</param>
    /// <param name="query">The query envelope.</param>
    /// <returns>The structured inspection result.</returns>
    protected abstract ProviderCatalogInspectionResult CreateResult(ProviderCatalogReadModel? model, QueryEnvelope query);

    /// <summary>Builds the configuration-free tenant result.</summary>
    protected abstract TenantProviderCatalogInspectionResult CreateTenantResult(
        ProviderCatalogReadModel? platform,
        TenantProviderEnablementReadModel? tenant,
        QueryEnvelope query,
        DateTimeOffset evaluatedAt);

    /// <summary>Gets the requested provider/model for a detail query, or nulls for a list.</summary>
    protected virtual (string? ProviderId, string? ModelId) GetRequestedEntry(QueryEnvelope query)
        => (null, null);

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

    private async Task<bool> IsProviderAdminAsync(QueryEnvelope query, bool platformQuery, CancellationToken cancellationToken)
    {
        if (platformQuery)
        {
            // EventStore's GlobalAdministrator flag is infrastructure authority, not Agents.PlatformOperator.
            // The latter must be independently proven by the authenticated host principal.
            AgentAdministrationContext actor = _contextProvider.GetContext();
            return query.IsGlobalAdmin && actor.IsPlatformOperator
                && !string.IsNullOrWhiteSpace(actor.ActorUserId)
                && string.Equals(actor.ActorUserId, query.UserId, StringComparison.Ordinal);
        }

        TenantAccessReadResult access = await _tenantAccessReader
            .ReadAsync(query.TenantId, query.UserId, callerPartyId: string.Empty, cancellationToken)
            .ConfigureAwait(false);
        return access is { Outcome: AgentInteractionGateOutcome.Satisfied, IsFresh: true };
    }
}
