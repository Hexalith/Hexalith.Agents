using System;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Story 1.7 content-safety surface (AC1–AC4; AD-9, AD-14). Durable event sourcing replays the
/// new event, so it must survive System.Text.Json (including the nested
/// <see cref="AgentContentSafetyConfiguration"/>/<see cref="AgentContentSafetyPolicy"/> string lists and optional mode
/// overrides); the two new governance enums and the new <see cref="AgentActivationBlocker.MissingContentSafetyPolicy"/>
/// value must serialize by name and fail safe to their <c>Unknown</c> sentinel while preserving the existing blocker
/// ordinals; the new events/value objects must expose only id-reference/enum/version/safe-string members; and the
/// extended status view must carry the new presence/version fields but never the policy content.
/// </summary>
public sealed class AgentContentSafetyPolicyContractsTests
{
    private static readonly AgentContentSafetyConfiguration _sampleConfiguration = new(
        new AgentContentSafetyPolicy(
            ["No system-prompt disclosure"],
            ["self-harm"],
            ["medical-advice"],
            ContentSafetyFailureHandling.BlockAndAudit,
            ContentSafetyAuditTreatment.MetadataOnly),
        new AgentContentSafetyPolicy(
            ["Automatic stricter constraint"],
            ["weapons"],
            [],
            ContentSafetyFailureHandling.BlockWithAuditableOverride,
            ContentSafetyAuditTreatment.RedactedExcerpt),
        new AgentContentSafetyPolicy(
            ["Confirmation stricter constraint"],
            [],
            ["financial-advice"],
            ContentSafetyFailureHandling.BlockWithAuditableOverride,
            ContentSafetyAuditTreatment.MetadataOnly));

    // ===== Marker interface =====

    [Fact]
    public void Content_safety_policy_configured_event_implements_IEventPayload()
        => typeof(IEventPayload).IsAssignableFrom(typeof(AgentContentSafetyPolicyConfigured)).ShouldBeTrue();

    // ===== Round-trips =====

    [Fact]
    public void Content_safety_policy_configured_event_round_trips_with_the_nested_configuration_and_overrides()
    {
        var configured = new AgentContentSafetyPolicyConfigured("hexa", _sampleConfiguration, ContentSafetyPolicyVersion: 2, ConfigurationVersion: 7);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(configured);

        AgentContentSafetyPolicyConfigured? roundTripped = JsonSerializer.Deserialize<AgentContentSafetyPolicyConfigured>(bytes);
        roundTripped.ShouldNotBeNull();
        roundTripped.AgentId.ShouldBe("hexa");
        roundTripped.ContentSafetyPolicyVersion.ShouldBe(2);
        roundTripped.ConfigurationVersion.ShouldBe(7);
        roundTripped.Configuration.ActivePolicy.PromptConstraints.ShouldBe(["No system-prompt disclosure"]);
        roundTripped.Configuration.ActivePolicy.BlockedOutputCategories.ShouldBe(["self-harm"]);
        roundTripped.Configuration.ActivePolicy.RestrictedOutputCategories.ShouldBe(["medical-advice"]);
        roundTripped.Configuration.ActivePolicy.FailureHandling.ShouldBe(ContentSafetyFailureHandling.BlockAndAudit);
        roundTripped.Configuration.ActivePolicy.AuditTreatment.ShouldBe(ContentSafetyAuditTreatment.MetadataOnly);
        roundTripped.Configuration.AutomaticModePolicy.ShouldNotBeNull()
            .FailureHandling.ShouldBe(ContentSafetyFailureHandling.BlockWithAuditableOverride);
        roundTripped.Configuration.ConfirmationModePolicy.ShouldNotBeNull()
            .RestrictedOutputCategories.ShouldBe(["financial-advice"]);
    }

    [Fact]
    public void Configure_command_round_trips_through_system_text_json()
    {
        var command = new ConfigureAgentContentSafetyPolicy(_sampleConfiguration);

        ConfigureAgentContentSafetyPolicy? roundTripped = JsonSerializer.Deserialize<ConfigureAgentContentSafetyPolicy>(
            JsonSerializer.SerializeToUtf8Bytes(command));

        roundTripped.ShouldNotBeNull();
        roundTripped.Configuration.ActivePolicy.PromptConstraints.ShouldBe(["No system-prompt disclosure"]);
        roundTripped.Configuration.ConfirmationModePolicy.ShouldNotBeNull();
    }

