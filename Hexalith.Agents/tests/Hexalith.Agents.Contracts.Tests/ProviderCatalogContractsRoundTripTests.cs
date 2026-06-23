using System.Text.Json;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Serialization gap-fill for the ProviderCatalog public surface (AC1, AC4). The dev-story suite round-trips
/// only <see cref="ProviderModelEntryCreated"/> and the <see cref="ProviderModelStatus"/> enum; durable event
/// sourcing replays <em>every</em> event and rejection type, so each must survive System.Text.Json without an
/// active state leaking through an absent value. These tests round-trip the remaining success events, a
/// representative enum-bearing rejection, and assert <see cref="ProviderConfigurationState"/> serializes by name.
/// </summary>
public sealed class ProviderCatalogContractsRoundTripTests
{
    [Fact]
    public void Metadata_updated_event_round_trips_through_system_text_json()
    {
        var updated = new ProviderModelEntryMetadataUpdated(
            "acme",
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o (v2)",
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 200_000,
            MaxOutputTokenLimit: 32_000,
            new ProviderModelTimeoutPolicy(45_000, 2),
            ProviderModelCapabilityFlags.Streaming | ProviderModelCapabilityFlags.Vision,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(updated);

        JsonSerializer.Deserialize<ProviderModelEntryMetadataUpdated>(bytes).ShouldBe(updated);
    }

    [Fact]
    public void Enabled_event_round_trips_through_system_text_json()
    {
        var enabled = new ProviderModelEntryEnabled("acme", "openai", "gpt-4o");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(enabled);

        JsonSerializer.Deserialize<ProviderModelEntryEnabled>(bytes).ShouldBe(enabled);
    }

    [Fact]
    public void Disabled_event_round_trips_through_system_text_json()
    {
        var disabled = new ProviderModelEntryDisabled("acme", "openai", "gpt-4o");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(disabled);

        JsonSerializer.Deserialize<ProviderModelEntryDisabled>(bytes).ShouldBe(disabled);
    }

    [Fact]
    public void Lifecycle_already_set_rejection_round_trips_with_enum_status()
    {
        var rejection = new ProviderModelEntryLifecycleStateAlreadySetRejection(
            "acme",
            "openai",
            "gpt-4o",
            ProviderModelStatus.Enabled,
            ProviderModelStatus.Enabled,
            "EnableProviderModelEntry");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(rejection);

        JsonSerializer.Deserialize<ProviderModelEntryLifecycleStateAlreadySetRejection>(bytes).ShouldBe(rejection);
    }

    [Fact]
    public void Configuration_state_serializes_by_name()
    {
        string json = JsonSerializer.Serialize(ProviderConfigurationState.Configured);

        json.ShouldBe("\"Configured\"");
        JsonSerializer.Deserialize<ProviderConfigurationState>(json).ShouldBe(ProviderConfigurationState.Configured);
    }

    [Fact]
    public void Unknown_configuration_state_is_the_default_so_absent_values_are_never_configured()
    {
        // AD-14 fail-safe: an absent/zero value must never deserialize to Configured.
        default(ProviderConfigurationState).ShouldBe(ProviderConfigurationState.Unknown);
        JsonSerializer.Deserialize<ProviderConfigurationState>("\"Unknown\"").ShouldBe(ProviderConfigurationState.Unknown);
    }
}
