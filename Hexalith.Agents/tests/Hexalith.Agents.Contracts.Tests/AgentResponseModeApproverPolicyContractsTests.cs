using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Story 1.6 response-mode + approver-policy surface (AC1–AC4; AD-7, AD-9, AD-14). Durable
/// event sourcing replays every new event, so each must survive System.Text.Json (including the nested
/// <see cref="AgentApproverPolicy"/>/<see cref="ApproverPolicySource"/> list); the new enums and the three new
/// <see cref="AgentActivationBlocker"/> values must serialize by name and fail safe to their <c>Unknown</c>
/// sentinel; and the new events/value objects must expose only id-reference/enum/version-shaped members — never a
/// secret, credential, or Party-PII member.
/// </summary>
public sealed class AgentResponseModeApproverPolicyContractsTests
{
    private static readonly AgentApproverPolicy _samplePolicy = new(
        [
            new ApproverPolicySource(ApproverPolicySourceKind.Caller, null, null),
            new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, "party-approver", null),
            new ApproverPolicySource(ApproverPolicySourceKind.TenantRole, null, "tenant-approver"),
            new ApproverPolicySource(ApproverPolicySourceKind.ConversationOwner, null, null),
        ],
        ApproverPolicyBasisDisclosure.OperatorOnly);

    // ===== Marker interfaces =====

    [Fact]
    public void Response_mode_configured_event_implements_IEventPayload()
        => typeof(IEventPayload).IsAssignableFrom(typeof(AgentResponseModeConfigured)).ShouldBeTrue();

    [Fact]
    public void Approver_policy_configured_event_implements_IEventPayload()
        => typeof(IEventPayload).IsAssignableFrom(typeof(AgentApproverPolicyConfigured)).ShouldBeTrue();

    // ===== Round-trips =====

    [Fact]
    public void Response_mode_configured_event_round_trips_through_system_text_json()
    {
        var configured = new AgentResponseModeConfigured("hexa", AgentResponseMode.Confirmation, ConfigurationVersion: 4);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(configured);

        JsonSerializer.Deserialize<AgentResponseModeConfigured>(bytes).ShouldBe(configured);
    }

    [Fact]
    public void Approver_policy_configured_event_round_trips_with_the_nested_policy_and_sources()
    {
        var configured = new AgentApproverPolicyConfigured("hexa", _samplePolicy, ApproverPolicyVersion: 2, ConfigurationVersion: 5);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(configured);

        AgentApproverPolicyConfigured? roundTripped = JsonSerializer.Deserialize<AgentApproverPolicyConfigured>(bytes);
        roundTripped.ShouldNotBeNull();
        roundTripped.AgentId.ShouldBe("hexa");
        roundTripped.ApproverPolicyVersion.ShouldBe(2);
        roundTripped.ConfigurationVersion.ShouldBe(5);
        roundTripped.Policy.DisclosureCategory.ShouldBe(ApproverPolicyBasisDisclosure.OperatorOnly);
        // The ordered approver sources survive by value (element-wise record comparison).
        roundTripped.Policy.Sources.ShouldBe(_samplePolicy.Sources);
    }

    [Fact]
    public void Configure_commands_round_trip_through_system_text_json()
    {
        var mode = new ConfigureAgentResponseMode(AgentResponseMode.Automatic);
        JsonSerializer.Deserialize<ConfigureAgentResponseMode>(JsonSerializer.SerializeToUtf8Bytes(mode)).ShouldBe(mode);

        var policy = new ConfigureAgentApproverPolicy(_samplePolicy);
        ConfigureAgentApproverPolicy? roundTripped = JsonSerializer.Deserialize<ConfigureAgentApproverPolicy>(JsonSerializer.SerializeToUtf8Bytes(policy));
        roundTripped.ShouldNotBeNull();
        roundTripped.Policy.Sources.ShouldBe(_samplePolicy.Sources);
        roundTripped.Policy.DisclosureCategory.ShouldBe(ApproverPolicyBasisDisclosure.OperatorOnly);
    }

    // ===== New enums serialize by name and fail safe =====

    [Fact]
    public void Response_mode_serializes_by_name_and_is_unknown_for_absent_or_unrecognized_input()
    {
        JsonSerializer.Serialize(AgentResponseMode.Confirmation).ShouldBe("\"Confirmation\"");
        JsonSerializer.Deserialize<AgentResponseMode>("\"Automatic\"").ShouldBe(AgentResponseMode.Automatic);
        default(AgentResponseMode).ShouldBe(AgentResponseMode.Unknown);
        JsonSerializer.Deserialize<AgentResponseMode>("\"Unknown\"").ShouldBe(AgentResponseMode.Unknown);
    }

    [Fact]
    public void Approver_policy_enums_serialize_by_name_and_fail_safe_to_unknown()
    {
        JsonSerializer.Serialize(ApproverPolicySourceKind.PredefinedParty).ShouldBe("\"PredefinedParty\"");
        JsonSerializer.Serialize(ApproverPolicyBasisDisclosure.OperatorOnly).ShouldBe("\"OperatorOnly\"");
        JsonSerializer.Serialize(ApproverPolicyValidationStatus.Valid).ShouldBe("\"Valid\"");

        default(ApproverPolicySourceKind).ShouldBe(ApproverPolicySourceKind.Unknown);
        default(ApproverPolicyBasisDisclosure).ShouldBe(ApproverPolicyBasisDisclosure.Unknown);
        default(ApproverPolicyValidationStatus).ShouldBe(ApproverPolicyValidationStatus.Unknown);
    }

    // ===== New activation blockers serialize by name and preserve ordinals (additive extensibility, AD-2) =====

    [Fact]
    public void New_response_and_approver_activation_blockers_serialize_by_name()
    {
        JsonSerializer.Serialize(AgentActivationBlocker.MissingResponseMode).ShouldBe("\"MissingResponseMode\"");
        JsonSerializer.Serialize(AgentActivationBlocker.MissingApproverPolicy).ShouldBe("\"MissingApproverPolicy\"");
        JsonSerializer.Serialize(AgentActivationBlocker.ApproverPolicyUnresolvable).ShouldBe("\"ApproverPolicyUnresolvable\"");
        JsonSerializer.Deserialize<AgentActivationBlocker>("\"ApproverPolicyUnresolvable\"").ShouldBe(AgentActivationBlocker.ApproverPolicyUnresolvable);
    }

    [Fact]
    public void Existing_activation_blocker_ordinals_are_preserved_and_response_approver_gates_are_appended()
    {
        // The new values are appended after ProviderUnavailable; existing ordinals must not shift (AD-2).
        ((int)AgentActivationBlocker.MissingProviderSelection).ShouldBe(5);
        ((int)AgentActivationBlocker.ProviderUnavailable).ShouldBe(6);
        ((int)AgentActivationBlocker.MissingResponseMode).ShouldBe(7);
        ((int)AgentActivationBlocker.MissingApproverPolicy).ShouldBe(8);
        ((int)AgentActivationBlocker.ApproverPolicyUnresolvable).ShouldBe(9);
    }

    // ===== New events / value objects expose only safe id/enum/version members (AC1; AD-7, AD-9, AD-14) =====

    [Fact]
    public void Response_and_approver_surface_exposes_no_secret_credential_or_pii_member()
    {
        Type[] surfaceTypes =
        [
            typeof(ConfigureAgentResponseMode),
            typeof(ConfigureAgentApproverPolicy),
            typeof(AgentResponseModeConfigured),
            typeof(AgentApproverPolicyConfigured),
            typeof(AgentApproverPolicy),
            typeof(ApproverPolicySource),
        ];

        string[] forbiddenTokens =
        [
            "Secret", "ApiKey", "Credential", "Password", "ConnectionString",
            "DisplayName", "Contact", "Email", "Phone", "PersonalData",
        ];

        foreach (Type type in surfaceTypes)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                forbiddenTokens.ShouldNotContain(
                    token => property.Name.Contains(token, StringComparison.OrdinalIgnoreCase),
                    $"{type.Name}.{property.Name} must not expose a secret/credential/PII member (AC1; AD-7, AD-9, AD-14).");
            }
        }
    }

    [Fact]
    public void Approver_policy_source_exposes_only_safe_reference_and_enum_members()
    {
        PropertyInfo[] properties = typeof(ApproverPolicySource).GetProperties();

        properties.ShouldContain(p => p.Name == "Kind" && p.PropertyType == typeof(ApproverPolicySourceKind));
        properties.ShouldContain(p => p.Name == "PartyId" && p.PropertyType == typeof(string));   // a reference, not PII (AD-7)
        properties.ShouldContain(p => p.Name == "TenantRole" && p.PropertyType == typeof(string)); // a safe role name
        properties.Length.ShouldBe(3); // no other (PII-bearing) member is on the source
    }

    [Fact]
    public void Status_view_exposes_safe_response_and_approver_fields_only()
    {
        PropertyInfo[] properties = typeof(AgentStatusView).GetProperties();

        properties.ShouldContain(p => p.Name == "ResponseMode" && p.PropertyType == typeof(AgentResponseMode));
        properties.ShouldContain(p => p.Name == "HasApproverPolicy" && p.PropertyType == typeof(bool));
        properties.ShouldContain(p => p.Name == "ApproverPolicyDisclosure" && p.PropertyType == typeof(ApproverPolicyBasisDisclosure));
        properties.ShouldContain(p => p.Name == "ApproverPolicyVersion" && p.PropertyType == typeof(int));
        // The full approver-source list is deliberately NOT on the compact status view (it lives on state / the config read path).
        properties.ShouldNotContain(p => p.Name.Contains("Sources", StringComparison.OrdinalIgnoreCase));
    }
}
