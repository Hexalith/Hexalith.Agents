using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Queries;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>
/// Shared authorize-before-disclosure base for the live Agent setup queries (Story 5.2 AC2, AC4). Both the status
/// and the configuration query serve the same authoritative <see cref="AgentSetupResult"/> read from the persisted
/// setup read model, so the API, the public client, and FrontComposer cannot drift apart.
/// </summary>
/// <remarks>
/// Authorization runs before the read model is even addressed, and a denied caller gets the same shaped
/// not-authorized result whether or not the Agent exists — so a probe cannot distinguish "no permission" from "no
/// such Agent", and cannot learn another tenant's Agent exists (AC4). The read-model key is tenant-scoped, so
/// cross-tenant disclosure is structurally impossible even if authorization were mis-wired.
/// </remarks>
public abstract class AgentSetupQueryHandlerBase(
    IReadModelStore readModelStore,
    IOptions<AgentSetupReadModelOptions> options,
    ITenantAccessReader tenantAccessReader) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IReadModelStore _readModelStore = readModelStore ?? throw new ArgumentNullException(nameof(readModelStore));
    private readonly IOptions<AgentSetupReadModelOptions> _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ITenantAccessReader _tenantAccessReader = tenantAccessReader ?? throw new ArgumentNullException(nameof(tenantAccessReader));

    /// <inheritdoc />
    public string Domain => AgentSetupReadModelAddresses.Domain;

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
            return QueryResult.FromPayload(ToElement(AgentSetupResult.NotAuthorized()));
        }

        if (!await IsAgentsAdminAsync(query, cancellationToken).ConfigureAwait(false))
        {
            return QueryResult.FromPayload(ToElement(AgentSetupResult.NotAuthorized()));
        }

        ReadModelEntry<AgentSetupReadModel> entry = await _readModelStore
            .GetAsync<AgentSetupReadModel>(
                _options.Value.StateStoreName,
                AgentSetupReadModelAddresses.Detail(query.TenantId, query.AggregateId),
                cancellationToken)
            .ConfigureAwait(false);

        return QueryResult.FromPayload(ToElement(AgentSetupViewFactory.Create(
            entry.Value,
            query.TenantId,
            query.AggregateId,
            ReadExpectedConfigurationVersion(query.Payload),
            isAgentsAdmin: true)));
    }

    /// <summary>Serializes the payload with contract enum names.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="value">The payload.</param>
    /// <returns>The serialized element.</returns>
    protected static JsonElement ToElement<T>(T value)
        => JsonSerializer.SerializeToElement(value, _jsonOptions);

    // Both setup queries carry the same single optional field, so one tolerant reader serves both: a missing,
    // empty, or unparseable payload simply means "no expectation", never a failed read.
    private static int? ReadExpectedConfigurationVersion(byte[]? payload)
    {
        if (payload is null or { Length: 0 })
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<GetAgentStatusQuery>(payload, _jsonOptions)?.ExpectedConfigurationVersion;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<bool> IsAgentsAdminAsync(QueryEnvelope query, CancellationToken cancellationToken)
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

/// <summary>Serves the live <see cref="GetAgentStatusQuery"/> from the persisted setup read model.</summary>
public sealed class GetAgentStatusQueryHandler(
    IReadModelStore readModelStore,
    IOptions<AgentSetupReadModelOptions> options,
    ITenantAccessReader tenantAccessReader)
    : AgentSetupQueryHandlerBase(readModelStore, options, tenantAccessReader)
{
    /// <inheritdoc />
    public override string QueryType => GetAgentStatusQuery.QueryType;
}

/// <summary>Serves the live <see cref="GetAgentConfigurationQuery"/> from the persisted setup read model.</summary>
public sealed class GetAgentConfigurationQueryHandler(
    IReadModelStore readModelStore,
    IOptions<AgentSetupReadModelOptions> options,
    ITenantAccessReader tenantAccessReader)
    : AgentSetupQueryHandlerBase(readModelStore, options, tenantAccessReader)
{
    /// <inheritdoc />
    public override string QueryType => GetAgentConfigurationQuery.QueryType;
}
