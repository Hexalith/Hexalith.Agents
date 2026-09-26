using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Gateway;

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

        // A tenant never enabled for any model has no enablement stream or read model; a missing stream at
        // sequence zero is current empty truth, while an existing unprojected stream stays unavailable.
        TenantProviderEnablementReadModel projectedTenant = tenant.Value ?? new TenantProviderEnablementReadModel();
        if (!await ProviderCatalogReadFreshness.HasHeadAsync(_gateway, tenantId, TenantProviderEnablementAggregate.Domain,
            tenantId, projectedTenant.LastSequenceNumber, ct).ConfigureAwait(false))
        {
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Unavailable, null);
        }

        // Do not probe a named platform stream for a key this tenant cannot see. Hidden and absent
        // keys share the same result once the tenant projection is known to be current.
        string key = ProviderCatalogState.EntryKey(providerId, modelId);
        if (!projectedTenant.State.Entries.TryGetValue(key, out TenantProviderEntryState? enabled)
            || !enabled.Enabled)
        {
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null);
        }

        // A platform stream at its projected sequence (missing, or holding only a rejected create) has no entry;
        // a committed but unprojected create stays unavailable rather than absent.
        string entryId = ProviderCatalogIdentity.EntryId(providerId, modelId);
        if (!await ProviderCatalogReadFreshness.HasHeadAsync(_gateway, ProviderCatalogIdentity.PlatformTenantId,
            ProviderCatalogAggregate.Domain, entryId, entry.Value?.StreamSequences.GetValueOrDefault(entryId) ?? 0, ct)
            .ConfigureAwait(false))
        {
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Unavailable, null);
        }

        if (platformEntry is null)
        {
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null);
        }

        // The authoritative reads can straddle the exclusive grace deadline. Evaluate eligibility only
        // after both checkpoints have returned so a just-expired grace period cannot remain selectable.
        TenantProviderCatalogInspectionResult visible = TenantProviderCatalogViewFactory.CreateEntry(
            entry.Value, projectedTenant, authorized: true, providerId, modelId, _clock.GetUtcNow());
        return visible.Status == ProviderCatalogInspectionStatus.Success && visible.Entries.Count == 1
            ? new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, platformEntry with
            {
                IsSelectableForNewActiveUse = visible.Entries[0].IsSelectableForNewActiveUse,
            })
            : new ProviderCatalogEntryReadResult(visible.Status, null);
    }
}
