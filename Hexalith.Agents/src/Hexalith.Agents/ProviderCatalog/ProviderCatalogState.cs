using System;
using System.Collections.Generic;

using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

namespace Hexalith.Agents.ProviderCatalog;

/// <summary>
/// Replay state for the tenant-scoped <c>ProviderCatalog</c> aggregate (AD-2 aggregate boundary). Holds the
/// catalog's provider/model entries keyed by provider+model identity. State changes only through the
/// <c>Apply</c> methods (AD-3); no-op <c>Apply</c> methods for the rejection events keep replay total so a
/// persisted rejection never breaks rehydration.
/// </summary>
public sealed class ProviderCatalogState
{
    /// <summary>Gets or sets the provider-catalog aggregate identifier (the tenant's catalog id).</summary>
    public string CatalogId { get; set; } = string.Empty;

    /// <summary>Gets or sets the provider/model entries keyed by <see cref="EntryKey"/>.</summary>
    public Dictionary<string, ProviderModelEntryState> Entries { get; set; } = [];

    /// <summary>
    /// Builds the composite dictionary key for a provider/model entry. Joins the identifiers with the ASCII
    /// unit-separator (<c>\u001f</c>) — not a character that appears in provider/model identifiers — so distinct
    /// (provider, model) pairs never collide (for example ("ab","c") and ("a","bc") map to different keys).
    /// </summary>
    /// <param name="providerId">Stable provider identifier.</param>
    /// <param name="modelId">Stable model identifier.</param>
    /// <returns>The composite entry key.</returns>
    public static string EntryKey(string providerId, string modelId) => $"{providerId}\u001f{modelId}";

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

            // Story 1.5: the replay-derived capability version starts at 1 on create.
            CapabilityVersion = 1,
        };
    }

    /// <summary>Applies a safe-metadata update to an existing entry.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProviderModelEntryMetadataUpdated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!Entries.TryGetValue(EntryKey(e.ProviderId, e.ModelId), out ProviderModelEntryState? entry))
        {
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

        // Story 1.5: a genuine capability-metadata change bumps the version. Story 1.2 emits this event only on a
        // real change (exact-duplicate updates are NoOp), so the counter increments only on genuine changes.
        entry.CapabilityVersion += 1;
    }

    /// <summary>Applies an entry enablement.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ProviderModelEntryEnabled e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (Entries.TryGetValue(EntryKey(e.ProviderId, e.ModelId), out ProviderModelEntryState? entry))
        {
            entry.IsEnabled = true;
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

    private void MarkReplayOnlyEventHandled() => _ = CatalogId;
}
