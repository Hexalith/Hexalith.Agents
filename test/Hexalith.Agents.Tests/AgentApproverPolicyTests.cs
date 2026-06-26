using System.Threading.Tasks;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Handle-method tests for the Approver Policy configuration behaviour added to <see cref="AgentAggregate"/>
/// (Story 1.6 AC2, AC4). Covers: recording the safe sources + disclosure and bumping both the approver-policy and
/// configuration versions while lifecycle stays unchanged, the structural rejections (shape mismatches, unknown
/// kind/disclosure, duplicate source), an empty policy being storable, idempotent identical-policy no-op, a
/// future-only policy change, authorization / not-found fail-closed behaviour, and replay through <c>Apply</c>.
/// </summary>
public sealed class AgentApproverPolicyTests
{
    private static ConfigureAgentApproverPolicy ConfigureCommand(AgentApproverPolicy? policy = null)
        => new(policy ?? SampleApproverPolicy);

    // ===== AC2/AC4: configure success records the policy, bumps both versions, lifecycle unchanged =====

    [Fact]
    public void Configure_policy_records_sources_disclosure_and_bumps_both_versions()
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1, no policy

        ConfigureAgentApproverPolicy command = ConfigureCommand();
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentApproverPolicyConfigured configured = result.Events[0].ShouldBeOfType<AgentApproverPolicyConfigured>();
        configured.AgentId.ShouldBe(AgentId);
        configured.Policy.Sources.Count.ShouldBe(3);
        configured.Policy.DisclosureCategory.ShouldBe(ApproverPolicyBasisDisclosure.OperatorOnly);
        configured.ApproverPolicyVersion.ShouldBe(1);
        configured.ConfigurationVersion.ShouldBe(2);

