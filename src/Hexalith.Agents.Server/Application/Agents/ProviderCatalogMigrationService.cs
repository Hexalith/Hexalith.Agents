using System.Text.Json;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>Migrates an explicit, complete legacy catalog inventory through EventStore commands.</summary>
public sealed class ProviderCatalogMigrationService(
    ProviderCatalogAdministrationOrchestrator catalog,
    IAgentAdministrationContextProvider contextProvider,
    IAgentCommandIdentityFactory identities,
    IReadModelStore readModels,
    IOptions<ProviderCatalogReadModelOptions> options,
    TimeProvider? clock = null,
    IEventStoreGatewayClient? gateway = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly IEventStoreGatewayClient? _gateway = gateway;

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

        // A legacy projection is only a migration source after its own EventStore stream has caught up.
        // The legacy write path is frozen, but a command committed before the freeze may still be delivered.
        for (int index = 0; index < tenantIds.Length; index++)
        {
            if (!await IsLegacyProjectionCurrentAsync(tenantIds[index], ordered[index], cancellationToken).ConfigureAwait(false))
            {
                return new("AuthoritativePending", 0, 0);
            }
        }
        Dictionary<string, long> sourceCheckpoints = tenantIds.Select((tenantId, index) =>
                new KeyValuePair<string, long>(tenantId, LegacyCheckpoint(tenantId, ordered[index])))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        if (ordered.SelectMany(model => model.Entries).Any(entry => entry.Pricing is not { } pricing
            || !Iso4217CurrencyCodes.IsValid(pricing.Currency)
            || pricing.InputTokenUnitPrice < 0 || pricing.OutputTokenUnitPrice < 0
            || pricing.PricingVersion < 0
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
        Dictionary<string, long> platformCheckpoints = new(StringComparer.Ordinal);
        Dictionary<string, long> tenantCheckpoints = tenantIds.ToDictionary(
            tenantId => tenantId,
            tenantId => tenantTargets[tenantId]?.LastSequenceNumber ?? 0,
            StringComparer.Ordinal);

        // Preflight never treats a projected target as authoritative until the target stream agrees.
        // This also detects a committed create whose projection has not appeared yet.
        foreach (var group in groups)
        {
            ProviderCatalogEntryView source = group.First().entry;
            string aggregateId = ProviderCatalogIdentity.EntryId(source.ProviderId, source.ModelId);
            platformCheckpoints[aggregateId] = platform?.StreamSequences.GetValueOrDefault(aggregateId) ?? 0;
            if (!await IsTargetCurrentAsync(ProviderCatalogIdentity.PlatformTenantId,
                ProviderCatalogAggregate.Domain, aggregateId,
                platformCheckpoints[aggregateId], cancellationToken).ConfigureAwait(false))
            {
                return new("AuthoritativePending", 0, 0);
            }
        }

        foreach (string tenantId in tenantIds)
        {
            if (!await IsTargetCurrentAsync(tenantId, TenantProviderEnablementAggregate.Domain, tenantId,
                tenantTargets[tenantId]?.LastSequenceNumber ?? 0, cancellationToken).ConfigureAwait(false))
            {
                return new("AuthoritativePending", 0, 0);
            }
        }

        // Resolve all existing-target conflicts before the first dispatch. A partial migration never overwrites
        // a different target, and an exact repeat has no work to submit.
        foreach (var group in groups)
        {
            ProviderCatalogEntryView source = group.First().entry;
            string provenance = PlatformProvenance(source);
            ProviderCatalogEntryView? target = Find(platform, source);
            bool expectedEnabled = group.Any(item => CanEnable(item.entry));
            if (target is not null
                && !PlatformMatches(target, source, provenance, expectedEnabled)
                && !(expectedEnabled && target.Status == ProviderModelStatus.Disabled
                    && PlatformMetadataMatches(target, source, provenance)))
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
                string commandResult = await WaitForCommandAsync(request, outcome, cancellationToken).ConfigureAwait(false);
                if (commandResult != "Completed")
                {
                    return new(commandResult, platformCount, tenantCount);
                }
                if (!await WaitForPlatformAsync(entry, provenance, platformEnabled,
                    request.MessageId, cancellationToken).ConfigureAwait(false))
                {
                    return new("AuthoritativePending", platformCount, tenantCount);
                }

                platform = await ReadPlatformAsync(cancellationToken).ConfigureAwait(false);
                platformCheckpoints[ProviderCatalogIdentity.EntryId(entry.ProviderId, entry.ModelId)] =
                    platform?.StreamSequences.GetValueOrDefault(ProviderCatalogIdentity.EntryId(entry.ProviderId, entry.ModelId)) ?? 0;
            }
            else if (platformEnabled && Find(platform, entry)?.Status == ProviderModelStatus.Disabled)
            {
                ProviderCatalogAdministrationRequest request = Request(actor, ProviderCatalogIdentity.PlatformTenantId);
                AgentAdministrationOutcome outcome = await catalog.EnableAsync(
                    request,
                    new EnableProviderModelEntry(entry.ProviderId, entry.ModelId,
                        Find(platform, entry)?.LifecycleRevision), cancellationToken).ConfigureAwait(false);
                if (!outcome.Authorized || !outcome.Dispatched)
                {
                    return new("DispatchFailed", platformCount, tenantCount);
                }

                platformCount++;
                string commandResult = await WaitForCommandAsync(request, outcome, cancellationToken).ConfigureAwait(false);
                if (commandResult != "Completed")
                {
                    return new(commandResult, platformCount, tenantCount);
                }
                if (!await WaitForPlatformAsync(entry, provenance, enabled: true,
                    request.MessageId, cancellationToken).ConfigureAwait(false))
                {
                    return new("AuthoritativePending", platformCount, tenantCount);
                }

                platform = await ReadPlatformAsync(cancellationToken).ConfigureAwait(false);
                platformCheckpoints[ProviderCatalogIdentity.EntryId(entry.ProviderId, entry.ModelId)] =
                    platform?.StreamSequences.GetValueOrDefault(ProviderCatalogIdentity.EntryId(entry.ProviderId, entry.ModelId)) ?? 0;
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
                ProviderCatalogAdministrationRequest request = Request(actor, model.TenantId);
                AgentAdministrationOutcome outcome = await catalog.SetTenantEnablementAsync(request, enablement, cancellationToken).ConfigureAwait(false);
                if (!outcome.Authorized || !outcome.Dispatched)
                {
                    return new("DispatchFailed", platformCount, tenantCount);
                }

                tenantCount++;
                string commandResult = await WaitForCommandAsync(request, outcome, cancellationToken).ConfigureAwait(false);
                if (commandResult != "Completed")
                {
                    return new(commandResult, platformCount, tenantCount);
                }
                if (!await WaitForTenantAsync(model.TenantId, tenantEntry, TenantProvenance(model),
                    request.MessageId, cancellationToken).ConfigureAwait(false))
                {
                    return new("AuthoritativePending", platformCount, tenantCount);
                }

                tenantTargets[model.TenantId] = await ReadTenantAsync(model.TenantId, cancellationToken).ConfigureAwait(false);
                tenantCheckpoints[model.TenantId] = tenantTargets[model.TenantId]?.LastSequenceNumber ?? 0;
            }
        }

        // A target checked early in a multi-entry run may have changed while later targets were dispatched.
        // Re-read every target before reporting a fully confirmed migration.
        ProviderCatalogReadModel? finalPlatform = await ReadPlatformAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<string, TenantProviderEnablementReadModel?> finalTenants = new(StringComparer.Ordinal);
        foreach (string tenantId in tenantIds)
        {
            finalTenants[tenantId] = await ReadTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
            if (finalTenants[tenantId]?.LastSequenceNumber != tenantCheckpoints[tenantId]
                || !await IsTargetCurrentAsync(tenantId, TenantProviderEnablementAggregate.Domain, tenantId,
                    tenantCheckpoints[tenantId], cancellationToken).ConfigureAwait(false))
            {
                return new("AuthoritativePending", platformCount, tenantCount);
            }
        }

        foreach (var group in groups)
        {
            ProviderCatalogEntryView source = group.First().entry;
            string aggregateId = ProviderCatalogIdentity.EntryId(source.ProviderId, source.ModelId);
            if (finalPlatform?.StreamSequences.GetValueOrDefault(aggregateId) != platformCheckpoints[aggregateId]
                || !await IsTargetCurrentAsync(ProviderCatalogIdentity.PlatformTenantId,
                ProviderCatalogAggregate.Domain, aggregateId,
                platformCheckpoints[aggregateId],
                cancellationToken).ConfigureAwait(false))
            {
                return new("AuthoritativePending", platformCount, tenantCount);
            }

            ProviderCatalogEntryView? target = Find(finalPlatform, source);
            if (target is null || !PlatformMatches(target, source, PlatformProvenance(source),
                group.Any(item => CanEnable(item.entry))))
            {
                return new("TargetConflict", platformCount, tenantCount);
            }

            foreach (var (model, entry) in group)
            {
                TenantProviderEntryState? tenantTarget = Find(finalTenants[model.TenantId], entry);
                if (tenantTarget is null || !TenantMatches(tenantTarget, entry, TenantProvenance(model)))
                {
                    return new("TargetConflict", platformCount, tenantCount);
                }
            }
        }

        // A legacy command admitted before the write freeze can commit during dispatch. Recheck the
        // original source heads after all targets; an equal-looking later projection is not the source copied.
        foreach (string tenantId in tenantIds)
        {
            if (!await IsLegacyCheckpointCurrentAsync(tenantId, sourceCheckpoints[tenantId], cancellationToken)
                .ConfigureAwait(false))
            {
                return new("AuthoritativePending", platformCount, tenantCount);
            }
        }

        return new(platformCount == 0 && tenantCount == 0 ? "NoOp" : "ProjectionConfirmed", platformCount, tenantCount);
    }

    private ProviderCatalogAdministrationRequest Request(AgentAdministrationContext actor, string tenantId)
        => new(identities.NewMessageId(), identities.NewCorrelationId(), tenantId, actor.ActorUserId,
            IsProviderAdmin: false, IsPlatformOperator: true);

    private Task<ProviderCatalogReadModel?> ReadPlatformAsync(CancellationToken ct)
        => ReadAsync<ProviderCatalogReadModel>(ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), ct);

    private async Task<bool> IsTargetCurrentAsync(string tenantId, string domain, string aggregateId,
        long projectedSequence, CancellationToken ct)
    {
        if (_gateway is null)
        {
            return false;
        }

        try
        {
            StreamReadPage page = await _gateway.ReadStreamAsync(
                new StreamReadRequest(tenantId, domain, aggregateId, PageSize: 1), ct).ConfigureAwait(false);
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

    private async Task<string> WaitForCommandAsync(ProviderCatalogAdministrationRequest request,
        AgentAdministrationOutcome outcome, CancellationToken ct)
    {
        if (_gateway is null || outcome.Receipt is not { } receipt
            || !string.Equals(receipt.MessageId, request.MessageId, StringComparison.Ordinal)
            || !string.Equals(receipt.CorrelationId, request.CorrelationId, StringComparison.Ordinal))
        {
            return "UnableToVerify";
        }

        DateTimeOffset deadline = _clock.GetUtcNow().AddSeconds(8);
        do
        {
            CommandStatusQueryResponse? status;
            try
            {
                status = await _gateway.GetCommandStatusAsync(request.MessageId, ct).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return "UnableToVerify";
            }

            if (status is not null)
            {
                if (!string.Equals(status.MessageId, request.MessageId, StringComparison.Ordinal)
                    || !string.Equals(status.TenantId, request.TenantId, StringComparison.Ordinal)
                    || !string.Equals(status.CorrelationId, request.CorrelationId, StringComparison.Ordinal))
                {
                    return "UnableToVerify";
                }

                if (status.IsRejected)
                {
                    return "Rejected";
                }

                if (status.StatusCode == (int)CommandStatus.Completed
                    && string.Equals(status.Status, nameof(CommandStatus.Completed), StringComparison.Ordinal))
                {
                    return status.EventCount switch
                    {
                        > 0 => "Completed",
                        0 => "TargetConflict",
                        _ => "UnableToVerify",
                    };
                }

                if (status.StatusCode == (int)CommandStatus.PublishFailed)
                {
                    return "AuthoritativePending";
                }

                if (status.StatusCode == (int)CommandStatus.TimedOut
                    || status.StatusCode == (int)CommandStatus.Rejected && status.Retryable is not true)
                {
                    return "DispatchFailed";
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), _clock, ct).ConfigureAwait(false);
        }
        while (_clock.GetUtcNow() < deadline);
        return "AuthoritativePending";
    }

    private async Task<bool> IsLegacyProjectionCurrentAsync(string tenantId, ProviderCatalogReadModel projection, CancellationToken ct)
    {
        long projected = LegacyCheckpoint(tenantId, projection);
        return (projected > 0 || projection.Entries.Count == 0)
            && await IsLegacyCheckpointCurrentAsync(tenantId, projected, ct).ConfigureAwait(false);
    }

    private static long LegacyCheckpoint(string tenantId, ProviderCatalogReadModel projection)
    {
        long projected = projection.StreamSequences.GetValueOrDefault(tenantId);
        return projected == 0 ? projection.LastSequenceNumber : projected;
    }

    private async Task<bool> IsLegacyCheckpointCurrentAsync(string tenantId, long expectedSequence, CancellationToken ct)
    {
        if (_gateway is null)
        {
            return false;
        }

        try
        {
            StreamReadPage page = await _gateway.ReadStreamAsync(
                new StreamReadRequest(tenantId, ProviderCatalogAggregate.Domain, tenantId, PageSize: 1), ct).ConfigureAwait(false);
            if (!string.Equals(page.Tenant, tenantId, StringComparison.Ordinal)
                || !string.Equals(page.Domain, ProviderCatalogAggregate.Domain, StringComparison.Ordinal)
                || !string.Equals(page.AggregateId, tenantId, StringComparison.Ordinal)
                || page.Metadata.LatestSequence < 0)
            {
                return false;
            }

            return expectedSequence == page.Metadata.LatestSequence;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }
    }

    private Task<TenantProviderEnablementReadModel?> ReadTenantAsync(string tenantId, CancellationToken ct)
        => ReadAsync<TenantProviderEnablementReadModel>(TenantProviderEnablementReadModelAddresses.Detail(tenantId), ct);

    private async Task<T?> ReadAsync<T>(string key, CancellationToken ct)
        where T : class
        => (await readModels.GetAsync<T>(options.Value.StateStoreName, key, ct).ConfigureAwait(false)).Value;

    private async Task<bool> WaitForPlatformAsync(ProviderCatalogEntryView source, string provenance,
        bool enabled, string messageId, CancellationToken ct)
    {
        DateTimeOffset deadline = _clock.GetUtcNow().AddSeconds(8);
        do
        {
            ProviderCatalogReadModel? model = await ReadPlatformAsync(ct).ConfigureAwait(false);
            ProviderCatalogEntryView? target = Find(model, source);
            string entryId = ProviderCatalogIdentity.EntryId(source.ProviderId, source.ModelId);
            if (target is not null && PlatformMatches(target, source, provenance, enabled)
                && model!.StreamCommandMessageIds.GetValueOrDefault(entryId)?.Contains(messageId, StringComparer.Ordinal) == true)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), _clock, ct).ConfigureAwait(false);
        }
        while (_clock.GetUtcNow() < deadline);
        return false;
    }

    private async Task<bool> WaitForTenantAsync(string tenantId, ProviderCatalogEntryView source,
        string provenance, string messageId, CancellationToken ct)
    {
        DateTimeOffset deadline = _clock.GetUtcNow().AddSeconds(8);
        do
        {
            TenantProviderEnablementReadModel? model = await ReadTenantAsync(tenantId, ct).ConfigureAwait(false);
            TenantProviderEntryState? target = Find(model, source);
            if (target is not null && model!.ProjectedCommandMessageIds.Contains(messageId, StringComparer.Ordinal))
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

    private static bool PlatformMatches(ProviderCatalogEntryView target, ProviderCatalogEntryView source,
        string provenance, bool enabled)
        => PlatformMetadataMatches(target, source, provenance)
            && target.Status == (enabled ? ProviderModelStatus.Enabled : ProviderModelStatus.Disabled);

    private static bool PlatformMetadataMatches(ProviderCatalogEntryView target, ProviderCatalogEntryView source,
        string provenance)
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
            LifecycleRevision = 1,
        });
}
