using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.ProviderCatalog;

using Shouldly;

using static Hexalith.Agents.Tests.ProviderCatalogTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Replay tests for <see cref="ProviderCatalogState"/> (AC4 determinism). Verifies the <c>Apply</c> methods
/// rebuild state deterministically, that disabling preserves history, and that the no-op rejection
/// <c>Apply</c> methods keep replay total so a persisted rejection never breaks rehydration.
/// </summary>
public sealed class ProviderCatalogStateReplayTests
{
    [Fact]
    public void Apply_create_disable_enable_tracks_enabled_and_preserves_entry()
    {
        var state = new ProviderCatalogState();

        state.Apply(CreatedEvent(ValidCreate(enabled: true)));
        string key = ProviderCatalogState.EntryKey("openai", "gpt-4o");
        state.CatalogId.ShouldBe(CatalogId);
        state.Entries[key].IsEnabled.ShouldBeTrue();

        state.Apply(new ProviderModelEntryDisabled(CatalogId, "openai", "gpt-4o"));
        state.Entries.ShouldContainKey(key); // AC2: history preserved, entry not removed
        state.Entries[key].IsEnabled.ShouldBeFalse();

        state.Apply(new ProviderModelEntryEnabled(CatalogId, "openai", "gpt-4o"));
        state.Entries[key].IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Apply_metadata_updated_rewrites_safe_fields()
    {
        ProviderCatalogState state = StateWith(ValidCreate());

        state.Apply(new ProviderModelEntryMetadataUpdated(
            CatalogId,
            "openai",
            "gpt-4o",
            "New Label",
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 200_000,
            MaxOutputTokenLimit: 32_000,
            new ProviderModelTimeoutPolicy(45_000, 1),
            ProviderModelCapabilityFlags.Vision,
            ProviderConfigurationState.Configured,
            "cfg-new"));

        ProviderModelEntryState entry = state.Entries[ProviderCatalogState.EntryKey("openai", "gpt-4o")];
        entry.DisplayLabel.ShouldBe("New Label");
        entry.ContextWindowTokenLimit.ShouldBe(200_000);
        entry.MaxOutputTokenLimit.ShouldBe(32_000);
        entry.SafeCapabilityFlags.ShouldBe(ProviderModelCapabilityFlags.Vision);
        entry.ConfigurationReferenceId.ShouldBe("cfg-new");
    }

    [Fact]
    public void Apply_rejection_events_are_replay_safe_noops()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        int entryCountBefore = state.Entries.Count;

        // Replaying a stream that contains persisted rejection events must not throw or mutate state.
        state.Apply(new ProviderCatalogAdministrationDeniedRejection(CatalogId, "intruder", "CreateProviderModelEntry"));
        state.Apply(new ProviderModelEntryAlreadyExistsRejection(CatalogId, "openai", "gpt-4o"));
        state.Apply(new ProviderModelEntryNotFoundRejection(CatalogId, "openai", "missing"));
        state.Apply(new ProviderModelEntryLifecycleStateAlreadySetRejection(
            CatalogId, "openai", "gpt-4o", ProviderModelStatus.Enabled, ProviderModelStatus.Enabled, "EnableProviderModelEntry"));
        state.Apply(new InvalidProviderModelMetadataRejection(CatalogId, "openai", "gpt-4o", "reason"));
        state.Apply(new UnsafeProviderConfigurationInputRejection(CatalogId, "openai", "gpt-4o", "reason"));

        state.Entries.Count.ShouldBe(entryCountBefore);
        state.Entries[ProviderCatalogState.EntryKey("openai", "gpt-4o")].IsEnabled.ShouldBeTrue();
    }
}
