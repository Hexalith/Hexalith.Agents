using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// End-to-end lifecycle tests for the governed Agent (<c>hexa</c>) feature (Story 1.3). Unlike the focused
/// Handle/Apply unit tests, these drive the <em>real</em> aggregate pipeline — JSON command envelope →
/// reflection dispatch in <see cref="AgentAggregate.ProcessAsync"/> → typed handler → events — and thread the
/// evolving state across successive commands through the production <c>Apply</c> replay handlers, then assert the
/// outcome through the authorized inspection read path (<see cref="AgentInspection"/>). Each test maps to an
/// acceptance criterion: AC1 (durable governed record with safe audit facts), AC2 (activation blocked by
/// missing/invalid fields, then remediated), AC3 (disabled is publicly visible and history is preserved), AC4
/// (administration authorization fails closed without leaking), plus idempotency and replay determinism.
/// </summary>
public sealed class AgentLifecycleE2ETests
{
    // ===== AC1: an authorized create is governed and inspectable through the safe status path =====

    [Fact]
    public async Task Authorized_create_then_inspect_surfaces_governed_record_without_instructions_text()
    {
        var aggregate = new AgentAggregate();
        var state = new AgentState();
        CreateAgent create = ValidCreate();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, create);

        result.IsSuccess.ShouldBeTrue();

