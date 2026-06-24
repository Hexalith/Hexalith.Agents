using System;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Story 1.5 Provider/model selection surface (AC1, AC2, AC4; AD-9, AD-14). Durable event
/// sourcing replays every new event/rejection, so each must survive System.Text.Json; the
/// <see cref="ProviderSelectionValidationStatus"/> verdict and the new
/// <see cref="AgentActivationBlocker.MissingProviderSelection"/>/<see cref="AgentActivationBlocker.ProviderUnavailable"/>
/// blockers must serialize by name and fail safe to their <c>Unknown</c> sentinel; and the provider events/rejection
/// + extended status view must expose only safe id/version-shaped fields — never a secret value, credential, or
/// configuration reference.
/// </summary>
public sealed class AgentProviderSelectionContractsTests
{
    // ===== Marker interfaces =====

    [Fact]
    public void Provider_selected_event_implements_IEventPayload()
        => typeof(IEventPayload).IsAssignableFrom(typeof(AgentProviderModelSelected)).ShouldBeTrue();

    [Fact]
    public void Provider_selection_rejected_implements_IRejectionEvent()
    {
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentProviderModelSelectionRejected)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentProviderModelSelectionRejected)).ShouldBeTrue();
    }

    // ===== Round-trips =====

    [Fact]
    public void Provider_model_selected_event_round_trips_through_system_text_json()
    {
        var selected = new AgentProviderModelSelected("hexa", "openai", "gpt-4o", ProviderCapabilityVersion: 3, ConfigurationVersion: 4);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(selected);

        JsonSerializer.Deserialize<AgentProviderModelSelected>(bytes).ShouldBe(selected);
    }

    [Fact]
    public void Provider_model_selection_rejected_round_trips_with_the_enum_verdict()
    {
        var rejection = new AgentProviderModelSelectionRejected("hexa", ProviderSelectionValidationStatus.NotConfigured);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(rejection);

        AgentProviderModelSelectionRejected? roundTripped = JsonSerializer.Deserialize<AgentProviderModelSelectionRejected>(bytes);
        roundTripped.ShouldBe(rejection);
        roundTripped!.Status.ShouldBe(ProviderSelectionValidationStatus.NotConfigured);
    }

    // ===== Verdict + blocker enums serialize by name and fail safe =====

    [Fact]
    public void Provider_selection_validation_status_serializes_by_name()
    {
        JsonSerializer.Serialize(ProviderSelectionValidationStatus.Valid).ShouldBe("\"Valid\"");
        JsonSerializer.Deserialize<ProviderSelectionValidationStatus>("\"Unauthorized\"").ShouldBe(ProviderSelectionValidationStatus.Unauthorized);
    }

    [Fact]
    public void Provider_selection_validation_status_is_unknown_by_default_and_for_unrecognized_input()
    {
        // AC2 fail-safe: an absent/unrecognized verdict must never deserialize to Valid.
        default(ProviderSelectionValidationStatus).ShouldBe(ProviderSelectionValidationStatus.Unknown);
        JsonSerializer.Deserialize<ProviderSelectionValidationStatus>("\"Unknown\"").ShouldBe(ProviderSelectionValidationStatus.Unknown);
    }

    [Fact]
    public void New_provider_activation_blockers_serialize_by_name()
    {
        JsonSerializer.Serialize(AgentActivationBlocker.MissingProviderSelection).ShouldBe("\"MissingProviderSelection\"");
        JsonSerializer.Serialize(AgentActivationBlocker.ProviderUnavailable).ShouldBe("\"ProviderUnavailable\"");
        JsonSerializer.Deserialize<AgentActivationBlocker>("\"ProviderUnavailable\"").ShouldBe(AgentActivationBlocker.ProviderUnavailable);
    }

    [Fact]
    public void Existing_activation_blocker_ordinals_are_preserved_and_provider_gates_are_appended()
    {
        // Additive extensibility (AD-2): the new values are appended after MissingPartyIdentity; existing ordinals
        // must not shift.
        ((int)AgentActivationBlocker.Unknown).ShouldBe(0);
        ((int)AgentActivationBlocker.MissingDisplayName).ShouldBe(1);
        ((int)AgentActivationBlocker.MissingInstructions).ShouldBe(2);
        ((int)AgentActivationBlocker.InvalidInstructions).ShouldBe(3);
        ((int)AgentActivationBlocker.MissingPartyIdentity).ShouldBe(4);
        ((int)AgentActivationBlocker.MissingProviderSelection).ShouldBe(5);
        ((int)AgentActivationBlocker.ProviderUnavailable).ShouldBe(6);
    }

    // ===== Provider events/rejection + status view expose only safe id/version-shaped fields (AC1; AD-9, AD-14) =====

    [Fact]
    public void Provider_surface_exposes_no_secret_credential_or_configuration_reference_member()
    {
        Type[] providerTypes =
        [
            typeof(SelectAgentProviderModel),
            typeof(AgentProviderModelSelected),
            typeof(AgentProviderModelSelectionRejected),
        ];

        string[] forbiddenTokens =
        [
            "Secret", "ApiKey", "Credential", "Password", "ConnectionString",
            "ConfigurationReference", "CapabilityMetadata", "CapabilityFlags", "TimeoutPolicy",
        ];

        foreach (Type type in providerTypes)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                forbiddenTokens.ShouldNotContain(
                    token => property.Name.Contains(token, StringComparison.OrdinalIgnoreCase),
                    $"{type.Name}.{property.Name} must not expose a secret/credential/config-reference/metadata member (AC1; AD-9, AD-14).");

                // Only id-shaped strings, the int version, and the safe verdict enum may cross the public boundary.
                bool safeType = property.PropertyType == typeof(string)
                    || property.PropertyType == typeof(int)
                    || property.PropertyType == typeof(ProviderSelectionValidationStatus);
                safeType.ShouldBeTrue(
                    $"{type.Name}.{property.Name} exposes non-id-shaped type '{property.PropertyType.Name}' on the provider surface.");
            }
        }
    }

    [Fact]
    public void Status_view_exposes_safe_provider_selection_fields_only()
    {
        PropertyInfo[] properties = typeof(AgentStatusView).GetProperties();

        properties.ShouldContain(p => p.Name == "HasProviderSelection" && p.PropertyType == typeof(bool));
        properties.ShouldContain(p => p.Name == "SelectedProviderId" && p.PropertyType == typeof(string));
        properties.ShouldContain(p => p.Name == "SelectedModelId" && p.PropertyType == typeof(string));
        // The capability version, configuration reference, and any secret/metadata are deliberately NOT on the view.
        properties.ShouldNotContain(p => p.Name.Contains("CapabilityVersion", StringComparison.OrdinalIgnoreCase));
        properties.ShouldNotContain(p => p.Name.Contains("ConfigurationReference", StringComparison.OrdinalIgnoreCase));
    }
}