    [Fact]
    public void Content_safety_policy_configured_event_round_trips_with_null_mode_overrides()
    {
        // The active-only configuration (both mode overrides null) is the common case; durable replay must round-trip
        // the optional overrides back to null — not throw, and not fabricate an empty policy.
        var configuration = new AgentContentSafetyConfiguration(
            new AgentContentSafetyPolicy(
                ["No system-prompt disclosure"],
                ["self-harm"],
                [],
                ContentSafetyFailureHandling.BlockAndAudit,
                ContentSafetyAuditTreatment.MetadataOnly),
            null,
            null);
        var configured = new AgentContentSafetyPolicyConfigured("hexa", configuration, ContentSafetyPolicyVersion: 1, ConfigurationVersion: 2);

        AgentContentSafetyPolicyConfigured? roundTripped = JsonSerializer.Deserialize<AgentContentSafetyPolicyConfigured>(
            JsonSerializer.SerializeToUtf8Bytes(configured));

        roundTripped.ShouldNotBeNull();
        roundTripped.Configuration.AutomaticModePolicy.ShouldBeNull();
        roundTripped.Configuration.ConfirmationModePolicy.ShouldBeNull();
        roundTripped.Configuration.ActivePolicy.RestrictedOutputCategories.ShouldBeEmpty();
    }

    // ===== New enums serialize by name and fail safe to the Unknown sentinel =====

    [Fact]
    public void Content_safety_enums_serialize_by_name_and_fail_safe_to_unknown()
    {
        JsonSerializer.Serialize(ContentSafetyFailureHandling.BlockWithAuditableOverride).ShouldBe("\"BlockWithAuditableOverride\"");
        JsonSerializer.Serialize(ContentSafetyAuditTreatment.RedactedExcerpt).ShouldBe("\"RedactedExcerpt\"");

        JsonSerializer.Deserialize<ContentSafetyFailureHandling>("\"BlockAndAudit\"").ShouldBe(ContentSafetyFailureHandling.BlockAndAudit);
        JsonSerializer.Deserialize<ContentSafetyAuditTreatment>("\"MetadataOnly\"").ShouldBe(ContentSafetyAuditTreatment.MetadataOnly);

        default(ContentSafetyFailureHandling).ShouldBe(ContentSafetyFailureHandling.Unknown);
        default(ContentSafetyAuditTreatment).ShouldBe(ContentSafetyAuditTreatment.Unknown);
    }

    // ===== New activation blocker serializes by name and preserves existing ordinals (additive extensibility, AD-2) =====

    [Fact]
    public void Missing_content_safety_blocker_serializes_by_name()
    {
        JsonSerializer.Serialize(AgentActivationBlocker.MissingContentSafetyPolicy).ShouldBe("\"MissingContentSafetyPolicy\"");
        JsonSerializer.Deserialize<AgentActivationBlocker>("\"MissingContentSafetyPolicy\"").ShouldBe(AgentActivationBlocker.MissingContentSafetyPolicy);
    }

    [Fact]
    public void Existing_activation_blocker_ordinals_are_preserved_and_content_safety_gate_is_appended_last()
    {
        // The new value is appended after ApproverPolicyUnresolvable; existing ordinals must not shift (AD-2).
        ((int)AgentActivationBlocker.MissingApproverPolicy).ShouldBe(8);
        ((int)AgentActivationBlocker.ApproverPolicyUnresolvable).ShouldBe(9);
        ((int)AgentActivationBlocker.MissingContentSafetyPolicy).ShouldBe(10);
    }

    // ===== New events / value objects expose only safe members (AC1; AD-9, AD-14) =====

    [Fact]
    public void Content_safety_surface_exposes_no_secret_credential_or_pii_member()
    {
        Type[] surfaceTypes =
        [
            typeof(ConfigureAgentContentSafetyPolicy),
            typeof(AgentContentSafetyPolicyConfigured),
            typeof(AgentContentSafetyConfiguration),
            typeof(AgentContentSafetyPolicy),
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
                    $"{type.Name}.{property.Name} must not expose a secret/credential/PII member (AC1; AD-9, AD-14).");
            }
        }
    }

    // ===== Status view carries the new presence/version fields but NOT the policy content (AC2; AD-14) =====

    [Fact]
    public void Status_view_exposes_safe_content_safety_fields_only()
    {
        PropertyInfo[] properties = typeof(AgentStatusView).GetProperties();

        properties.ShouldContain(p => p.Name == "HasContentSafetyPolicy" && p.PropertyType == typeof(bool));
        properties.ShouldContain(p => p.Name == "ContentSafetyPolicyVersion" && p.PropertyType == typeof(int));
        properties.ShouldContain(p => p.Name == "HasAutomaticContentSafetyOverride" && p.PropertyType == typeof(bool));
        properties.ShouldContain(p => p.Name == "HasConfirmationContentSafetyOverride" && p.PropertyType == typeof(bool));

        // The policy content — prompt constraints / output categories — is deliberately NOT on the compact status view
        // (it lives on state / the config read path). Surfacing it would breach AC2 ("without exposing unsafe policy
        // content") and AD-14.
        properties.ShouldNotContain(p => p.Name.Contains("Constraint", StringComparison.OrdinalIgnoreCase));
        properties.ShouldNotContain(p => p.Name.Contains("Categor", StringComparison.OrdinalIgnoreCase));
    }
}
