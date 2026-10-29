using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Queries;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.Serialization;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Application.Agents;
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
/// such Agent", and cannot learn another tenant's Agent exists (AC4). The tenant-scoped read-model key and the
/// view factory's embedded-identity validation prevent a misrouted projection from disclosing another scope.
/// </remarks>
public abstract class AgentSetupQueryHandlerBase(
    IReadModelStore readModelStore,
    IOptions<AgentSetupReadModelOptions> options,
    ITenantAccessReader tenantAccessReader,
    IApproverPolicyResolver? approverResolver = null,
    IAgentAdministrationContextProvider? contextProvider = null) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        // The fallback factory must precede the string converter: options.Converters wins over a type-level
        // [JsonConverter], so without it every contract enum declaring the Unknown = 0 degradation would be
        // silently downgraded back to the throwing converter here.
        Converters = { new UnknownFallbackEnumConverterFactory(), new JsonStringEnumConverter() },
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

        (AgentAdministrationContext? actor, TenantAccessReadResult? access) = await ReadAuthorityAsync(query, cancellationToken).ConfigureAwait(false);
        bool IsAuthorized() => actor?.IsAuthorized == true
            && access is { Outcome: AgentInteractionGateOutcome.Satisfied, IsFresh: true }
            && access.Authority?.IsCurrent != false;
        if (!IsAuthorized())
        {
            return QueryResult.FromPayload(ToElement(AgentSetupResult.NotAuthorized()));
        }

        ReadModelEntry<AgentSetupReadModel> entry;
        try
        {
            entry = await _readModelStore
                .GetAsync<AgentSetupReadModel>(
                    _options.Value.StateStoreName,
                    AgentSetupReadModelAddresses.Detail(query.TenantId, query.AggregateId),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return QueryResult.FromPayload(ToElement(IsAuthorized() ? AgentSetupResult.Unavailable() : AgentSetupResult.NotAuthorized()));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!IsAuthorized())
        {
            return QueryResult.FromPayload(ToElement(AgentSetupResult.NotAuthorized()));
        }

        AgentSetupResult setup = await AgentSetupBasisEnricher.EnrichAsync(
            AgentSetupViewFactory.Create(entry.Value, query.TenantId, query.AggregateId,
                ReadExpectedConfigurationVersion(query.Payload), isAgentsAdmin: true),
            entry.Value, approverResolver, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return QueryResult.FromPayload(ToElement(IsAuthorized() ? setup : AgentSetupResult.NotAuthorized()));
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

    private async Task<(AgentAdministrationContext? Actor, TenantAccessReadResult? Access)> ReadAuthorityAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        if (contextProvider is null)
        {
            return (null, null);
        }

        AgentAdministrationContext actor;
        try
        {
            // Query UserId is the authenticated token subject; current authority may resolve it to a different
            // stable Parties actor. Bind the subject and both tenant scopes before disclosing setup details.
            AgentAdministrationContext identity = contextProvider.GetContext();
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(identity.ActorUserId, query.UserId, StringComparison.Ordinal)
                || !string.Equals(identity.TenantId, query.TenantId, StringComparison.Ordinal))
            {
                return (null, null);
            }

            actor = await contextProvider
                .GetContextAsync(false, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!actor.IsAuthorized || !string.Equals(actor.TenantId, query.TenantId, StringComparison.Ordinal))
            {
                return (null, null);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return (null, null);
        }

        TenantAccessReadResult access = await _tenantAccessReader
            .ReadAsync(query.TenantId, query.UserId, callerPartyId: string.Empty, cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return (actor, access);
    }
}
