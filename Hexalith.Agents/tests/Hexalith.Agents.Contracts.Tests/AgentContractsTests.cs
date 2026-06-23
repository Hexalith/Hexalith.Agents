using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Agent public surface (AC1–AC4; AD-2, AD-14). Proves the events/rejections implement the
/// expected EventStore marker interfaces, the sensitive Agent Instructions text is confined to the create/update
/// success events (never a rejection/denial surface or the status view), and the lifecycle/blocker enums serialize
/// safely by name. The assembly-wide secret-token and provider-SDK-type guards live in
/// <see cref="ContractsSecretNonDisclosureTests"/> and <see cref="ContractsBoundaryTests"/>.
/// </summary>
public sealed class AgentContractsTests
{
    private static readonly Assembly _contracts = typeof(AgentsContractsAssemblyMarker).Assembly;

    [Fact]
    public void All_agent_events_implement_IEventPayload()
    {
        Type[] eventTypes = AgentTypesIn("Hexalith.Agents.Contracts.Agent.Events");

        eventTypes.ShouldNotBeEmpty();
        foreach (Type type in eventTypes)
        {
            typeof(IEventPayload).IsAssignableFrom(type)
                .ShouldBeTrue($"{type.Name} must implement IEventPayload.");
        }
    }

    [Fact]
    public void All_agent_rejections_implement_IRejectionEvent()
    {
        Type[] rejectionTypes = AgentTypesIn("Hexalith.Agents.Contracts.Agent.Events.Rejections");

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
    public void No_agent_rejection_exposes_instructions(/* AD-14: instructions never appear on a rejection surface */)
    {
        Type[] rejectionTypes = AgentTypesIn("Hexalith.Agents.Contracts.Agent.Events.Rejections");

        foreach (Type type in rejectionTypes)
        {
            type.GetProperties()
                .ShouldNotContain(
                    p => p.Name.Contains("Instruction", StringComparison.OrdinalIgnoreCase),
                    $"{type.Name} must not expose any instruction-bearing member (AD-14).");
        }
    }

    [Fact]
    public void Status_view_exposes_instruction_presence_not_the_text()
    {
        PropertyInfo[] properties = typeof(AgentStatusView).GetProperties();

        // Safe instruction signals are present...
        properties.ShouldContain(p => p.Name == "HasInstructions" && p.PropertyType == typeof(bool));
        properties.ShouldContain(p => p.Name == "InstructionsValid" && p.PropertyType == typeof(bool));
        properties.ShouldContain(p => p.Name == "InstructionsVersion" && p.PropertyType == typeof(int));
        // ...but the raw instructions text is not.
        properties.ShouldNotContain(p => p.Name == "Instructions");
    }

    [Fact]
    public void Only_success_events_carry_the_instructions_text()
    {
        // The sanctioned durable home for the sensitive instructions text is the create/update success events.
        typeof(AgentCreated).GetProperties()
            .ShouldContain(p => p.Name == "Instructions" && p.PropertyType == typeof(string));
        typeof(AgentConfigurationUpdated).GetProperties()
            .ShouldContain(p => p.Name == "Instructions" && p.PropertyType == typeof(string));
    }

    [Fact]
    public void Activation_blocked_rejection_does_not_serialize_instructions_text()
    {
        const string instructions = "You are hexa, a secret-bearing prompt that must never leak.";
        var rejection = new AgentActivationBlockedRejection("hexa", [AgentActivationBlocker.MissingInstructions]);

        string json = JsonSerializer.Serialize(rejection);

        json.ShouldNotContain(instructions);
        json.ShouldContain("MissingInstructions"); // blockers serialize by name
    }

    [Fact]
    public void Lifecycle_status_serializes_by_name()
    {
        string json = JsonSerializer.Serialize(AgentLifecycleStatus.Disabled);

        json.ShouldBe("\"Disabled\"");
        JsonSerializer.Deserialize<AgentLifecycleStatus>(json).ShouldBe(AgentLifecycleStatus.Disabled);
    }

    [Fact]
    public void Activation_blocker_serializes_by_name()
    {
        string json = JsonSerializer.Serialize(AgentActivationBlocker.InvalidInstructions);

        json.ShouldBe("\"InvalidInstructions\"");
        JsonSerializer.Deserialize<AgentActivationBlocker>(json).ShouldBe(AgentActivationBlocker.InvalidInstructions);
    }

    [Fact]
    public void Created_event_round_trips_through_system_text_json()
    {
        var created = new AgentCreated(
            "hexa",
            "acme",
            "Hexa Assistant",
            "Tenant governed assistant",
            "You are hexa, a helpful and concise enterprise assistant.",
            ConfigurationVersion: 1,
            InstructionsVersion: 1);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(created);

        JsonSerializer.Deserialize<AgentCreated>(bytes).ShouldBe(created);
    }

    private static Type[] AgentTypesIn(string @namespace)
        => _contracts.GetExportedTypes()
            .Where(type => type.Namespace == @namespace && !type.IsAbstract && !type.IsInterface)
            .ToArray();
}
