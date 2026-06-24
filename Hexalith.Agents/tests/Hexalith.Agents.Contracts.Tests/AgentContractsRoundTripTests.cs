using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Serialization gap-fill for the Agent public surface (AC1–AC4; AD-2, AD-14). The dev-story suite round-trips
/// only <see cref="AgentCreated"/> and the lifecycle/blocker enums; durable event sourcing replays <em>every</em>
/// event and rejection type, so each must survive System.Text.Json — including the
/// <see cref="AgentActivationBlockedRejection"/> blocker collection and the enum-bearing
/// <see cref="AgentLifecycleStateAlreadySetRejection"/>. These tests round-trip the remaining success events, all
/// rejection events, the safe status view, and assert the lifecycle/blocker enums fail safe to
/// <see cref="AgentLifecycleStatus.Unknown"/> / <see cref="AgentActivationBlocker.Unknown"/> when absent.
/// </summary>
public sealed class AgentContractsRoundTripTests
{
    // ===== Success events =====

    [Fact]
    public void Configuration_updated_event_round_trips_through_system_text_json()
    {
        var updated = new AgentConfigurationUpdated(
            "hexa",
            "Hexa Renamed",
            "Updated description",
            "You are hexa, an updated and careful enterprise assistant.",
            InstructionsChanged: true,
            ConfigurationVersion: 2,
            InstructionsVersion: 2);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(updated);

        JsonSerializer.Deserialize<AgentConfigurationUpdated>(bytes).ShouldBe(updated);
    }

    [Fact]
    public void Configuration_updated_event_round_trips_with_a_null_description()
    {
        var updated = new AgentConfigurationUpdated(
            "hexa",
            "Hexa Renamed",
            Description: null,
            "You are hexa, an updated and careful enterprise assistant.",
            InstructionsChanged: false,
            ConfigurationVersion: 3,
            InstructionsVersion: 2);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(updated);

        JsonSerializer.Deserialize<AgentConfigurationUpdated>(bytes).ShouldBe(updated);
    }

    [Fact]
    public void Activated_event_round_trips_through_system_text_json()
    {
        var activated = new AgentActivated("hexa");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(activated);

        JsonSerializer.Deserialize<AgentActivated>(bytes).ShouldBe(activated);
    }

    [Fact]
    public void Disabled_event_round_trips_through_system_text_json()
    {
        var disabled = new AgentDisabled("hexa");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(disabled);

        JsonSerializer.Deserialize<AgentDisabled>(bytes).ShouldBe(disabled);
    }

    // ===== Rejection events =====

    [Fact]
    public void Administration_denied_rejection_round_trips_through_system_text_json()
    {
        var rejection = new AgentAdministrationDeniedRejection("hexa", "intruder", "CreateAgent");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(rejection);

        JsonSerializer.Deserialize<AgentAdministrationDeniedRejection>(bytes).ShouldBe(rejection);
    }

    [Fact]
    public void Not_found_rejection_round_trips_through_system_text_json()
    {
        var rejection = new AgentNotFoundRejection("hexa");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(rejection);

        JsonSerializer.Deserialize<AgentNotFoundRejection>(bytes).ShouldBe(rejection);
    }

    [Fact]
    public void Already_exists_rejection_round_trips_through_system_text_json()
    {
        var rejection = new AgentAlreadyExistsRejection("hexa");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(rejection);

        JsonSerializer.Deserialize<AgentAlreadyExistsRejection>(bytes).ShouldBe(rejection);
    }

    [Fact]
    public void Invalid_configuration_rejection_round_trips_through_system_text_json()
    {
        var rejection = new InvalidAgentConfigurationRejection("hexa", "DisplayName must not exceed 256 characters.");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(rejection);

        JsonSerializer.Deserialize<InvalidAgentConfigurationRejection>(bytes).ShouldBe(rejection);
    }

    [Fact]
    public void Lifecycle_already_set_rejection_round_trips_with_enum_status()
    {
        var rejection = new AgentLifecycleStateAlreadySetRejection(
            "hexa",
            AgentLifecycleStatus.Disabled,
            AgentLifecycleStatus.Disabled,
            "DisableAgent");

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(rejection);

        JsonSerializer.Deserialize<AgentLifecycleStateAlreadySetRejection>(bytes).ShouldBe(rejection);
    }