        // Inspect through the authorized read path — the governed identity/lifecycle/version round-trips safely.
        AgentInspectionResult inspection = AgentInspection.GetStatus(state, isAgentsAdmin: true);
        inspection.Status.ShouldBe(AgentInspectionStatus.Success);
        AgentStatusView view = inspection.Agent.ShouldNotBeNull();
        view.AgentId.ShouldBe(AgentId);
        view.TenantId.ShouldBe(TenantId);
        view.DisplayName.ShouldBe("Hexa Assistant");
        view.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft); // new agents start as a non-callable draft
        view.ConfigurationVersion.ShouldBe(1);
        view.HasInstructions.ShouldBeTrue();
        view.InstructionsValid.ShouldBeTrue();
        view.InstructionsVersion.ShouldBe(1);
        view.HasPartyIdentity.ShouldBeFalse(); // 1.4 AC4: a newly created agent is not yet linked to a Party
        view.ActivationBlockers.ShouldBe([AgentActivationBlocker.MissingPartyIdentity]);

        // AC1 / AD-14: the safe status surface never carries the raw Agent Instructions text.
        JsonSerializer.Serialize(view).ShouldNotContain(ValidInstructions);
    }

    // ===== AC2: activation is blocked by missing/invalid fields, then succeeds once remediated =====

    [Fact]
    public async Task Activation_blocked_until_required_fields_are_remediated_then_succeeds()
    {
        var aggregate = new AgentAggregate();
        var state = new AgentState();

        // Create an incomplete draft (no display name, no instructions).
        (await ProcessAndApplyAsync(aggregate, state, ValidCreate(displayName: "", instructions: ""))).IsSuccess.ShouldBeTrue();

        // AC2: activation is rejected with the specific blockers and the lifecycle stays non-active.
        DomainResult blocked = await ProcessAndApplyAsync(aggregate, state, new ActivateAgent());
        AgentActivationBlockedRejection rejection = blocked.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        rejection.Blockers.ShouldContain(AgentActivationBlocker.MissingDisplayName);
        rejection.Blockers.ShouldContain(AgentActivationBlocker.MissingInstructions);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft); // rejected activation never makes hexa callable

        // The status path reports the same blockers an activation would reject (read/write in lock-step).
        AgentStatusView draftView = AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull();
        draftView.ActivationBlockers.ShouldBe(rejection.Blockers);

        // Remediate via an authorized configuration update.
        (await ProcessAndApplyAsync(aggregate, state, new UpdateAgentConfiguration("Hexa Assistant", "desc", ValidInstructions)))
            .IsSuccess.ShouldBeTrue();

        // 1.4 AC4: the party-identity gate remains until a valid Party is linked.
        AgentStatusView remediatedView = AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull();
        remediatedView.ActivationBlockers.ShouldBe([AgentActivationBlocker.MissingPartyIdentity]);
        var link = new LinkAgentPartyIdentity(LinkedPartyId);
        (await ProcessAndApplyAsync(aggregate, state, link, LinkEnvelope(link))).IsSuccess.ShouldBeTrue();

        // AC2: activation now succeeds and the agent is active with no remaining blockers.
        (await ProcessAndApplyAsync(aggregate, state, new ActivateAgent())).IsSuccess.ShouldBeTrue();
        AgentStatusView activeView = AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull();
        activeView.Lifecycle.ShouldBe(AgentLifecycleStatus.Active);
        activeView.ActivationBlockers.ShouldBeEmpty();
        activeView.InstructionsValid.ShouldBeTrue();
    }

    // ===== AC3: disable is publicly visible, preserves history, and reactivation re-runs the gates =====

    [Fact]
    public async Task Create_activate_disable_reactivate_preserves_history_and_keeps_disabled_visible()
    {
        var aggregate = new AgentAggregate();
        var state = new AgentState();
        CreateAgent create = ValidCreate();

        (await ProcessAndApplyAsync(aggregate, state, create)).IsSuccess.ShouldBeTrue();

        // 1.4 AC4: a linked Party is required before the agent can activate.
        var link = new LinkAgentPartyIdentity(LinkedPartyId);
        (await ProcessAndApplyAsync(aggregate, state, link, LinkEnvelope(link))).IsSuccess.ShouldBeTrue();

        (await ProcessAndApplyAsync(aggregate, state, new ActivateAgent())).IsSuccess.ShouldBeTrue();

        // Disable: a lifecycle flag flip only.
        (await ProcessAndApplyAsync(aggregate, state, new DisableAgent())).IsSuccess.ShouldBeTrue();

        // AC3: the disabled state is visible through the public status path...
        AgentStatusView disabledView = AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull();
        disabledView.Lifecycle.ShouldBe(AgentLifecycleStatus.Disabled);

        // ...and prior identity/instructions/configuration are not deleted or rewritten by the disable.
        disabledView.DisplayName.ShouldBe(create.DisplayName);
        disabledView.ConfigurationVersion.ShouldBe(2); // create (1) + party link (2)
        disabledView.HasInstructions.ShouldBeTrue();
        disabledView.HasPartyIdentity.ShouldBeTrue(); // the link survives the disable (history preserved, AC3)
        disabledView.InstructionsVersion.ShouldBe(1);
        state.Instructions.ShouldBe(ValidInstructions); // durable instructions survive the disable

        // Reactivating a disabled-but-valid agent re-runs the gates and restores Active.
        (await ProcessAndApplyAsync(aggregate, state, new ActivateAgent())).IsSuccess.ShouldBeTrue();
        AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull()
            .Lifecycle.ShouldBe(AgentLifecycleStatus.Active);
    }

    // ===== AC4: authorization fails closed — the request fails before any mutation =====

    [Fact]
    public async Task Unauthorized_create_fails_before_mutation_and_authorized_inspection_sees_no_agent()
    {
        var aggregate = new AgentAggregate();
        var state = new AgentState();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, ValidCreate(), isAgentsAdmin: false);

        // The mutation is rejected with a generic denial...
        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();

        // ...and no agent was ever created (fails before mutation). A later authorized read sees no agent.
        AgentInspection.GetStatus(state, isAgentsAdmin: true).Status.ShouldBe(AgentInspectionStatus.AgentNotFound);
    }

    [Fact]
    public async Task Unauthorized_caller_cannot_inspect_even_after_authorized_create()
    {
        var aggregate = new AgentAggregate();
        var state = new AgentState();

        (await ProcessAndApplyAsync(aggregate, state, ValidCreate())).IsSuccess.ShouldBeTrue();

        // AC4: unauthorized inspection fails closed with no agent data (no fingerprinting of whether hexa exists).
        AgentInspectionResult inspection = AgentInspection.GetStatus(state, isAgentsAdmin: false);
        inspection.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        inspection.Agent.ShouldBeNull();
    }

    // ===== AC1/AC4: idempotency — duplicate delivery is a no-op; a conflicting payload is rejected, never silent =====

    [Fact]
    public async Task Duplicate_create_delivery_is_idempotent_and_conflicting_payload_is_rejected()
    {
        var aggregate = new AgentAggregate();
        var state = new AgentState();
        CreateAgent create = ValidCreate();

        (await ProcessAndApplyAsync(aggregate, state, create)).IsSuccess.ShouldBeTrue();

        // Exact-duplicate re-delivery → deterministic no-op, state unchanged.
        (await ProcessAndApplyAsync(aggregate, state, create)).IsNoOp.ShouldBeTrue();

        // Conflicting payload (same identity, different display name) → rejection, never a silent mutation.
        DomainResult conflict = await ProcessAndApplyAsync(aggregate, state, create with { DisplayName = "Conflicting Name" });
        conflict.IsRejection.ShouldBeTrue();
        _ = conflict.Events[0].ShouldBeOfType<AgentAlreadyExistsRejection>();

        // Inspection confirms the original display name survived — the conflicting create did not mutate state.
        AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull()
            .DisplayName.ShouldBe(create.DisplayName);
    }

    // ===== AC3/AC1: rehydration is deterministic — replaying the emitted stream reproduces identical state =====

    [Fact]
    public async Task Replaying_the_emitted_event_stream_rehydrates_identical_state()
    {
        var aggregate = new AgentAggregate();
        var live = new AgentState();

        // A full admin journey: create → rename/update → activate → disable. We capture every emitted success
        // event so we can replay the exact stream into a fresh state and prove deterministic rehydration.
        var stream = new List<IEventPayload>();

        async Task Drive<TCommand>(TCommand command) where TCommand : notnull
        {
            DomainResult r = await ProcessAndApplyAsync(aggregate, live, command);
            r.IsRejection.ShouldBeFalse();
            stream.AddRange(r.Events);
        }

        async Task DriveWith<TCommand>(TCommand command, CommandEnvelope envelope) where TCommand : notnull
        {
            DomainResult r = await ProcessAndApplyAsync(aggregate, live, command, envelope);
            r.IsRejection.ShouldBeFalse();
            stream.AddRange(r.Events);
        }

        await Drive(ValidCreate());
        await Drive(new UpdateAgentConfiguration("Hexa Renamed", "Updated description", "You are hexa, an updated and careful assistant."));
        var link = new LinkAgentPartyIdentity(LinkedPartyId); // 1.4 AC4: link before activation
        await DriveWith(link, LinkEnvelope(link));
        await Drive(new ActivateAgent());
        await Drive(new DisableAgent());

        // Replay the captured stream into a brand-new state through the production Apply handlers.
        var replayed = new AgentState();
        ApplyAll(replayed, new DomainResult(stream));

        // Both states must yield identical inspection views — proving replay is deterministic regardless of how
        // the state was built. (Records with a collection member are compared field-wise; the blocker list is
        // compared element-wise so structural — not reference — equality is asserted.)
        AgentStatusView liveView = AgentInspection.GetStatus(live, isAgentsAdmin: true).Agent.ShouldNotBeNull();
        AgentStatusView replayedView = AgentInspection.GetStatus(replayed, isAgentsAdmin: true).Agent.ShouldNotBeNull();

        replayedView.AgentId.ShouldBe(liveView.AgentId);
        replayedView.TenantId.ShouldBe(liveView.TenantId);
        replayedView.DisplayName.ShouldBe(liveView.DisplayName);
        replayedView.Description.ShouldBe(liveView.Description);
        replayedView.Lifecycle.ShouldBe(liveView.Lifecycle);
        replayedView.Lifecycle.ShouldBe(AgentLifecycleStatus.Disabled);
        replayedView.ConfigurationVersion.ShouldBe(liveView.ConfigurationVersion);
        replayedView.ConfigurationVersion.ShouldBe(3); // create (1) + one accepted update (2) + party link (3)
        replayedView.HasInstructions.ShouldBe(liveView.HasInstructions);
        replayedView.InstructionsValid.ShouldBe(liveView.InstructionsValid);
        replayedView.InstructionsVersion.ShouldBe(liveView.InstructionsVersion);
        replayedView.InstructionsVersion.ShouldBe(2); // instructions text changed once on the update
        replayedView.HasPartyIdentity.ShouldBe(liveView.HasPartyIdentity);
        replayedView.HasPartyIdentity.ShouldBeTrue(); // the party link replays deterministically
        replayedView.ActivationBlockers.ShouldBe(liveView.ActivationBlockers);
    }
}
