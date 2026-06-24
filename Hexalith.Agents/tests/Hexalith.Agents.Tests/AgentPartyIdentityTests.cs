using System.Threading.Tasks;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Handle-method tests for the Party-identity link/replace behaviour added to <see cref="AgentAggregate"/>
/// (Story 1.4). Covers: the AD-3 trusted-verdict gate and fail-closed rejection on any non-<c>Valid</c>/absent
/// verdict (AC2), storing only the stable <c>PartyId</c> reference and bumping the configuration version while
/// lifecycle stays unchanged (AC1), exactly-one-identity with idempotent re-link and explicit replacement (AC3),
/// the <c>MissingPartyIdentity</c> readiness gate blocking then unblocking activation (AC4), and authorization /
/// not-found fail-closed behaviour.
/// </summary>
public sealed class AgentPartyIdentityTests
{
    private const string OtherPartyId = "party-002";

    // ===== AC1: link success stores only the id reference, bumps version, lifecycle unchanged =====

    [Fact]
    public void Link_with_valid_verdict_stores_party_id_bumps_version_and_keeps_lifecycle()
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1, no party

        var command = new LinkAgentPartyIdentity(LinkedPartyId);
        DomainResult result = AgentAggregate.Handle(command, state, LinkEnvelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentPartyIdentityLinked linked = result.Events[0].ShouldBeOfType<AgentPartyIdentityLinked>();
        linked.AgentId.ShouldBe(AgentId);
        linked.PartyId.ShouldBe(LinkedPartyId);
        linked.ConfigurationVersion.ShouldBe(2);

        ApplyAll(state, result);
        state.PartyId.ShouldBe(LinkedPartyId);
        state.ConfigurationVersion.ShouldBe(2);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft); // linking never changes lifecycle (Story 1.3 invariant)
    }

    // ===== AC2: any non-Valid verdict fails closed — not stored, and MissingPartyIdentity still blocks =====

    [Theory]
    [InlineData(PartyLinkValidationStatus.Missing)]
    [InlineData(PartyLinkValidationStatus.Disabled)]
    [InlineData(PartyLinkValidationStatus.Ambiguous)]
    [InlineData(PartyLinkValidationStatus.Unavailable)]
    [InlineData(PartyLinkValidationStatus.Unauthorized)]
    [InlineData(PartyLinkValidationStatus.Unknown)]
    public void Link_with_non_valid_verdict_rejects_with_that_status_and_stores_nothing(PartyLinkValidationStatus verdict)
    {
        AgentState state = StateWith(ValidCreate());

        var command = new LinkAgentPartyIdentity(LinkedPartyId);
        DomainResult result = AgentAggregate.Handle(command, state, LinkEnvelope(command, verdict));

        result.IsRejection.ShouldBeTrue();
        AgentPartyIdentityLinkRejected rejected = result.Events[0].ShouldBeOfType<AgentPartyIdentityLinkRejected>();
        rejected.AgentId.ShouldBe(AgentId);
        rejected.Status.ShouldBe(verdict);

        ApplyAll(state, result);
        state.PartyId.ShouldBeNull(); // AC1/AC2: nothing stored on a rejected link

        // AC2/AC4: with no linked Party, activation still fails closed with MissingPartyIdentity.
        DomainResult activation = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));
        AgentActivationBlockedRejection blocked = activation.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        blocked.Blockers.ShouldContain(AgentActivationBlocker.MissingPartyIdentity);
    }

    [Fact]
    public void Link_with_absent_verdict_fails_closed_to_unknown()
    {
        // A direct-gateway command that never went through the orchestration carries no trusted verdict.
        AgentState state = StateWith(ValidCreate());

        var command = new LinkAgentPartyIdentity(LinkedPartyId);
        DomainResult result = AgentAggregate.Handle(command, state, LinkEnvelope(command, includeValidation: false));

        AgentPartyIdentityLinkRejected rejected = result.Events[0].ShouldBeOfType<AgentPartyIdentityLinkRejected>();
        rejected.Status.ShouldBe(PartyLinkValidationStatus.Unknown);
    }

    [Fact]
    public void Link_with_unparseable_verdict_fails_closed_to_unknown()
    {
        // A garbage extension value must never be trusted as Valid (fail closed).
        AgentState state = StateWith(ValidCreate());
        var command = new LinkAgentPartyIdentity(LinkedPartyId);

        CommandEnvelopeWithRawValidation(command, out CommandEnvelope envelope, "not-a-verdict");
        DomainResult result = AgentAggregate.Handle(command, state, envelope);

        result.Events[0].ShouldBeOfType<AgentPartyIdentityLinkRejected>().Status.ShouldBe(PartyLinkValidationStatus.Unknown);
    }

    [Theory]
    [InlineData("1")]      // numeric underlying value of Valid — Enum.TryParse would resolve it, so it must be rejected
    [InlineData("01")]     // numeric with leading zero
    [InlineData("valid")]  // wrong case — the verdict is matched case-sensitively by its canonical name
    [InlineData(" Valid")] // padded — not an exact name
    public void Link_with_numeric_or_aliased_verdict_fails_closed_to_unknown(string rawVerdict)
    {
        // The verdict is contractually serialized by name; a numeric/aliased form must never be trusted as Valid
        // (the aggregate gate is the documented fail-closed boundary against direct-gateway calls — AC2).
        AgentState state = StateWith(ValidCreate());
        var command = new LinkAgentPartyIdentity(LinkedPartyId);

        CommandEnvelopeWithRawValidation(command, out CommandEnvelope envelope, rawVerdict);
        DomainResult result = AgentAggregate.Handle(command, state, envelope);

        result.Events[0].ShouldBeOfType<AgentPartyIdentityLinkRejected>().Status.ShouldBe(PartyLinkValidationStatus.Unknown);
        ApplyAll(state, result);
        state.PartyId.ShouldBeNull(); // nothing linked on a fail-closed rejection
    }

    // ===== AC3: exactly one identity — idempotent re-link, distinct link rejected, explicit replace =====

    [Fact]
    public void Relink_same_party_id_is_an_idempotent_noop()
    {
        AgentState state = StateWithLinkedParty(ValidCreate());

        var command = new LinkAgentPartyIdentity(LinkedPartyId);
        DomainResult result = AgentAggregate.Handle(command, state, LinkEnvelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Link_a_different_party_when_already_linked_is_rejected_as_already_linked()
    {
        AgentState state = StateWithLinkedParty(ValidCreate());

        var command = new LinkAgentPartyIdentity(OtherPartyId);
        DomainResult result = AgentAggregate.Handle(command, state, LinkEnvelope(command));

        result.IsRejection.ShouldBeTrue();
        AgentPartyIdentityAlreadyLinkedRejection rejection =
            result.Events[0].ShouldBeOfType<AgentPartyIdentityAlreadyLinkedRejection>();
        rejection.AgentId.ShouldBe(AgentId);
        rejection.AttemptedPartyId.ShouldBe(OtherPartyId);

        ApplyAll(state, result);
        state.PartyId.ShouldBe(LinkedPartyId); // the existing link is unchanged
    }

    [Fact]
    public void Replace_with_valid_verdict_swaps_to_the_new_identity_keeping_exactly_one()
    {
        AgentState state = StateWithLinkedParty(ValidCreate());
        int versionBeforeReplace = state.ConfigurationVersion;

        var command = new ReplaceAgentPartyIdentity(OtherPartyId);
        DomainResult result = AgentAggregate.Handle(command, state, LinkEnvelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentPartyIdentityReplaced replaced = result.Events[0].ShouldBeOfType<AgentPartyIdentityReplaced>();
        replaced.PreviousPartyId.ShouldBe(LinkedPartyId);
        replaced.PartyId.ShouldBe(OtherPartyId);
        replaced.ConfigurationVersion.ShouldBe(versionBeforeReplace + 1);

        ApplyAll(state, result);
        state.PartyId.ShouldBe(OtherPartyId); // exactly one active identity, now the replacement
    }

    [Fact]
    public void Replace_with_the_same_id_is_an_idempotent_noop()
    {
        AgentState state = StateWithLinkedParty(ValidCreate());

        var command = new ReplaceAgentPartyIdentity(LinkedPartyId);
        DomainResult result = AgentAggregate.Handle(command, state, LinkEnvelope(command));

        result.IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public void Replace_on_an_unlinked_agent_links_with_a_null_previous_id()
    {
        AgentState state = StateWith(ValidCreate()); // no prior identity

        var command = new ReplaceAgentPartyIdentity(LinkedPartyId);
        DomainResult result = AgentAggregate.Handle(command, state, LinkEnvelope(command));

        AgentPartyIdentityReplaced replaced = result.Events[0].ShouldBeOfType<AgentPartyIdentityReplaced>();
        replaced.PreviousPartyId.ShouldBeNull();
        replaced.PartyId.ShouldBe(LinkedPartyId);

        ApplyAll(state, result);
        state.PartyId.ShouldBe(LinkedPartyId);
    }

    [Fact]
    public void Replace_with_non_valid_verdict_fails_closed_and_keeps_the_existing_link()
    {
        AgentState state = StateWithLinkedParty(ValidCreate());

        var command = new ReplaceAgentPartyIdentity(OtherPartyId);
        DomainResult result = AgentAggregate.Handle(command, state, LinkEnvelope(command, PartyLinkValidationStatus.Unavailable));

        result.Events[0].ShouldBeOfType<AgentPartyIdentityLinkRejected>().Status.ShouldBe(PartyLinkValidationStatus.Unavailable);

        ApplyAll(state, result);
        state.PartyId.ShouldBe(LinkedPartyId); // unchanged
    }

    [Fact]
    public void Replace_with_absent_verdict_fails_closed_to_unknown_and_keeps_the_existing_link()
    {
        // Symmetry with the link path: a direct-gateway replace that never went through the orchestration carries no
        // trusted verdict, so the shared fail-closed gate rejects it as Unknown and leaves the existing link intact.
        AgentState state = StateWithLinkedParty(ValidCreate());

        var command = new ReplaceAgentPartyIdentity(OtherPartyId);
        DomainResult result = AgentAggregate.Handle(command, state, LinkEnvelope(command, includeValidation: false));

        result.Events[0].ShouldBeOfType<AgentPartyIdentityLinkRejected>().Status.ShouldBe(PartyLinkValidationStatus.Unknown);

        ApplyAll(state, result);
        state.PartyId.ShouldBe(LinkedPartyId); // unchanged
    }

    // ===== Not-found and authorization fail closed (AC) =====

    [Fact]
    public void Link_on_a_missing_agent_is_rejected_as_not_found()
    {
        var command = new LinkAgentPartyIdentity(LinkedPartyId);

        DomainResult result = AgentAggregate.Handle(command, state: null, LinkEnvelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentNotFoundRejection>();
    }

    [Fact]
    public void Replace_on_a_missing_agent_is_rejected_as_not_found()
    {
        var command = new ReplaceAgentPartyIdentity(LinkedPartyId);

        DomainResult result = AgentAggregate.Handle(command, state: null, LinkEnvelope(command));

        _ = result.Events[0].ShouldBeOfType<AgentNotFoundRejection>();
    }

    [Fact]
    public void Link_without_agents_admin_is_denied_before_reading_the_verdict()
    {
        AgentState state = StateWith(ValidCreate());
        var command = new LinkAgentPartyIdentity(LinkedPartyId);

        // Even with a forged Valid verdict, the unauthorized caller is denied first.
        DomainResult result = AgentAggregate.Handle(
            command,
            state,
            LinkEnvelope(command, PartyLinkValidationStatus.Valid, isAgentsAdmin: false, actorUserId: "intruder"));

        AgentAdministrationDeniedRejection denied = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
        denied.CommandName.ShouldBe(nameof(LinkAgentPartyIdentity));
        result.Events.ShouldNotContain(e => e is AgentPartyIdentityLinked);
    }

    [Fact]
    public void Replace_without_agents_admin_is_denied()
    {
        AgentState state = StateWithLinkedParty(ValidCreate());
        var command = new ReplaceAgentPartyIdentity(OtherPartyId);

        DomainResult result = AgentAggregate.Handle(command, state, LinkEnvelope(command, isAgentsAdmin: false));

        AgentAdministrationDeniedRejection denied = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
        denied.CommandName.ShouldBe(nameof(ReplaceAgentPartyIdentity));
    }

    // ===== AC4: party identity is a distinct readiness gate that blocks then unblocks activation =====

    [Fact]
    public void Activation_is_blocked_by_missing_party_identity_then_unblocked_after_a_valid_link()
    {
        AgentState state = StateWith(ValidCreate()); // valid display name + instructions, but no party

        // Only the party gate remains — display name and instructions are valid.
        DomainResult blocked = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));
        AgentActivationBlockedRejection rejection = blocked.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        rejection.Blockers.ShouldBe([AgentActivationBlocker.MissingPartyIdentity]);

        // Link a valid Party, then activation succeeds (other gates already pass).
        var link = new LinkAgentPartyIdentity(LinkedPartyId);
        ApplyAll(state, AgentAggregate.Handle(link, state, LinkEnvelope(link)));

        DomainResult activated = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));
        activated.IsSuccess.ShouldBeTrue();
        _ = activated.Events[0].ShouldBeOfType<AgentActivated>();
    }

    [Fact]
    public void Activation_reports_the_party_gate_last_in_the_documented_deterministic_order()
    {
        // When every gate fails, the blockers must be reported in the policy's documented order — display name,
        // then instructions, then party identity (the 1.4 gate is appended last, never interleaved).
        AgentState state = StateWith(ValidCreate(displayName: "", instructions: "")); // all three gates fail, no party

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));

        AgentActivationBlockedRejection blocked = result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        blocked.Blockers.ShouldBe([
            AgentActivationBlocker.MissingDisplayName,
            AgentActivationBlocker.MissingInstructions,
            AgentActivationBlocker.MissingPartyIdentity,
        ]);
    }

    // ===== Full reflection dispatch + JSON payload round-trip via ProcessAsync (E2E journey) =====

    [Fact]
    public async Task ProcessAndApply_link_then_activate_then_replace_threads_state_and_keeps_one_identity()
    {
        var aggregate = new AgentAggregate();
        var state = new AgentState();

        // Create a valid draft — activation is blocked by the party gate until a Party is linked (AC4).
        (await ProcessAndApplyAsync(aggregate, state, ValidCreate())).IsSuccess.ShouldBeTrue();
        (await ProcessAndApplyAsync(aggregate, state, new ActivateAgent()))
            .Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldContain(AgentActivationBlocker.MissingPartyIdentity);

        // Link a valid Party through the full pipeline (trusted verdict in the envelope).
        var link = new LinkAgentPartyIdentity(LinkedPartyId);
        (await ProcessAndApplyAsync(aggregate, state, link, LinkEnvelope(link))).IsSuccess.ShouldBeTrue();
        state.PartyId.ShouldBe(LinkedPartyId);

        // Now activation succeeds, and the party presence is visible through the read path (AC4).
        (await ProcessAndApplyAsync(aggregate, state, new ActivateAgent())).IsSuccess.ShouldBeTrue();
        AgentStatusView active = AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull();
        active.Lifecycle.ShouldBe(AgentLifecycleStatus.Active);
        active.HasPartyIdentity.ShouldBeTrue();
        active.ActivationBlockers.ShouldBeEmpty();

        // Replace keeps exactly one identity.
        var replace = new ReplaceAgentPartyIdentity(OtherPartyId);
        (await ProcessAndApplyAsync(aggregate, state, replace, LinkEnvelope(replace))).IsSuccess.ShouldBeTrue();
        state.PartyId.ShouldBe(OtherPartyId);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Active); // replacement does not change lifecycle
    }

    // Builds a link envelope whose party:linkValidation extension holds an arbitrary raw string (to test
    // fail-closed parsing of an unrecognized verdict value).
    private static void CommandEnvelopeWithRawValidation(
        LinkAgentPartyIdentity command,
        out CommandEnvelope envelope,
        string rawValidationValue)
    {
        envelope = new(
            "msg-raw",
            TenantId,
            "agent",
            AgentId,
            nameof(LinkAgentPartyIdentity),
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-raw",
            null,
            "admin-user",
            new System.Collections.Generic.Dictionary<string, string>
            {
                [AgentAdminExtensionKey] = "true",
                [PartyLinkValidationExtensionKey] = rawValidationValue,
            });
    }
}
