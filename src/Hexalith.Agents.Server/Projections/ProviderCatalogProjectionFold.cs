using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.Contracts.Serialization;
using Hexalith.Agents.ProviderCatalog;

using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// Deterministic fold from a provider-catalog event slice onto the persisted catalog read model (Story 5.3).
/// The fold is pure: it reads no clock and no dependency, and it skips events at or below the stored checkpoint.
/// </summary>
public static class ProviderCatalogProjectionFold
{
    /// <summary>Reason reported when a delivery skips aggregate sequence numbers.</summary>
    public const string DeliverySequenceGapReason = "delivery-sequence-gap";

    /// <summary>Reason reported when a delivery carries an event this domain cannot resolve.</summary>
    public const string UnresolvedEventReason = "unresolved-event-type";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        // The fallback factory must precede the string converter: options.Converters wins over a type-level
        // [JsonConverter], so without it every contract enum declaring the Unknown = 0 degradation would be
        // silently downgraded back to the throwing converter here.
        Converters = { new UnknownFallbackEnumConverterFactory(), new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Returns the reason a delivery must be retried rather than folded, or <see langword="null"/> when the slice is
    /// safe to apply.
    /// </summary>
    /// <param name="events">The delivered event slice.</param>
    /// <param name="checkpoint">The highest sequence number already folded (0 when none).</param>
    /// <returns>The retry reason, or <see langword="null"/>.</returns>
    public static string? GetDeliveryFailureReason(IReadOnlyCollection<ProjectionEventDto> events, long checkpoint)
    {
        ArgumentNullException.ThrowIfNull(events);

        ProjectionEventDto[] pending = [.. events.Where(item => item.SequenceNumber > checkpoint).OrderBy(item => item.SequenceNumber)];
        if (pending.Length == 0)
        {
            return null;
        }

        if (pending[0].SequenceNumber != checkpoint + 1)
        {
            return DeliverySequenceGapReason;
        }

        for (int index = 1; index < pending.Length; index++)
        {
            if (pending[index].SequenceNumber != pending[index - 1].SequenceNumber + 1)
            {
                return DeliverySequenceGapReason;
            }
        }

        return Array.Exists(pending, item => ProviderCatalogEventTypeResolver.Resolve(item.EventTypeName) is null)
            ? UnresolvedEventReason
            : null;
    }

    /// <summary>Folds the delivered slice onto the current read model, returning the next value.</summary>
    /// <param name="request">The projection request carrying the tenant, catalog id, and event slice.</param>
    /// <param name="current">The currently persisted read model, or <see langword="null"/> when absent.</param>
    /// <returns>The next read model.</returns>
    public static ProviderCatalogReadModel Fold(ProjectionRequest request, ProviderCatalogReadModel? current)
    {
        ArgumentNullException.ThrowIfNull(request);

        long checkpoint = current?.StreamSequences.GetValueOrDefault(request.AggregateId) ?? 0;
        ProviderCatalogState state = ToState(current, request.TenantId);
        DateTimeOffset? projectedAt = current?.ProjectedAt;
        long lastSequence = checkpoint;

        foreach (ProjectionEventDto @event in request.Events
                     .Where(item => item.SequenceNumber > checkpoint)
                     .OrderBy(item => item.SequenceNumber))
        {
            if (ProviderCatalogEventTypeResolver.Resolve(@event.EventTypeName) is not { } eventType)
            {
                break;
            }

            if (Deserialize(@event.Payload, eventType) is not { } payload)
            {
                // Empty or corrupt payloads must not advance the checkpoint; the delivery can retry this event.
                break;
            }

            Apply(state, payload);
            lastSequence = Math.Max(lastSequence, @event.SequenceNumber);
            DateTimeOffset timestamp = @event.Timestamp.ToUniversalTime();
            projectedAt = projectedAt is { } known && known >= timestamp ? known : timestamp;
        }

        Dictionary<string, long> streamSequences = current?.StreamSequences is { } prior
            ? new(prior, StringComparer.Ordinal)
            : new(StringComparer.Ordinal);
        streamSequences[request.AggregateId] = lastSequence;
        return ToReadModel(state, request.TenantId, streamSequences, projectedAt);
    }

    /// <summary>Rebuilds the folded catalog state from a persisted read model.</summary>
    /// <param name="model">The persisted read model, or <see langword="null"/>.</param>
    /// <param name="tenantId">The tenant scope to seed an empty state with.</param>
    /// <returns>The rehydrated state.</returns>
    public static ProviderCatalogState ToState(ProviderCatalogReadModel? model, string tenantId)
    {
        var state = new ProviderCatalogState { CatalogId = model?.CatalogId ?? tenantId };
        if (model is null)
        {
            return state;
        }

        foreach (Contracts.ProviderCatalog.ProviderCatalogEntryView view in model.Entries)
        {
            state.Entries[ProviderCatalogState.EntryKey(view.ProviderId, view.ModelId)] = new ProviderModelEntryState
            {
                ProviderId = view.ProviderId,
                ModelId = view.ModelId,
                DisplayLabel = view.DisplayLabel,
                IsEnabled = view.Status == Contracts.ProviderCatalog.ProviderModelStatus.Enabled,
                SupportsTextGeneration = view.SupportsTextGeneration,
                ContextWindowTokenLimit = view.ContextWindowTokenLimit,
                MaxOutputTokenLimit = view.MaxOutputTokenLimit,
                TimeoutPolicy = view.TimeoutPolicy,
                SafeCapabilityFlags = view.SafeCapabilityFlags,
                ConfigurationState = view.ConfigurationState,
                ConfigurationReferenceId = view.ConfigurationReferenceId,
                CapabilityVersion = view.CapabilityVersion,
                Pricing = view.Pricing,
                DataHandling = view.DataHandling,
                DataHandlingHistory = view.DataHandlingHistory is null ? [] : [.. view.DataHandlingHistory],
                MigratedFrom = view.MigratedFrom,
            };
        }

        return state;
    }

    private static ProviderCatalogReadModel ToReadModel(
        ProviderCatalogState state,
        string tenantId,
        Dictionary<string, long> streamSequences,
        DateTimeOffset? projectedAt)
        => new()
        {
            CatalogId = tenantId,
            TenantId = tenantId,
            Entries = [.. state.Entries.Values
                .OrderBy(entry => entry.ProviderId, StringComparer.Ordinal)
                .ThenBy(entry => entry.ModelId, StringComparer.Ordinal)
                .Select(ProviderCatalogInspection.ToView)],
            StreamSequences = streamSequences,
            LastSequenceNumber = streamSequences.Values.Sum(),
            ProjectedAt = projectedAt,
            ProjectionVersion = streamSequences.Count == 0
                ? null
                : streamSequences.Values.Sum().ToString(CultureInfo.InvariantCulture),
        };

    private static object? Deserialize(byte[] payload, Type eventType)
    {
        try
        {
            return payload is null or { Length: 0 }
                ? null
                : JsonSerializer.Deserialize(payload, eventType, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void Apply(ProviderCatalogState state, object payload)
    {
        switch (payload)
        {
            case ProviderModelEntryCreated e: state.Apply(e); break;
            case ProviderModelEntryMetadataUpdated e: state.Apply(e); break;
            case ProviderModelEntryEnabled e: state.Apply(e); break;
            case ProviderModelEntryDisabled e: state.Apply(e); break;
            case ProviderCatalogAdministrationDeniedRejection e: state.Apply(e); break;
            case ProviderModelEntryAlreadyExistsRejection e: state.Apply(e); break;
            case ProviderModelEntryNotFoundRejection e: state.Apply(e); break;
            case ProviderModelEntryLifecycleStateAlreadySetRejection e: state.Apply(e); break;
            case InvalidProviderModelMetadataRejection e: state.Apply(e); break;
            case UnsafeProviderConfigurationInputRejection e: state.Apply(e); break;
            case InvalidProviderModelPricingRejection e: state.Apply(e); break;
            case InvalidProviderDataHandlingRejection e: state.Apply(e); break;
            case ProviderModelCapabilityVersionRegressedRejection e: state.Apply(e); break;
            case ProviderModelEntryStaleRevisionRejection e: state.Apply(e); break;
            default: break;
        }
    }
}
