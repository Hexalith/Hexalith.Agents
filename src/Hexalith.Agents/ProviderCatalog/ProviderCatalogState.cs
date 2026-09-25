using System;
using System.Collections.Generic;

using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

namespace Hexalith.Agents.ProviderCatalog;

/// <summary>
/// Replay state for a platform <c>ProviderCatalog</c> entry (AD-2 aggregate boundary). Holds the
/// provider/model entry keyed by provider+model identity. State changes only through the
/// <c>Apply</c> methods (AD-3); no-op <c>Apply</c> methods for the rejection events keep replay total so a
/// persisted rejection never breaks rehydration.
/// </summary>
public sealed class ProviderCatalogState
{
    /// <summary>Gets or sets the provider-catalog aggregate identifier.</summary>
    public string CatalogId { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider/model entries keyed by <see cref="EntryKey"/>.</summary>
    public Dictionary<string, ProviderModelEntryState> Entries { get; set; } = [];

    /// <summary>
    /// Builds a collision-free length-prefixed dictionary key for a provider/model pair.
    /// </summary>
    /// <param name="providerId">Stable provider identifier.</param>
    /// <param name="modelId">Stable model identifier.</param>
    /// <returns>The composite entry key.</returns>
    public static string EntryKey(string providerId, string modelId)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(modelId);
        return $"{providerId.Length}:{providerId}{modelId.Length}:{modelId}";
    }

    /// <summary>Applies a provider/model entry creation.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProviderModelEntryCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        CatalogId = e.CatalogId;
        Entries[EntryKey(e.ProviderId, e.ModelId)] = new ProviderModelEntryState
        {
            ProviderId = e.ProviderId,
            ModelId = e.ModelId,
            DisplayLabel = e.DisplayLabel,
            IsEnabled = e.Enabled,
            SupportsTextGeneration = e.SupportsTextGeneration,
            ContextWindowTokenLimit = e.ContextWindowTokenLimit,
            MaxOutputTokenLimit = e.MaxOutputTokenLimit,
            TimeoutPolicy = e.TimeoutPolicy,
            SafeCapabilityFlags = e.SafeCapabilityFlags,
            ConfigurationState = e.ConfigurationState,
            ConfigurationReferenceId = e.ConfigurationReferenceId,
            CapabilityVersion = e.CapabilityVersion > 0 ? e.CapabilityVersion : 1,
            LifecycleRevision = 1,
            Pricing = e.Pricing,
            DataHandling = e.DataHandling,
            DataHandlingHistory = e.DataHandling is null ? [] : [e.DataHandling],
            MigratedFrom = e.MigratedFrom,
        };
    }

    /// <summary>Applies a safe-metadata or pricing update to an existing entry.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProviderModelEntryMetadataUpdated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!Entries.TryGetValue(EntryKey(e.ProviderId, e.ModelId), out ProviderModelEntryState? entry))
        {
            return;
        }

        int nextVersion = e.CapabilityVersion > 0 ? e.CapabilityVersion : entry.CapabilityVersion + 1;
        if (nextVersion <= entry.CapabilityVersion)
        {
            // A reused or decreased CapabilityVersion must not overwrite current truth.
            return;
        }

        entry.DisplayLabel = e.DisplayLabel;
        entry.SupportsTextGeneration = e.SupportsTextGeneration;
        entry.ContextWindowTokenLimit = e.ContextWindowTokenLimit;
        entry.MaxOutputTokenLimit = e.MaxOutputTokenLimit;
        entry.TimeoutPolicy = e.TimeoutPolicy;
        entry.SafeCapabilityFlags = e.SafeCapabilityFlags;
        entry.ConfigurationState = e.ConfigurationState;
        entry.ConfigurationReferenceId = e.ConfigurationReferenceId;
        entry.Pricing = e.Pricing;
        if (e.DataHandling is { } terms && terms.DataHandlingVersion > (entry.DataHandling?.DataHandlingVersion ?? 0))
        {
            entry.DataHandlingHistory.Add(terms);
        }

        entry.DataHandling = e.DataHandling;
        entry.CapabilityVersion = nextVersion;
    }

    /// <summary>Applies an entry enablement.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProviderModelEntryEnabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (Entries.TryGetValue(EntryKey(e.ProviderId, e.ModelId), out ProviderModelEntryState? entry))
        {
            entry.IsEnabled = true;
            entry.LifecycleRevision++;
        }
    }

    /// <summary>Applies an entry disablement (history preserved; entry stays in the catalog).</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProviderModelEntryDisabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (Entries.TryGetValue(EntryKey(e.ProviderId, e.ModelId), out ProviderModelEntryState? entry))
        {
            entry.IsEnabled = false;
            entry.LifecycleRevision++;
        }
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProviderCatalogAdministrationDeniedRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProviderModelEntryAlreadyExistsRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProviderModelEntryNotFoundRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProviderModelEntryLifecycleStateAlreadySetRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(InvalidProviderModelMetadataRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(UnsafeProviderConfigurationInputRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(InvalidProviderModelPricingRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler for invalid data handling terms.</summary>
    /// <param name="e">The rejected event.</param>
    public void Apply(InvalidProviderDataHandlingRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProviderModelCapabilityVersionRegressedRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler — rejection events carry no state change.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProviderModelEntryStaleRevisionRejection e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    /// <summary>No-op replay handler for a stale lifecycle revision.</summary>
    /// <param name="e">The rejection event.</param>
    public void Apply(ProviderModelLifecycleRevisionRejected e)
    {
        ArgumentNullException.ThrowIfNull(e);
        MarkReplayOnlyEventHandled();
    }

    private void MarkReplayOnlyEventHandled() => _ = CatalogId;
}
