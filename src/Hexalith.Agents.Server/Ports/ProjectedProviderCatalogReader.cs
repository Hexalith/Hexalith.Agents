using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Streams;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Live <see cref="IProviderCatalogReader"/> over the projected provider-catalog read model (Story 5.3).
/// Cross-tenant reads fail closed as <see cref="ProviderCatalogInspectionStatus.NotAuthorized"/> before the
/// store is addressed when the HTTP caller belongs to a different tenant.
/// </summary>
public sealed class ProjectedProviderCatalogReader(
    IReadModelStore readModelStore,
    IOptions<ProviderCatalogReadModelOptions> options,
    IAgentAdministrationContextProvider contextProvider,
    IEventStoreGatewayClient? gateway = null,
    TimeProvider? clock = null) : IProviderCatalogReader
{
    private readonly IReadModelStore _readModelStore = readModelStore
        ?? throw new ArgumentNullException(nameof(readModelStore));

    private readonly IOptions<ProviderCatalogReadModelOptions> _options = options
        ?? throw new ArgumentNullException(nameof(options));

    private readonly IAgentAdministrationContextProvider _contextProvider = contextProvider
        ?? throw new ArgumentNullException(nameof(contextProvider));

    private readonly IEventStoreGatewayClient? _gateway = gateway;
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    /// <inheritdoc />
    public async Task<ProviderCatalogEntryReadResult> GetEntryAsync(
        string tenantId,
        string providerId,
        string modelId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        AgentAdministrationContext context = _contextProvider.GetContext();
        if (!context.IsAuthorized)
        {
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.NotAuthorized, null);
        }

        if (!string.Equals(context.TenantId, tenantId, StringComparison.Ordinal))
        {
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.NotAuthorized, null);
        }

        ReadModelEntry<ProviderCatalogReadModel> entry;
        ReadModelEntry<TenantProviderEnablementReadModel> tenant;
        try
        {
            entry = await _readModelStore
                .GetAsync<ProviderCatalogReadModel>(
                    _options.Value.StateStoreName,
                    ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId),
                    ct)
                .ConfigureAwait(false);
            tenant = await _readModelStore.GetAsync<TenantProviderEnablementReadModel>(
                _options.Value.StateStoreName,
                TenantProviderEnablementReadModelAddresses.Detail(tenantId), ct).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A degraded store read is not a denial: the verdict maps a successful-shape empty entry to Unavailable.
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Unavailable, null);
        }

        ProviderCatalogEntryView? platformEntry = entry.Value?.Entries.FirstOrDefault(item =>
            item.ProviderId == providerId && item.ModelId == modelId);
        if (tenant.Value is null || !await IsTenantProjectionCurrentAsync(tenantId, tenant.Value, ct).ConfigureAwait(false))
        {
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Unavailable, null);
        }

        // Do not probe a named platform stream for a key this tenant cannot see. Hidden and absent
        // keys share the same result once the tenant projection is known to be current.
        string key = ProviderCatalogState.EntryKey(providerId, modelId);
        if (!tenant.Value.State.Entries.TryGetValue(key, out TenantProviderEntryState? enabled)
            || !enabled.Enabled)
        {
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null);
        }

        if (platformEntry is null)
        {
            bool? committed = await IsPlatformStreamCommittedAsync(providerId, modelId, ct).ConfigureAwait(false);
            return new ProviderCatalogEntryReadResult(committed is false
                ? ProviderCatalogInspectionStatus.EntryNotFound : ProviderCatalogInspectionStatus.Unavailable, null);
        }

        if (entry.Value is null || !await IsPlatformProjectionCurrentAsync(
            entry.Value, providerId, modelId, ct).ConfigureAwait(false))
        {
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Unavailable, null);
        }

        // The authoritative reads can straddle the exclusive grace deadline. Evaluate eligibility only
        // after both checkpoints have returned so a just-expired grace period cannot remain selectable.
        TenantProviderCatalogInspectionResult visible = TenantProviderCatalogViewFactory.CreateEntry(
            entry.Value, tenant.Value, authorized: true, providerId, modelId, _clock.GetUtcNow());
        if (visible.Status != ProviderCatalogInspectionStatus.Success || visible.Entries.Count != 1)
        {
            return new ProviderCatalogEntryReadResult(visible.Status, null);
        }

        ProviderCatalogInspectionResult result = platformEntry is null
            ? ProviderCatalogInspectionResult.NotFound()
            : ProviderCatalogInspectionResult.Success([platformEntry with
            {
                IsSelectableForNewActiveUse = visible.Entries[0].IsSelectableForNewActiveUse,
            }]);

        return result.Status switch
        {
            ProviderCatalogInspectionStatus.Success when result.Entries.Count == 1
                => new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, result.Entries[0]),
            ProviderCatalogInspectionStatus.EntryNotFound
                => new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null),
            ProviderCatalogInspectionStatus.NotAuthorized
                => new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.NotAuthorized, null),
            ProviderCatalogInspectionStatus.Unavailable
                => new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Unavailable, null),
            _ => new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Unavailable, null),
        };
    }

    private async Task<bool> IsPlatformProjectionCurrentAsync(
        ProviderCatalogReadModel projected,
        string providerId,
        string modelId,
        CancellationToken ct)
    {
        if (_gateway is null)
        {
            return false;
        }

        string aggregateId = ProviderCatalogIdentity.EntryId(providerId, modelId);
        try
        {
            StreamReadPage page = await _gateway.ReadStreamAsync(
                new StreamReadRequest(ProviderCatalogIdentity.PlatformTenantId,
                    ProviderCatalogReadModelAddresses.Domain, aggregateId, PageSize: 1), ct).ConfigureAwait(false);
            return string.Equals(page.Tenant, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal)
                && string.Equals(page.Domain, ProviderCatalogReadModelAddresses.Domain, StringComparison.Ordinal)
                && string.Equals(page.AggregateId, aggregateId, StringComparison.Ordinal)
                && page.Metadata.LatestSequence > 0
                && projected.StreamSequences.GetValueOrDefault(aggregateId) == page.Metadata.LatestSequence;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }
    }

    private async Task<bool?> IsPlatformStreamCommittedAsync(string providerId, string modelId, CancellationToken ct)
    {
        if (_gateway is null)
        {
            return null;
        }

        string aggregateId = ProviderCatalogIdentity.EntryId(providerId, modelId);
        try
        {
            StreamReadPage page = await _gateway.ReadStreamAsync(
                new StreamReadRequest(ProviderCatalogIdentity.PlatformTenantId,
                    ProviderCatalogReadModelAddresses.Domain, aggregateId, PageSize: 1), ct).ConfigureAwait(false);
            if (!string.Equals(page.Tenant, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal)
                || !string.Equals(page.Domain, ProviderCatalogReadModelAddresses.Domain, StringComparison.Ordinal)
                || !string.Equals(page.AggregateId, aggregateId, StringComparison.Ordinal))
            {
                return null;
            }

            return page.Metadata.LatestSequence > 0;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    private async Task<bool> IsTenantProjectionCurrentAsync(
        string tenantId,
        TenantProviderEnablementReadModel projected,
        CancellationToken ct)
    {
        if (_gateway is null)
        {
            return false;
        }

        try
        {
            StreamReadPage page = await _gateway.ReadStreamAsync(
                new StreamReadRequest(tenantId, TenantProviderEnablementAggregate.Domain, tenantId, PageSize: 1),
                ct).ConfigureAwait(false);
            return string.Equals(page.Tenant, tenantId, StringComparison.Ordinal)
                && string.Equals(page.Domain, TenantProviderEnablementAggregate.Domain, StringComparison.Ordinal)
                && string.Equals(page.AggregateId, tenantId, StringComparison.Ordinal)
                && page.Metadata.LatestSequence > 0
                && projected.LastSequenceNumber == page.Metadata.LatestSequence;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }
    }
}
