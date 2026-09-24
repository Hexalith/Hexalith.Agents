using System.Text.Json;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>Migrates an explicit, complete legacy catalog inventory through EventStore commands.</summary>
public sealed class ProviderCatalogMigrationService(
    ProviderCatalogAdministrationOrchestrator catalog,
    IAgentAdministrationContextProvider contextProvider,
    IAgentCommandIdentityFactory identities,
    IReadModelStore readModels,
    IOptions<ProviderCatalogReadModelOptions> options,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    /// <summary>Reads persisted legacy catalogs, preflights targets, and dispatches only missing targets.</summary>
    public async Task<ProviderCatalogMigrationResult> MigrateAsync(
        IReadOnlyCollection<string> legacyTenantIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(legacyTenantIds);
        AgentAdministrationContext actor = contextProvider.GetContext();
        if (!actor.IsPlatformOperator || string.IsNullOrWhiteSpace(actor.ActorUserId))
        {
            return new("NotAuthorized", 0, 0);
        }

        string[] tenantIds = [.. legacyTenantIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        if (tenantIds.Length == 0 || tenantIds.Any(string.IsNullOrWhiteSpace)
            || tenantIds.Contains(ProviderCatalogIdentity.PlatformTenantId, StringComparer.Ordinal))
        {
            return new("InvalidLegacyInventory", 0, 0);
        }

        ProviderCatalogReadModel[] ordered = new ProviderCatalogReadModel[tenantIds.Length];
        for (int index = 0; index < tenantIds.Length; index++)
        {
            ordered[index] = (await readModels.GetAsync<ProviderCatalogReadModel>(options.Value.StateStoreName,
                ProviderCatalogReadModelAddresses.Detail(tenantIds[index]), cancellationToken).ConfigureAwait(false)).Value!;
            if (ordered[index] is null)
            {
                return new("InvalidLegacyInventory", 0, 0);
            }
        }
        if (ordered.Where((item, index) => string.IsNullOrWhiteSpace(item.TenantId)
            || !string.Equals(item.TenantId, tenantIds[index], StringComparison.Ordinal)
            || !string.Equals(item.CatalogId, item.TenantId, StringComparison.Ordinal)).Any())
        {
            return new("InvalidLegacyInventory", 0, 0);
        }

        if (ordered.SelectMany(model => model.Entries).Any(entry => entry.Pricing is null
            || entry.DataHandling is not null
                && ProviderDataHandlingPolicy.Validate(entry.DataHandling, current: null, allowHistoricalVersion: true) is not null))
        {
            return new("InvalidLegacyInventory", 0, 0);
        }

        var groups = ordered.SelectMany(model => model.Entries.Select(entry => (model, entry)))
            .GroupBy(item => ProviderCatalogState.EntryKey(item.entry.ProviderId, item.entry.ModelId), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal).ToArray();
        foreach (var group in groups)
        {
            string[] fingerprints = [.. group.Select(item => MetadataFingerprint(item.entry)).Distinct(StringComparer.Ordinal)];
            if (fingerprints.Length != 1)
            {
                return new("DivergentLegacyMetadata", 0, 0);
            }
        }

        ProviderCatalogReadModel? platform = await ReadPlatformAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<string, TenantProviderEnablementReadModel?> tenantTargets = new(StringComparer.Ordinal);
        foreach (string tenantId in tenantIds)
        {
            tenantTargets[tenantId] = await ReadTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }

        // Resolve all existing-target conflicts before the first dispatch. A partial migration never overwrites
        // a different target, and an exact repeat has no work to submit.
        foreach (var group in groups)
        {
            ProviderCatalogEntryView source = group.First().entry;
            string provenance = PlatformProvenance(source);
            ProviderCatalogEntryView? target = Find(platform, source);
            if (target is not null && !PlatformMatches(target, source, provenance))
            {
                return new("TargetConflict", 0, 0);
            }

            foreach (var (model, entry) in group)
            {
                TenantProviderEntryState? tenantTarget = Find(tenantTargets[model.TenantId], entry);
                if (tenantTarget is not null && !TenantMatches(tenantTarget, entry, TenantProvenance(model)))
                {
                    return new("TargetConflict", 0, 0);
                }
            }
        }

        int platformCount = 0;
        int tenantCount = 0;
        foreach (var group in groups)
        {
            ProviderCatalogEntryView entry = group.First().entry;
            string provenance = PlatformProvenance(entry);
            bool platformEnabled = group.Any(item => CanEnable(item.entry));
            var create = new CreateProviderModelEntry(
                entry.ProviderId, entry.ModelId, entry.DisplayLabel,
                platformEnabled,
                entry.SupportsTextGeneration,
                entry.ContextWindowTokenLimit,
                entry.MaxOutputTokenLimit,
                entry.TimeoutPolicy,
                entry.SafeCapabilityFlags,
                entry.ConfigurationReferenceId,
                entry.Pricing!,
                entry.DataHandling,
                provenance,
                entry.CapabilityVersion);
            if (Find(platform, entry) is null)
            {
                ProviderCatalogAdministrationRequest request = Request(actor, ProviderCatalogIdentity.PlatformTenantId);
                AgentAdministrationOutcome outcome = await catalog.CreateAsync(request, create, cancellationToken).ConfigureAwait(false);
                if (!outcome.Authorized || !outcome.Dispatched)
                {
                    return new("DispatchFailed", platformCount, tenantCount);
                }

                platformCount++;
                if (!await WaitForPlatformAsync(entry, provenance, platformEnabled, cancellationToken).ConfigureAwait(false))
                {
                    return new("AuthoritativePending", platformCount, tenantCount);
                }

                platform = await ReadPlatformAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (platformEnabled && Find(platform, entry)?.Status == ProviderModelStatus.Disabled)
            {
                AgentAdministrationOutcome outcome = await catalog.EnableAsync(
                    Request(actor, ProviderCatalogIdentity.PlatformTenantId),
                    new EnableProviderModelEntry(entry.ProviderId, entry.ModelId), cancellationToken).ConfigureAwait(false);
                if (!outcome.Authorized || !outcome.Dispatched)
                {
                    return new("DispatchFailed", platformCount, tenantCount);
                }

                platformCount++;
                if (!await WaitForPlatformAsync(entry, provenance, enabled: true, cancellationToken).ConfigureAwait(false))
                {
                    return new("AuthoritativePending", platformCount, tenantCount);
                }

                platform = await ReadPlatformAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var (model, tenantEntry) in group)
            {
                if (Find(tenantTargets[model.TenantId], tenantEntry) is not null)
                {
                    continue;
                }

                var enablement = new SetTenantProviderModelEnablement(
                    model.TenantId, tenantEntry.ProviderId, tenantEntry.ModelId,
                    CanEnable(tenantEntry),
                    ExpectedRevision: tenantTargets[model.TenantId]?.State.Revision ?? 0,
                    tenantEntry.DataHandling,
                    TenantProvenance(model));
                AgentAdministrationOutcome outcome = await catalog.SetTenantEnablementAsync(Request(actor, model.TenantId), enablement, cancellationToken).ConfigureAwait(false);
                if (!outcome.Authorized || !outcome.Dispatched)
                {
                    return new("DispatchFailed", platformCount, tenantCount);
                }

                tenantCount++;
                if (!await WaitForTenantAsync(model.TenantId, tenantEntry, TenantProvenance(model), cancellationToken).ConfigureAwait(false))
                {
                    return new("AuthoritativePending", platformCount, tenantCount);
                }

                tenantTargets[model.TenantId] = await ReadTenantAsync(model.TenantId, cancellationToken).ConfigureAwait(false);
            }
        }

        return new(platformCount == 0 && tenantCount == 0 ? "NoOp" : "ProjectionConfirmed", platformCount, tenantCount);
    }

    private ProviderCatalogAdministrationRequest Request(AgentAdministrationContext actor, string tenantId)
        => new(identities.NewMessageId(), identities.NewCorrelationId(), tenantId, actor.ActorUserId,
            IsProviderAdmin: false, IsPlatformOperator: true);

    private Task<ProviderCatalogReadModel?> ReadPlatformAsync(CancellationToken ct)
        => ReadAsync<ProviderCatalogReadModel>(ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), ct);

    private Task<TenantProviderEnablementReadModel?> ReadTenantAsync(string tenantId, CancellationToken ct)
        => ReadAsync<TenantProviderEnablementReadModel>(TenantProviderEnablementReadModelAddresses.Detail(tenantId), ct);

    private async Task<T?> ReadAsync<T>(string key, CancellationToken ct)
        where T : class
        => (await readModels.GetAsync<T>(options.Value.StateStoreName, key, ct).ConfigureAwait(false)).Value;

    private async Task<bool> WaitForPlatformAsync(ProviderCatalogEntryView source, string provenance, bool enabled, CancellationToken ct)
    {
        DateTimeOffset deadline = _clock.GetUtcNow().AddSeconds(8);
        do
        {
            ProviderCatalogEntryView? target = Find(await ReadPlatformAsync(ct).ConfigureAwait(false), source);
            if (target is not null && PlatformMatches(target, source, provenance)
                && target.Status == (enabled ? ProviderModelStatus.Enabled : ProviderModelStatus.Disabled))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), _clock, ct).ConfigureAwait(false);
        }
        while (_clock.GetUtcNow() < deadline);
        return false;
    }

    private async Task<bool> WaitForTenantAsync(string tenantId, ProviderCatalogEntryView source, string provenance, CancellationToken ct)
    {
        DateTimeOffset deadline = _clock.GetUtcNow().AddSeconds(8);
        do
        {
            TenantProviderEntryState? target = Find(await ReadTenantAsync(tenantId, ct).ConfigureAwait(false), source);
            if (target is not null)
            {
                return TenantMatches(target, source, provenance);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), _clock, ct).ConfigureAwait(false);
        }
        while (_clock.GetUtcNow() < deadline);
        return false;
    }

    private static ProviderCatalogEntryView? Find(ProviderCatalogReadModel? model, ProviderCatalogEntryView source)
        => model?.Entries.FirstOrDefault(item => item.ProviderId == source.ProviderId && item.ModelId == source.ModelId);

    private static TenantProviderEntryState? Find(TenantProviderEnablementReadModel? model, ProviderCatalogEntryView source)
        => model?.State.Entries.GetValueOrDefault(ProviderCatalogState.EntryKey(source.ProviderId, source.ModelId));

    private static bool PlatformMatches(ProviderCatalogEntryView target, ProviderCatalogEntryView source, string provenance)
        => string.Equals(target.MigratedFrom, provenance, StringComparison.Ordinal)
            && string.Equals(MetadataFingerprint(target), MetadataFingerprint(source), StringComparison.Ordinal);

    private static bool TenantMatches(TenantProviderEntryState target, ProviderCatalogEntryView source, string provenance)
        => target.Enabled == CanEnable(source)
            && string.Equals(target.MigratedFrom, provenance, StringComparison.Ordinal);

    private static bool CanEnable(ProviderCatalogEntryView source)
        => source.Status == ProviderModelStatus.Enabled
            && source.DataHandling is { DataHandlingVersion: >= 1 } terms
            && ProviderDataHandlingPolicy.Validate(terms, current: null, allowHistoricalVersion: true) is null;

    private static string TenantProvenance(ProviderCatalogReadModel model)
        => $"legacy:{model.TenantId}:{model.CatalogId}";

    private static string PlatformProvenance(ProviderCatalogEntryView entry)
        => $"legacy:provider-model:{ProviderCatalogIdentity.EntryId(entry.ProviderId, entry.ModelId)}";

    private static string MetadataFingerprint(ProviderCatalogEntryView view)
        => JsonSerializer.Serialize(view with
        {
            Status = ProviderModelStatus.Disabled,
            IsSelectableForNewActiveUse = false,
            MigratedFrom = null,
            DataHandlingHistory = null,
        });
}
