using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.ProviderCatalog;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.ProviderCatalogTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Story 5.3 — CapabilityVersion is monotonic and non-reusable; a reused or decreased version is rejected and
/// the persisted catalog state is unchanged.
/// </summary>
public sealed class ProviderCatalogVersionRegressionTests
{
    [Fact]
    public void An_expected_capability_version_below_current_is_rejected_and_does_not_mutate()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        int versionBefore = state.Entries[ProviderCatalogState.EntryKey("openai", "gpt-4o")].CapabilityVersion;
        var command = ValidUpdate(displayLabel: "regressed", expectedCapabilityVersion: 0);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        ProviderModelCapabilityVersionRegressedRejection rejection =
            result.Events[0].ShouldBeOfType<ProviderModelCapabilityVersionRegressedRejection>();
        rejection.AttemptedCapabilityVersion.ShouldBe(0);
        rejection.CurrentCapabilityVersion.ShouldBe(versionBefore);
        state.Entries[ProviderCatalogState.EntryKey("openai", "gpt-4o")].CapabilityVersion.ShouldBe(versionBefore);
        state.Entries[ProviderCatalogState.EntryKey("openai", "gpt-4o")].DisplayLabel.ShouldBe("OpenAI GPT-4o");
    }

    [Fact]
    public void An_expected_capability_version_ahead_of_current_is_a_typed_stale_revision()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        var command = ValidUpdate(displayLabel: "stale", expectedCapabilityVersion: 3);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderModelEntryStaleRevisionRejection>();
        state.Entries[ProviderCatalogState.EntryKey("openai", "gpt-4o")].DisplayLabel.ShouldBe("OpenAI GPT-4o");
    }

    [Fact]
    public void Applying_a_metadata_event_with_a_reused_capability_version_does_not_overwrite_state()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        ProviderModelEntryState before = state.Entries[ProviderCatalogState.EntryKey("openai", "gpt-4o")];

        state.Apply(new ProviderModelEntryMetadataUpdated(
            CatalogId,
            "openai",
            "gpt-4o",
            "Should not apply",
            SupportsTextGeneration: true,
            200_000,
            16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o",
            ValidPricing(2),
            CapabilityVersion: 1));

        ProviderModelEntryState after = state.Entries[ProviderCatalogState.EntryKey("openai", "gpt-4o")];
        after.DisplayLabel.ShouldBe(before.DisplayLabel);
        after.CapabilityVersion.ShouldBe(1);
    }

    [Fact]
    public void Enable_and_disable_leave_capability_and_pricing_versions_unchanged()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        string key = ProviderCatalogState.EntryKey("openai", "gpt-4o");
        int capability = state.Entries[key].CapabilityVersion;
        int pricing = state.Entries[key].Pricing!.PricingVersion;

        DomainResult disable = ProviderCatalogAggregate.Handle(
            new Contracts.ProviderCatalog.Commands.DisableProviderModelEntry("openai", "gpt-4o"),
            state,
            Envelope(new Contracts.ProviderCatalog.Commands.DisableProviderModelEntry("openai", "gpt-4o")));
        ApplyAll(state, disable);
        DomainResult enable = ProviderCatalogAggregate.Handle(
            new Contracts.ProviderCatalog.Commands.EnableProviderModelEntry("openai", "gpt-4o"),
            state,
            Envelope(new Contracts.ProviderCatalog.Commands.EnableProviderModelEntry("openai", "gpt-4o")));
        ApplyAll(state, enable);

        state.Entries[key].CapabilityVersion.ShouldBe(capability);
        state.Entries[key].Pricing!.PricingVersion.ShouldBe(pricing);
    }
}