    [Fact]
    public void Activation_blocked_rejection_round_trips_the_blocker_collection()
    {
        var rejection = new AgentActivationBlockedRejection(
            "hexa",
            [AgentActivationBlocker.MissingDisplayName, AgentActivationBlocker.InvalidInstructions]);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(rejection);

        AgentActivationBlockedRejection? roundTripped = JsonSerializer.Deserialize<AgentActivationBlockedRejection>(bytes);
        roundTripped.ShouldNotBeNull();
        roundTripped.AgentId.ShouldBe("hexa");
        // The blockers survive in order and by name (compared element-wise, not by list reference).
        roundTripped.Blockers.ShouldBe(rejection.Blockers);
    }

    // ===== Safe status view =====

    [Fact]
    public void Status_view_round_trips_through_system_text_json_without_instructions_text()
    {
        var view = new AgentStatusView(
            "hexa",
            "acme",
            "Hexa Assistant",
            "Tenant governed assistant",
            AgentLifecycleStatus.Draft,
            ConfigurationVersion: 1,
            HasInstructions: true,
            InstructionsValid: false,
            InstructionsVersion: 1,
            HasPartyIdentity: false,
            HasProviderSelection: true,
            SelectedProviderId: "openai",
            SelectedModelId: "gpt-4o",
            ResponseMode: AgentResponseMode.Confirmation,
            HasApproverPolicy: true,
            ApproverPolicyDisclosure: ApproverPolicyBasisDisclosure.OperatorOnly,
            ApproverPolicyVersion: 2,
            HasContentSafetyPolicy: true,
            ContentSafetyPolicyVersion: 3,
            HasAutomaticContentSafetyOverride: false,
            HasConfirmationContentSafetyOverride: true,
            [AgentActivationBlocker.InvalidInstructions, AgentActivationBlocker.MissingPartyIdentity]);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(view);

        AgentStatusView? roundTripped = JsonSerializer.Deserialize<AgentStatusView>(bytes);
        roundTripped.ShouldNotBeNull();
        roundTripped.AgentId.ShouldBe(view.AgentId);
        roundTripped.Lifecycle.ShouldBe(view.Lifecycle);
        roundTripped.ConfigurationVersion.ShouldBe(view.ConfigurationVersion);
        roundTripped.HasInstructions.ShouldBe(view.HasInstructions);
        roundTripped.InstructionsValid.ShouldBe(view.InstructionsValid);
        roundTripped.InstructionsVersion.ShouldBe(view.InstructionsVersion);
        roundTripped.HasPartyIdentity.ShouldBe(view.HasPartyIdentity);
        roundTripped.HasProviderSelection.ShouldBe(view.HasProviderSelection);
        roundTripped.SelectedProviderId.ShouldBe(view.SelectedProviderId);
        roundTripped.SelectedModelId.ShouldBe(view.SelectedModelId);
        roundTripped.ResponseMode.ShouldBe(view.ResponseMode);
        roundTripped.HasApproverPolicy.ShouldBe(view.HasApproverPolicy);
        roundTripped.ApproverPolicyDisclosure.ShouldBe(view.ApproverPolicyDisclosure);
        roundTripped.ApproverPolicyVersion.ShouldBe(view.ApproverPolicyVersion);
        roundTripped.HasContentSafetyPolicy.ShouldBe(view.HasContentSafetyPolicy);
        roundTripped.ContentSafetyPolicyVersion.ShouldBe(view.ContentSafetyPolicyVersion);
        roundTripped.HasAutomaticContentSafetyOverride.ShouldBe(view.HasAutomaticContentSafetyOverride);
        roundTripped.HasConfirmationContentSafetyOverride.ShouldBe(view.HasConfirmationContentSafetyOverride);
        roundTripped.ActivationBlockers.ShouldBe(view.ActivationBlockers);
    }

    // ===== Enum fail-safe defaults =====

    [Fact]
    public void Unknown_lifecycle_status_is_the_default_so_absent_values_are_never_active()
    {
        // AD-2 / AC3 fail-safe: an absent/zero lifecycle must never deserialize to Active.
        default(AgentLifecycleStatus).ShouldBe(AgentLifecycleStatus.Unknown);
        JsonSerializer.Deserialize<AgentLifecycleStatus>("\"Unknown\"").ShouldBe(AgentLifecycleStatus.Unknown);
    }

    [Fact]
    public void Unknown_activation_blocker_is_the_default_so_absent_values_are_never_a_concrete_blocker()
    {
        default(AgentActivationBlocker).ShouldBe(AgentActivationBlocker.Unknown);
        JsonSerializer.Deserialize<AgentActivationBlocker>("\"Unknown\"").ShouldBe(AgentActivationBlocker.Unknown);
    }
}