        ApplyAll(state, result);
        state.ApproverPolicySources.ShouldNotBeNull().Count.ShouldBe(3);
        state.ApproverPolicyDisclosure.ShouldBe(ApproverPolicyBasisDisclosure.OperatorOnly);
        state.ApproverPolicyVersion.ShouldBe(1);
        state.ConfigurationVersion.ShouldBe(2);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft); // configuring a policy never changes lifecycle
    }

    [Fact]
    public void Configure_empty_policy_is_structurally_storable()
    {
        AgentState state = StateWith(ValidCreate());

        ConfigureAgentApproverPolicy command = ConfigureCommand(new AgentApproverPolicy([], ApproverPolicyBasisDisclosure.UserVisible));
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentApproverPolicyConfigured configured = result.Events[0].ShouldBeOfType<AgentApproverPolicyConfigured>();
        configured.Policy.Sources.ShouldBeEmpty();
        configured.ApproverPolicyVersion.ShouldBe(1);

        ApplyAll(state, result);
        state.ApproverPolicySources.ShouldNotBeNull().ShouldBeEmpty(); // an empty policy is recorded (won't satisfy confirmation readiness)
    }

    // ===== AC2: structural rejections → InvalidAgentConfigurationRejection (no value echoed) =====

    public static TheoryData<AgentApproverPolicy> StructurallyInvalidPolicies() =>
    [
        // PredefinedParty without a PartyId.
        new AgentApproverPolicy([new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, null, null)], ApproverPolicyBasisDisclosure.OperatorOnly),
        // PredefinedParty carrying a TenantRole it must not.
        new AgentApproverPolicy([new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, ApproverPartyId, ApproverTenantRole)], ApproverPolicyBasisDisclosure.OperatorOnly),
        // TenantRole without a role.
        new AgentApproverPolicy([new ApproverPolicySource(ApproverPolicySourceKind.TenantRole, null, null)], ApproverPolicyBasisDisclosure.OperatorOnly),
        // TenantRole carrying a PartyId it must not.
        new AgentApproverPolicy([new ApproverPolicySource(ApproverPolicySourceKind.TenantRole, ApproverPartyId, ApproverTenantRole)], ApproverPolicyBasisDisclosure.OperatorOnly),
        // Caller carrying a PartyId it must not.
        new AgentApproverPolicy([new ApproverPolicySource(ApproverPolicySourceKind.Caller, ApproverPartyId, null)], ApproverPolicyBasisDisclosure.OperatorOnly),
        // ConversationOwner carrying a TenantRole it must not.
        new AgentApproverPolicy([new ApproverPolicySource(ApproverPolicySourceKind.ConversationOwner, null, ApproverTenantRole)], ApproverPolicyBasisDisclosure.OperatorOnly),
        // Unknown source kind.
        new AgentApproverPolicy([new ApproverPolicySource(ApproverPolicySourceKind.Unknown, null, null)], ApproverPolicyBasisDisclosure.OperatorOnly),
        // Unknown disclosure category.
        new AgentApproverPolicy([new ApproverPolicySource(ApproverPolicySourceKind.Caller, null, null)], ApproverPolicyBasisDisclosure.Unknown),
        // Duplicate source.
        new AgentApproverPolicy(
            [
                new ApproverPolicySource(ApproverPolicySourceKind.Caller, null, null),
                new ApproverPolicySource(ApproverPolicySourceKind.Caller, null, null),
            ],
            ApproverPolicyBasisDisclosure.OperatorOnly),
    ];

    [Theory]
    [MemberData(nameof(StructurallyInvalidPolicies))]
    public void Configure_structurally_invalid_policy_is_rejected_and_records_nothing(AgentApproverPolicy policy)
    {
        AgentState state = StateWith(ValidCreate());

        ConfigureAgentApproverPolicy command = ConfigureCommand(policy);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();

        ApplyAll(state, result);
        state.ApproverPolicySources.ShouldBeNull(); // nothing recorded on a rejected configuration
    }

    // ===== QA gap-fill (AC2): null policy, normalization contract, and dedup-after-normalization =====

    [Fact]
    public void Configure_null_policy_is_rejected_as_invalid_configuration()
    {
        // A bare ConfigureAgentApproverPolicy can carry a null Policy (e.g. deserialized as {"Policy":null}); the
        // aggregate must reject it structurally rather than NRE — the "Approver policy is required." guard.
        AgentState state = StateWith(ValidCreate());

        var command = new ConfigureAgentApproverPolicy(null!);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();

        ApplyAll(state, result);
        state.ApproverPolicySources.ShouldBeNull();
    }

    [Fact]
    public void Configure_caller_source_with_whitespace_reference_normalizes_it_away_and_stores_the_policy()
    {
        // A Caller source carrying a whitespace-only PartyId is structurally storable: normalization blanks the
        // ignored field to null (it does NOT carry a real reference), so the recorded source is Caller(null, null).
        AgentState state = StateWith(ValidCreate());

        var policy = new AgentApproverPolicy(
            [new ApproverPolicySource(ApproverPolicySourceKind.Caller, "   ", null)],
            ApproverPolicyBasisDisclosure.OperatorOnly);
        DomainResult result = AgentAggregate.Handle(ConfigureCommand(policy), state, Envelope(ConfigureCommand(policy)));

        result.IsSuccess.ShouldBeTrue();
        ApproverPolicySource stored = result.Events[0].ShouldBeOfType<AgentApproverPolicyConfigured>().Policy.Sources[0];
        stored.Kind.ShouldBe(ApproverPolicySourceKind.Caller);
        stored.PartyId.ShouldBeNull(); // the whitespace reference was normalized away
    }

    [Fact]
    public void Configure_predefined_party_with_a_whitespace_party_id_is_rejected_after_normalization()
    {
        // The symmetric case: a PredefinedParty's PartyId is REQUIRED, and a whitespace-only value normalizes to null,
        // so the policy is rejected exactly like a literal-null PartyId.
        AgentState state = StateWith(ValidCreate());

        var policy = new AgentApproverPolicy(
            [new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, "   ", null)],
            ApproverPolicyBasisDisclosure.OperatorOnly);
        DomainResult result = AgentAggregate.Handle(ConfigureCommand(policy), state, Envelope(ConfigureCommand(policy)));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();

        ApplyAll(state, result);
        state.ApproverPolicySources.ShouldBeNull();
    }

    [Fact]
    public void Configure_sources_equal_only_after_normalization_are_rejected_as_duplicates()
    {
        // Two Caller sources that differ only by a blank-vs-null ignored field collapse to the same normalized source,
        // so duplicate detection (which runs on the normalized form) must reject the policy.
        AgentState state = StateWith(ValidCreate());

        var policy = new AgentApproverPolicy(
            [
                new ApproverPolicySource(ApproverPolicySourceKind.Caller, "   ", null),
                new ApproverPolicySource(ApproverPolicySourceKind.Caller, null, null),
            ],
            ApproverPolicyBasisDisclosure.OperatorOnly);
        DomainResult result = AgentAggregate.Handle(ConfigureCommand(policy), state, Envelope(ConfigureCommand(policy)));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();
    }

    // ===== AC4 / AD-13: idempotent identical policy, changed policy bumps version + prior preserved =====

    [Fact]
    public void Reconfigure_identical_policy_is_an_idempotent_noop()
    {
        AgentState state = StateWithApproverPolicy(ValidCreate());

        ConfigureAgentApproverPolicy command = ConfigureCommand();
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Changing_the_policy_emits_a_new_event_bumps_approver_version_and_preserves_prior_events()
    {
        AgentState state = StateWith(ValidCreate());

        // First policy.
        ConfigureAgentApproverPolicy first = ConfigureCommand();
        DomainResult firstResult = AgentAggregate.Handle(first, state, Envelope(first));
        AgentApproverPolicyConfigured firstEvent = firstResult.Events[0].ShouldBeOfType<AgentApproverPolicyConfigured>();
        ApplyAll(state, firstResult);

        // Change the disclosure category (a genuine change).
        var changedPolicy = SampleApproverPolicy with { DisclosureCategory = ApproverPolicyBasisDisclosure.Redacted };
        DomainResult changedResult = AgentAggregate.Handle(ConfigureCommand(changedPolicy), state, Envelope(ConfigureCommand(changedPolicy)));

        changedResult.IsSuccess.ShouldBeTrue();
        AgentApproverPolicyConfigured changedEvent = changedResult.Events[0].ShouldBeOfType<AgentApproverPolicyConfigured>();
        changedEvent.Policy.DisclosureCategory.ShouldBe(ApproverPolicyBasisDisclosure.Redacted);
        changedEvent.ApproverPolicyVersion.ShouldBe(2); // bumped from 1
        changedEvent.ConfigurationVersion.ShouldBe(firstEvent.ConfigurationVersion + 1);

        // AC4: the prior append-only event is unchanged — a changed policy never rewrites it (future-only).
        firstEvent.Policy.DisclosureCategory.ShouldBe(ApproverPolicyBasisDisclosure.OperatorOnly);
        firstEvent.ApproverPolicyVersion.ShouldBe(1);

        ApplyAll(state, changedResult);
        state.ApproverPolicyDisclosure.ShouldBe(ApproverPolicyBasisDisclosure.Redacted);
        state.ApproverPolicyVersion.ShouldBe(2);
    }

    // ===== Not-found and authorization fail closed =====

    [Fact]
    public void Configure_on_a_missing_agent_is_rejected_as_not_found()
    {
        ConfigureAgentApproverPolicy command = ConfigureCommand();

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentNotFoundRejection>();
    }

    [Fact]
    public void Configure_without_agents_admin_is_denied()
    {
        AgentState state = StateWith(ValidCreate());
        ConfigureAgentApproverPolicy command = ConfigureCommand();

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command, isAgentsAdmin: false, actorUserId: "intruder"));

        AgentAdministrationDeniedRejection denied = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
        denied.CommandName.ShouldBe(nameof(ConfigureAgentApproverPolicy));
        result.Events.ShouldNotContain(e => e is AgentApproverPolicyConfigured);
    }

    // ===== Replay / rehydration through Apply =====

    [Fact]
    public void Apply_policy_change_tracks_the_single_recorded_policy_and_bumps_versions()
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1

        state.Apply(new AgentApproverPolicyConfigured(AgentId, SampleApproverPolicy, ApproverPolicyVersion: 1, ConfigurationVersion: 2));
        state.ApproverPolicySources.ShouldNotBeNull().Count.ShouldBe(3);
        state.ApproverPolicyVersion.ShouldBe(1);
        state.ConfigurationVersion.ShouldBe(2);

        var changed = SampleApproverPolicy with { DisclosureCategory = ApproverPolicyBasisDisclosure.Omitted };
        state.Apply(new AgentApproverPolicyConfigured(AgentId, changed, ApproverPolicyVersion: 2, ConfigurationVersion: 3));
        state.ApproverPolicyDisclosure.ShouldBe(ApproverPolicyBasisDisclosure.Omitted);
        state.ApproverPolicyVersion.ShouldBe(2);
        state.ConfigurationVersion.ShouldBe(3);
    }

    [Fact]
    public void Apply_policy_before_create_is_ignored()
    {
        var state = new AgentState();

        state.Apply(new AgentApproverPolicyConfigured(AgentId, SampleApproverPolicy, ApproverPolicyVersion: 1, ConfigurationVersion: 2));

        state.ApproverPolicySources.ShouldBeNull();
        state.IsCreated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAndApply_configure_then_reconfigure_threads_state_and_surfaces_presence()
    {
        var aggregate = new AgentAggregate();
        var state = new AgentState();

        (await ProcessAndApplyAsync(aggregate, state, ValidCreate())).IsSuccess.ShouldBeTrue();

        (await ProcessAndApplyAsync(aggregate, state, ConfigureCommand())).IsSuccess.ShouldBeTrue();
        state.ApproverPolicyVersion.ShouldBe(1);

        // The policy presence + disclosure surface through the read path without exposing the source list (AC4).
        AgentStatusView view = AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull();
        view.HasApproverPolicy.ShouldBeTrue();
        view.ApproverPolicyDisclosure.ShouldBe(ApproverPolicyBasisDisclosure.OperatorOnly);
        view.ApproverPolicyVersion.ShouldBe(1);

        // Re-asserting the identical policy is an idempotent no-op.
        (await ProcessAndApplyAsync(aggregate, state, ConfigureCommand())).IsNoOp.ShouldBeTrue();
    }
}
