using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Agents.Server.Projections;

/// <summary>Compares catalog projections with their owning EventStore stream heads.</summary>
public static class ProviderCatalogReadFreshness
{
    /// <summary>Returns whether every relevant platform entry projection is authoritative.</summary>
    public static async Task<bool> IsPlatformCurrentAsync(
        IEventStoreGatewayClient? gateway,
        ProviderCatalogReadModel? platform,
        string? providerId,
        string? modelId,
        CancellationToken cancellationToken)
    {
        if (gateway is null)
        {
            return false;
        }

        if (platform is null)
        {
            return providerId is not null && modelId is not null
                && await HasHeadAsync(gateway, ProviderCatalogIdentity.PlatformTenantId,
                    ProviderCatalogAggregate.Domain, ProviderCatalogIdentity.EntryId(providerId, modelId),
                    0, cancellationToken).ConfigureAwait(false);
        }

        IEnumerable<string> entryIds = providerId is not null && modelId is not null
            ? [ProviderCatalogIdentity.EntryId(providerId, modelId)]
            : platform.StreamSequences.Keys.Concat(platform.Entries.Select(entry =>
                ProviderCatalogIdentity.EntryId(entry.ProviderId, entry.ModelId)));
        foreach (string entryId in entryIds.Distinct(StringComparer.Ordinal))
        {
            if (!await HasHeadAsync(gateway, ProviderCatalogIdentity.PlatformTenantId,
                ProviderCatalogAggregate.Domain, entryId,
                platform.StreamSequences.GetValueOrDefault(entryId), cancellationToken).ConfigureAwait(false))
            {
                return false;
            }
        }

        return platform.Entries.Count > 0 || providerId is not null;
    }

    /// <summary>Returns whether tenant enablement and relevant platform projections are authoritative.</summary>
    public static async Task<bool> IsTenantCurrentAsync(
        IEventStoreGatewayClient? gateway,
        string tenantId,
        ProviderCatalogReadModel? platform,
        TenantProviderEnablementReadModel? tenant,
        string? providerId,
        string? modelId,
        CancellationToken cancellationToken)
    {
        if (gateway is null || tenant is null
            || !await HasHeadAsync(gateway, tenantId, TenantProviderEnablementAggregate.Domain,
                tenantId, tenant.LastSequenceNumber, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        string? requestedKey = providerId is not null && modelId is not null
            ? ProviderCatalogState.EntryKey(providerId, modelId) : null;
        if (requestedKey is not null
            && (!tenant.State.Entries.TryGetValue(requestedKey, out TenantProviderEntryState? requested)
                || !requested.Enabled))
        {
            // Hidden and absent keys have identical tenant-facing truth, independent of the platform catalog.
            return true;
        }

        foreach (KeyValuePair<string, TenantProviderEntryState> pair in tenant.State.Entries)
        {
            if (!pair.Value.Enabled || requestedKey is not null && pair.Key != requestedKey)
            {
                continue;
            }

            ProviderCatalogEntryView? entry = platform?.Entries.FirstOrDefault(item =>
                ProviderCatalogState.EntryKey(item.ProviderId, item.ModelId) == pair.Key);
            if (entry is null)
            {
                return false;
            }

            string entryId = ProviderCatalogIdentity.EntryId(entry.ProviderId, entry.ModelId);
            if (!await HasHeadAsync(gateway, ProviderCatalogIdentity.PlatformTenantId,
                ProviderCatalogAggregate.Domain, entryId,
                platform!.StreamSequences.GetValueOrDefault(entryId), cancellationToken).ConfigureAwait(false))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Returns whether one stream head equals its projected sequence.</summary>
    public static async Task<bool> HasHeadAsync(
        IEventStoreGatewayClient? gateway,
        string tenantId,
        string domain,
        string aggregateId,
        long projectedSequence,
        CancellationToken cancellationToken)
    {
        if (gateway is null)
        {
            return false;
        }

        try
        {
            StreamReadPage page = await gateway.ReadStreamAsync(
                new StreamReadRequest(tenantId, domain, aggregateId, PageSize: 1), cancellationToken).ConfigureAwait(false);
            return string.Equals(page.Tenant, tenantId, StringComparison.Ordinal)
                && string.Equals(page.Domain, domain, StringComparison.Ordinal)
                && string.Equals(page.AggregateId, aggregateId, StringComparison.Ordinal)
                && page.Metadata.LatestSequence == projectedSequence;
        }
        catch (EventStoreGatewayException exception) when (projectedSequence == 0
            && exception.StatusCode == 404
            && string.Equals(exception.ReasonCode, StreamReplayReasonCodes.MissingStream, StringComparison.Ordinal))
        {
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }
    }
}
