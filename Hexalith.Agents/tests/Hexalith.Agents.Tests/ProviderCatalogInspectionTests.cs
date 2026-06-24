using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.ProviderCatalog;

using Shouldly;

using static Hexalith.Agents.Tests.ProviderCatalogTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Tests for the pure inspection read path (<see cref="ProviderCatalogInspection"/>): authorized inspection of
/// current and historical state without secrets (AC2), and fail-closed structured results for unauthorized
/// inspection (AC3).
/// </summary>
public sealed class ProviderCatalogInspectionTests
{
    [Fact]
    public void GetEntry_authorized_existing_returns_success_view()
    {
        ProviderCatalogState state = StateWith(ValidCreate());

        ProviderCatalogInspectionResult result = ProviderCatalogInspection.GetEntry(state, isProviderAdmin: true, "openai", "gpt-4o");

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        ProviderCatalogEntryView view = result.Entries.ShouldHaveSingleItem();
        view.ProviderId.ShouldBe("openai");
        view.ModelId.ShouldBe("gpt-4o");
        view.Status.ShouldBe(ProviderModelStatus.Enabled);
        view.IsSelectableForNewActiveUse.ShouldBeTrue();
        view.ConfigurationState.ShouldBe(ProviderConfigurationState.Configured);
        view.ConfigurationReferenceId.ShouldBe("cfg-openai-gpt4o"); // safe reference only — no secret value
    }

    [Fact]
    public void GetEntry_maps_the_capability_version_from_state()
    {
        // Story 1.5: the view surfaces the replay-derived capability version (1 at create, +1 per metadata update).
        ProviderCatalogState state = StateWith(ValidCreate());

        ProviderCatalogInspection.GetEntry(state, isProviderAdmin: true, "openai", "gpt-4o")
            .Entries.ShouldHaveSingleItem().CapabilityVersion.ShouldBe(1);

        state.Apply(new ProviderModelEntryMetadataUpdated(
            CatalogId,
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o (v2)",
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 200_000,
            MaxOutputTokenLimit: 16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o"));

        ProviderCatalogInspection.GetEntry(state, isProviderAdmin: true, "openai", "gpt-4o")
            .Entries.ShouldHaveSingleItem().CapabilityVersion.ShouldBe(2);
    }

    [Fact]
    public void GetEntry_unauthorized_returns_not_authorized_with_no_data()
    {
        ProviderCatalogState state = StateWith(ValidCreate());

        ProviderCatalogInspectionResult result = ProviderCatalogInspection.GetEntry(state, isProviderAdmin: false, "openai", "gpt-4o");

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        result.Entries.ShouldBeEmpty(); // AC3: no fingerprinting of which entries exist
    }

    [Fact]
    public void GetEntry_missing_returns_not_found()
    {
        ProviderCatalogInspectionResult result = ProviderCatalogInspection.GetEntry(StateWith(ValidCreate()), isProviderAdmin: true, "openai", "missing");

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);
        result.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void GetEntry_disabled_entry_is_visible_but_not_selectable()
    {
        ProviderCatalogState state = StateWith(ValidCreate(enabled: false));

        ProviderCatalogInspectionResult result = ProviderCatalogInspection.GetEntry(state, isProviderAdmin: true, "openai", "gpt-4o");

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        ProviderCatalogEntryView view = result.Entries.ShouldHaveSingleItem();
        view.Status.ShouldBe(ProviderModelStatus.Disabled);
        view.IsSelectableForNewActiveUse.ShouldBeFalse(); // AC2
    }

    [Fact]
    public void ListEntries_excludes_disabled_when_not_requested()
    {
        ProviderCatalogState state = StateWith(
            ValidCreate(enabled: true, providerId: "openai", modelId: "gpt-4o"),
            ValidCreate(enabled: false, providerId: "anthropic", modelId: "claude"));

        ProviderCatalogInspectionResult result = ProviderCatalogInspection.ListEntries(state, isProviderAdmin: true, includeDisabled: false);

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        ProviderCatalogEntryView view = result.Entries.ShouldHaveSingleItem();
        view.ProviderId.ShouldBe("openai");
    }

    [Fact]
    public void ListEntries_includes_disabled_when_requested_and_flags_not_selectable()
    {
        ProviderCatalogState state = StateWith(
            ValidCreate(enabled: true, providerId: "openai", modelId: "gpt-4o"),
            ValidCreate(enabled: false, providerId: "anthropic", modelId: "claude"));

        ProviderCatalogInspectionResult result = ProviderCatalogInspection.ListEntries(state, isProviderAdmin: true, includeDisabled: true);

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        result.Entries.Count.ShouldBe(2);
        // AC2: disabled entry visible for history, but flagged not selectable for new active use.
        result.Entries.ShouldContain(v => v.ProviderId == "anthropic" && !v.IsSelectableForNewActiveUse);
        result.Entries.ShouldContain(v => v.ProviderId == "openai" && v.IsSelectableForNewActiveUse);
    }

    [Fact]
    public void ListEntries_unauthorized_returns_not_authorized_with_no_data()
    {
        ProviderCatalogState state = StateWith(ValidCreate());

        ProviderCatalogInspectionResult result = ProviderCatalogInspection.ListEntries(state, isProviderAdmin: false, includeDisabled: true);

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        result.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void ListEntries_authorized_empty_catalog_returns_empty_success()
    {
        ProviderCatalogInspectionResult result = ProviderCatalogInspection.ListEntries(state: null, isProviderAdmin: true, includeDisabled: true);

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        result.Entries.ShouldBeEmpty();
    }
}
