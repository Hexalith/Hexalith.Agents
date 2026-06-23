using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the ProviderCatalog public surface (AC1, AC3; AD-9, AD-14). Proves the events/rejections
/// implement the expected EventStore marker interfaces, the read surface exposes only a safe configuration
/// reference/state (no value-bearing secret field), and the enum/value types serialize safely by name. The
/// assembly-wide secret-token and provider-SDK-type guards live in <see cref="ContractsSecretNonDisclosureTests"/>.
/// </summary>
public sealed class ProviderCatalogContractsTests
{
    private static readonly Assembly _contracts = typeof(AgentsContractsAssemblyMarker).Assembly;

    [Fact]
    public void All_provider_catalog_events_implement_IEventPayload()
    {
        Type[] eventTypes = ProviderCatalogTypesIn("Hexalith.Agents.Contracts.ProviderCatalog.Events");

        eventTypes.ShouldNotBeEmpty();
        foreach (Type type in eventTypes)
        {
            typeof(IEventPayload).IsAssignableFrom(type)
                .ShouldBeTrue($"{type.Name} must implement IEventPayload.");
        }
    }

    [Fact]
    public void All_provider_catalog_rejections_implement_IRejectionEvent()
    {
        Type[] rejectionTypes = ProviderCatalogTypesIn("Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections");

        rejectionTypes.ShouldNotBeEmpty();
        foreach (Type type in rejectionTypes)
        {
            typeof(IRejectionEvent).IsAssignableFrom(type)
                .ShouldBeTrue($"{type.Name} must implement IRejectionEvent.");
            typeof(IEventPayload).IsAssignableFrom(type)
                .ShouldBeTrue($"{type.Name} must implement IEventPayload (via IRejectionEvent).");
        }
    }

    [Fact]
    public void Provider_catalog_view_exposes_safe_configuration_reference_and_state_only()
    {
        PropertyInfo[] properties = typeof(ProviderCatalogEntryView).GetProperties();

        properties.ShouldContain(p => p.Name == "ConfigurationReferenceId" && p.PropertyType == typeof(string));
        properties.ShouldContain(p => p.Name == "ConfigurationState" && p.PropertyType == typeof(ProviderConfigurationState));
        // No value-bearing field that could carry a raw secret.
        properties.ShouldNotContain(p => p.Name.Contains("Value", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Provider_model_status_serializes_by_name()
    {
        string json = JsonSerializer.Serialize(ProviderModelStatus.Disabled);

        json.ShouldBe("\"Disabled\"");
        JsonSerializer.Deserialize<ProviderModelStatus>(json).ShouldBe(ProviderModelStatus.Disabled);
    }

    [Fact]
    public void Capability_flags_round_trip()
    {
        ProviderModelCapabilityFlags flags = ProviderModelCapabilityFlags.Streaming | ProviderModelCapabilityFlags.Vision;

        string json = JsonSerializer.Serialize(flags);

        JsonSerializer.Deserialize<ProviderModelCapabilityFlags>(json).ShouldBe(flags);
    }

    [Fact]
    public void Created_event_round_trips_through_system_text_json()
    {
        var created = new ProviderModelEntryCreated(
            "acme",
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o",
            Enabled: true,
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 128_000,
            MaxOutputTokenLimit: 16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming | ProviderModelCapabilityFlags.ToolCalling,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(created);

        JsonSerializer.Deserialize<ProviderModelEntryCreated>(bytes).ShouldBe(created);
    }

    private static Type[] ProviderCatalogTypesIn(string @namespace)
        => _contracts.GetExportedTypes()
            .Where(type => type.Namespace == @namespace && !type.IsAbstract && !type.IsInterface)
            .ToArray();
}
