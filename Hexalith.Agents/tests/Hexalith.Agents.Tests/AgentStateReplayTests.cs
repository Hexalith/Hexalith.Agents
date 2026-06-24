using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Shouldly;

using static Hexalith.Agents.Tests.AgentTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Replay tests for <see cref="AgentState"/> (AC3 history preservation; replay determinism). Verifies the
/// <c>Apply</c> methods rebuild lifecycle deterministically, that disabling preserves all prior state, and that
/// the no-op rejection <c>Apply</c> methods keep replay total so a persisted rejection never breaks rehydration.
/// </summary>
public sealed class AgentStateReplayTests
{
    [Fact]
    public void Apply_create_activate_disable_tracks_lifecycle_and_preserves_history()
    {
        var state = new AgentState();

        state.Apply(CreatedEvent(ValidCreate()));
        state.IsCreated.ShouldBeTrue();
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);

        state.Apply(new AgentActivated(AgentId));
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Active);

        state.Apply(new AgentDisabled(AgentId));
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Disabled);

        // AC3: disabling is a lifecycle flag flip only — identity, instructions, and configuration are preserved.
        state.AgentId.ShouldBe(AgentId);
        state.TenantId.ShouldBe(TenantId);
        state.DisplayName.ShouldBe("Hexa Assistant");
        state.Instructions.ShouldBe(ValidInstructions);
        state.ConfigurationVersion.ShouldBe(1);
        state.InstructionsVersion.ShouldBe(1);
    }

    [Fact]
    public void Apply_disabled_then_reactivate_restores_active_without_losing_state()
    {
        AgentState state = DisabledStateWith(ValidCreate());

        state.Apply(new AgentActivated(AgentId));

        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Active);
        state.Instructions.ShouldBe(ValidInstructions); // history intact across the disable/reactivate cycle
    }

    [Fact]
    public void Apply_configuration_updated_rewrites_safe_fields_and_versions()
    {
        AgentState state = StateWith(ValidCreate());

        state.Apply(new AgentConfigurationUpdated(
            AgentId,
            "New Name",
            "New description",
            "You are hexa, a freshly updated assistant.",
            InstructionsChanged: true,
            ConfigurationVersion: 2,
            InstructionsVersion: 2));

        state.DisplayName.ShouldBe("New Name");
        state.Description.ShouldBe("New description");
        state.Instructions.ShouldBe("You are hexa, a freshly updated assistant.");
        state.ConfigurationVersion.ShouldBe(2);
        state.InstructionsVersion.ShouldBe(2);
    }

    [Fact]
    public void Apply_rejection_events_are_replay_safe_noops()
    {
        AgentState state = ActiveStateWith(ValidCreate());

        // Replaying a stream that contains persisted rejection events must not throw or mutate state.
        state.Apply(new AgentAdministrationDeniedRejection(AgentId, "intruder", "CreateAgent"));
        state.Apply(new AgentNotFoundRejection(AgentId));
        state.Apply(new AgentAlreadyExistsRejection(AgentId));
        state.Apply(new AgentActivationBlockedRejection(AgentId, [AgentActivationBlocker.MissingDisplayName]));
        state.Apply(new AgentLifecycleStateAlreadySetRejection(
            AgentId, AgentLifecycleStatus.Active, AgentLifecycleStatus.Active, "ActivateAgent"));
        state.Apply(new InvalidAgentConfigurationRejection(AgentId, "reason"));

        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Active);
        state.DisplayName.ShouldBe("Hexa Assistant");
        state.ConfigurationVersion.ShouldBe(1);
    }

    [Fact]
    public void Apply_pre_create_rejection_only_leaves_state_uncreated()
    {
        // A stream that contains only a persisted pre-create rejection must rehydrate to a non-created state, so
        // handlers and inspection still treat the Agent as not existing.
        var state = new AgentState();

        state.Apply(new AgentAdministrationDeniedRejection(AgentId, "intruder", "CreateAgent"));

        state.IsCreated.ShouldBeFalse();
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Unknown);
    }

    [Fact]
    public void Apply_party_link_then_replace_tracks_a_single_identity_and_bumps_version()
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1, no party

        state.Apply(new AgentPartyIdentityLinked(AgentId, "party-001", ConfigurationVersion: 2));
        state.PartyId.ShouldBe("party-001");
        state.ConfigurationVersion.ShouldBe(2);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft); // linking never changes lifecycle

        state.Apply(new AgentPartyIdentityReplaced(AgentId, "party-001", "party-002", ConfigurationVersion: 3));
        state.PartyId.ShouldBe("party-002"); // still exactly one identity, now the replacement
        state.ConfigurationVersion.ShouldBe(3);
    }

    [Fact]
    public void Apply_party_rejection_events_are_replay_safe_noops()
    {
        AgentState state = StateWithLinkedParty(ValidCreate());
        string? partyBefore = state.PartyId;

        state.Apply(new AgentPartyIdentityLinkRejected(AgentId, PartyLinkValidationStatus.Unavailable));
        state.Apply(new AgentPartyIdentityAlreadyLinkedRejection(AgentId, "party-999"));

        state.PartyId.ShouldBe(partyBefore); // persisted rejections never mutate the linked identity
    }

    [Fact]
    public void Apply_party_link_before_create_is_ignored()
    {
        // A link event replayed against a never-created state must be ignored (IsCreated guard), exactly like the
        // other update applies.
        var state = new AgentState();

        state.Apply(new AgentPartyIdentityLinked(AgentId, "party-001", ConfigurationVersion: 2));

        state.PartyId.ShouldBeNull();
        state.IsCreated.ShouldBeFalse();
    }
}
