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
/// Handle-method tests for the Content Safety Policy configuration behaviour added to <see cref="AgentAggregate"/>
/// (Story 1.7 AC1, AC3). Covers: recording the active policy and bumping both the content-safety policy and
/// configuration versions while lifecycle stays unchanged, recording a stricter mode-specific override, the
/// structural rejections (null config/active policy, unknown failure handling/audit treatment, an empty policy with
/// no constraint/category, an invalid mode override), list normalization (trim/dedupe/blank-drop), idempotent
/// identical-configuration no-op, a future-only policy change, authorization / not-found fail-closed behaviour, and
/// replay through <c>Apply</c>.
/// </summary>
public sealed class AgentContentSafetyPolicyTests
{
    private static readonly AgentContentSafetyPolicy _strictConfirmationOverride = new(
        ["No system-prompt disclosure", "No internal tooling details"],
        ["self-harm", "weapons"],
        ["medical-advice"],
        ContentSafetyFailureHandling.BlockWithAuditableOverride,
        ContentSafetyAuditTreatment.RedactedExcerpt);

    private static ConfigureAgentContentSafetyPolicy ConfigureCommand(AgentContentSafetyConfiguration? configuration = null)
        => new(configuration ?? SampleContentSafetyConfiguration);

    // ===== AC1: configure success records the active policy, bumps both versions, lifecycle unchanged =====

    [Fact]
    public void Configure_policy_records_active_policy_and_bumps_both_versions()
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1, no content-safety policy

        ConfigureAgentContentSafetyPolicy command = ConfigureCommand();
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentContentSafetyPolicyConfigured configured = result.Events[0].ShouldBeOfType<AgentContentSafetyPolicyConfigured>();
        configured.AgentId.ShouldBe(AgentId);
        configured.Configuration.ActivePolicy.PromptConstraints.ShouldBe(["No system-prompt disclosure"]);
        configured.Configuration.ActivePolicy.BlockedOutputCategories.ShouldBe(["self-harm"]);
        configured.Configuration.ActivePolicy.FailureHandling.ShouldBe(ContentSafetyFailureHandling.BlockAndAudit);
        configured.Configuration.ActivePolicy.AuditTreatment.ShouldBe(ContentSafetyAuditTreatment.MetadataOnly);
        configured.Configuration.AutomaticModePolicy.ShouldBeNull();
        configured.Configuration.ConfirmationModePolicy.ShouldBeNull();
        configured.ContentSafetyPolicyVersion.ShouldBe(1);
        configured.ConfigurationVersion.ShouldBe(2);

        ApplyAll(state, result);
        state.ContentSafety.ShouldNotBeNull();
        state.ContentSafetyPolicyVersion.ShouldBe(1);
        state.ConfigurationVersion.ShouldBe(2);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft); // configuring a policy never changes lifecycle
    }

    // ===== AC3: a stricter mode-specific override is recorded and surfaced =====

    [Fact]
    public void Configure_with_a_confirmation_mode_override_records_the_override()
    {
        AgentState state = StateWith(ValidCreate());

        var configuration = new AgentContentSafetyConfiguration(SampleContentSafetyPolicy, null, _strictConfirmationOverride);
        ConfigureAgentContentSafetyPolicy command = ConfigureCommand(configuration);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentContentSafetyPolicyConfigured configured = result.Events[0].ShouldBeOfType<AgentContentSafetyPolicyConfigured>();
        configured.Configuration.AutomaticModePolicy.ShouldBeNull();
        configured.Configuration.ConfirmationModePolicy.ShouldNotBeNull()
            .FailureHandling.ShouldBe(ContentSafetyFailureHandling.BlockWithAuditableOverride);

        // AC3: the status view surfaces WHICH mode carries an override, without exposing the policy content.
        ApplyAll(state, result);
        AgentStatusView view = AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull();
        view.HasContentSafetyPolicy.ShouldBeTrue();
        view.HasAutomaticContentSafetyOverride.ShouldBeFalse();
        view.HasConfirmationContentSafetyOverride.ShouldBeTrue();
    }

    // ===== AC1/AC2: structural rejections → InvalidAgentConfigurationRejection (no value echoed) =====

    public static TheoryData<AgentContentSafetyConfiguration> StructurallyInvalidConfigurations() =>
    [
        // Unknown failure handling on the active policy.
        new AgentContentSafetyConfiguration(
            new AgentContentSafetyPolicy(["a"], [], [], ContentSafetyFailureHandling.Unknown, ContentSafetyAuditTreatment.MetadataOnly),
            null,
            null),
        // Unknown audit treatment on the active policy.
        new AgentContentSafetyConfiguration(
            new AgentContentSafetyPolicy(["a"], [], [], ContentSafetyFailureHandling.BlockAndAudit, ContentSafetyAuditTreatment.Unknown),
            null,
            null),
        // Empty active policy — no prompt constraint and no output category.
        new AgentContentSafetyConfiguration(
            new AgentContentSafetyPolicy([], [], [], ContentSafetyFailureHandling.BlockAndAudit, ContentSafetyAuditTreatment.MetadataOnly),
            null,
            null),
        // A valid active policy but an invalid Automatic-mode override (unknown failure handling).
        new AgentContentSafetyConfiguration(
            new AgentContentSafetyPolicy(["a"], [], [], ContentSafetyFailureHandling.BlockAndAudit, ContentSafetyAuditTreatment.MetadataOnly),
            new AgentContentSafetyPolicy(["b"], [], [], ContentSafetyFailureHandling.Unknown, ContentSafetyAuditTreatment.MetadataOnly),
            null),
        // A valid active policy but an empty Confirmation-mode override.
        new AgentContentSafetyConfiguration(
            new AgentContentSafetyPolicy(["a"], [], [], ContentSafetyFailureHandling.BlockAndAudit, ContentSafetyAuditTreatment.MetadataOnly),
            null,
            new AgentContentSafetyPolicy([], [], [], ContentSafetyFailureHandling.BlockAndAudit, ContentSafetyAuditTreatment.MetadataOnly)),
    ];

    [Theory]
    [MemberData(nameof(StructurallyInvalidConfigurations))]
    public void Configure_structurally_invalid_configuration_is_rejected_and_records_nothing(AgentContentSafetyConfiguration configuration)
    {
        AgentState state = StateWith(ValidCreate());

        ConfigureAgentContentSafetyPolicy command = ConfigureCommand(configuration);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();

        ApplyAll(state, result);
        state.ContentSafety.ShouldBeNull(); // nothing recorded on a rejected configuration
    }

    [Fact]
    public void Configure_null_configuration_is_rejected_as_invalid_configuration()
    {
        // A bare command can carry a null Configuration (e.g. deserialized as {"Configuration":null}); the aggregate
        // must reject it structurally rather than NRE — the "Content safety configuration is required." guard.
        AgentState state = StateWith(ValidCreate());

        var command = new ConfigureAgentContentSafetyPolicy(null!);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();

        ApplyAll(state, result);
        state.ContentSafety.ShouldBeNull();
    }

    [Fact]
    public void Configure_null_active_policy_is_rejected_as_invalid_configuration()
    {
        AgentState state = StateWith(ValidCreate());

        var command = new ConfigureAgentContentSafetyPolicy(new AgentContentSafetyConfiguration(null!, null, null));
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();
    }

    // ===== AC1: list normalization (trim, drop blanks, ordinal de-duplicate) =====

    [Fact]
    public void Configure_normalizes_lists_by_trimming_dropping_blanks_and_deduplicating()
    {
        AgentState state = StateWith(ValidCreate());

        var policy = new AgentContentSafetyPolicy(
            ["  keep me  ", "keep me", "   ", null!, "second"],
            ["self-harm", "self-harm"],
            [],
            ContentSafetyFailureHandling.BlockAndAudit,
            ContentSafetyAuditTreatment.MetadataOnly);
        var command = new ConfigureAgentContentSafetyPolicy(new AgentContentSafetyConfiguration(policy, null, null));
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentContentSafetyPolicy stored = result.Events[0].ShouldBeOfType<AgentContentSafetyPolicyConfigured>().Configuration.ActivePolicy;
        stored.PromptConstraints.ShouldBe(["keep me", "second"]); // trimmed, blanks dropped, duplicate collapsed
        stored.BlockedOutputCategories.ShouldBe(["self-harm"]); // duplicate collapsed
    }

    // ===== AC1 / AD-13: idempotent identical configuration, changed policy bumps version + prior preserved =====

    [Fact]
    public void Reconfigure_identical_configuration_is_an_idempotent_noop()
    {
        AgentState state = StateWithContentSafety(ValidCreate());

        ConfigureAgentContentSafetyPolicy command = ConfigureCommand();
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Changing_the_policy_emits_a_new_event_bumps_content_safety_version_and_preserves_prior_events()
    {
        AgentState state = StateWith(ValidCreate());

        // First configuration.
        ConfigureAgentContentSafetyPolicy first = ConfigureCommand();
        DomainResult firstResult = AgentAggregate.Handle(first, state, Envelope(first));
        AgentContentSafetyPolicyConfigured firstEvent = firstResult.Events[0].ShouldBeOfType<AgentContentSafetyPolicyConfigured>();
        ApplyAll(state, firstResult);

        // Change the failure handling (a genuine change).
        var changedPolicy = SampleContentSafetyPolicy with { FailureHandling = ContentSafetyFailureHandling.BlockWithAuditableOverride };
        var changed = new ConfigureAgentContentSafetyPolicy(new AgentContentSafetyConfiguration(changedPolicy, null, null));
        DomainResult changedResult = AgentAggregate.Handle(changed, state, Envelope(changed));

        changedResult.IsSuccess.ShouldBeTrue();
        AgentContentSafetyPolicyConfigured changedEvent = changedResult.Events[0].ShouldBeOfType<AgentContentSafetyPolicyConfigured>();
        changedEvent.Configuration.ActivePolicy.FailureHandling.ShouldBe(ContentSafetyFailureHandling.BlockWithAuditableOverride);
        changedEvent.ContentSafetyPolicyVersion.ShouldBe(2); // bumped from 1
        changedEvent.ConfigurationVersion.ShouldBe(firstEvent.ConfigurationVersion + 1);

        // AC1: the prior append-only event is unchanged — a changed policy never rewrites it (future-only).
        firstEvent.Configuration.ActivePolicy.FailureHandling.ShouldBe(ContentSafetyFailureHandling.BlockAndAudit);
        firstEvent.ContentSafetyPolicyVersion.ShouldBe(1);

        ApplyAll(state, changedResult);
        state.ContentSafety.ShouldNotBeNull().ActivePolicy.FailureHandling.ShouldBe(ContentSafetyFailureHandling.BlockWithAuditableOverride);
        state.ContentSafetyPolicyVersion.ShouldBe(2);
    }

    // ===== Not-found and authorization fail closed =====

    [Fact]
    public void Configure_on_a_missing_agent_is_rejected_as_not_found()
    {
        ConfigureAgentContentSafetyPolicy command = ConfigureCommand();

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentNotFoundRejection>();
    }

    [Fact]
    public void Configure_without_agents_admin_is_denied()
    {
        AgentState state = StateWith(ValidCreate());
        ConfigureAgentContentSafetyPolicy command = ConfigureCommand();

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command, isAgentsAdmin: false, actorUserId: "intruder"));

        AgentAdministrationDeniedRejection denied = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
        denied.CommandName.ShouldBe(nameof(ConfigureAgentContentSafetyPolicy));
        result.Events.ShouldNotContain(e => e is AgentContentSafetyPolicyConfigured);
    }

    // ===== Replay / rehydration through Apply =====

    [Fact]
    public void Apply_policy_change_tracks_the_single_recorded_configuration_and_bumps_versions()
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1

        state.Apply(new AgentContentSafetyPolicyConfigured(AgentId, SampleContentSafetyConfiguration, ContentSafetyPolicyVersion: 1, ConfigurationVersion: 2));
        state.ContentSafety.ShouldNotBeNull();
        state.ContentSafetyPolicyVersion.ShouldBe(1);
        state.ConfigurationVersion.ShouldBe(2);

        var changed = SampleContentSafetyConfiguration with { ConfirmationModePolicy = _strictConfirmationOverride };
        state.Apply(new AgentContentSafetyPolicyConfigured(AgentId, changed, ContentSafetyPolicyVersion: 2, ConfigurationVersion: 3));
        state.ContentSafety.ShouldNotBeNull().ConfirmationModePolicy.ShouldNotBeNull();
        state.ContentSafetyPolicyVersion.ShouldBe(2);
        state.ConfigurationVersion.ShouldBe(3);
    }

    [Fact]
    public void Apply_policy_before_create_is_ignored()
    {
        var state = new AgentState();

        state.Apply(new AgentContentSafetyPolicyConfigured(AgentId, SampleContentSafetyConfiguration, ContentSafetyPolicyVersion: 1, ConfigurationVersion: 2));

        state.ContentSafety.ShouldBeNull();
        state.IsCreated.ShouldBeFalse();
    }

    // ===== QA gap-fill (AC2; AD-14): the structural rejection reason never echoes configured policy content =====

    [Fact]
    public void Configure_invalid_policy_rejection_reason_never_echoes_the_configured_policy_content()
    {
        // AD-14 / AC2: a structural rejection reason is a SAFE classification only — it must never leak any configured
        // descriptor/category text (the policy content kept off every non-durable surface: rejection, status, logs).
        const string secretLikeConstraint = "DO-NOT-LEAK-prompt-constraint-x1y2z3";
        const string secretLikeCategory = "DO-NOT-LEAK-blocked-category-x1y2z3";
        AgentState state = StateWith(ValidCreate());

        // A valid active policy but an invalid Automatic-mode override (Unknown failure handling) whose lists carry
        // recognisable tokens — the rejection must classify the failure without echoing any of that content.
        var invalid = new AgentContentSafetyConfiguration(
            SampleContentSafetyPolicy,
            new AgentContentSafetyPolicy(
                [secretLikeConstraint],
                [secretLikeCategory],
                [],
                ContentSafetyFailureHandling.Unknown,
                ContentSafetyAuditTreatment.MetadataOnly),
            null);
        var command = new ConfigureAgentContentSafetyPolicy(invalid);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        InvalidAgentConfigurationRejection rejection = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();
        rejection.Reason.ShouldNotContain(secretLikeConstraint);
        rejection.Reason.ShouldNotContain(secretLikeCategory);
    }

    // ===== QA gap-fill (AC3): an Automatic-mode override is recorded and surfaced symmetrically to Confirmation =====

    [Fact]
    public void Configure_with_an_automatic_mode_override_surfaces_only_the_automatic_override()
    {
        AgentState state = StateWith(ValidCreate());

        var configuration = new AgentContentSafetyConfiguration(SampleContentSafetyPolicy, _strictConfirmationOverride, null);
        ConfigureAgentContentSafetyPolicy command = ConfigureCommand(configuration);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<AgentContentSafetyPolicyConfigured>()
            .Configuration.AutomaticModePolicy.ShouldNotBeNull();

        // AC3: the status view surfaces WHICH mode carries an override, without exposing the policy content.
        ApplyAll(state, result);
        AgentStatusView view = AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull();
        view.HasAutomaticContentSafetyOverride.ShouldBeTrue();
        view.HasConfirmationContentSafetyOverride.ShouldBeFalse();
    }

    // ===== QA gap-fill (AD-13): mode overrides participate in the by-value idempotency comparison =====

    [Fact]
    public void Reconfigure_identical_configuration_with_a_mode_override_is_an_idempotent_noop()
    {
        var withOverride = new AgentContentSafetyConfiguration(SampleContentSafetyPolicy, null, _strictConfirmationOverride);
        AgentState state = StateWithContentSafety(ValidCreate(), withOverride);

        var command = new ConfigureAgentContentSafetyPolicy(withOverride);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Adding_a_mode_override_to_an_otherwise_identical_configuration_is_a_genuine_change()
    {
        // The active policy is unchanged, but adding a Confirmation-mode override is a real change (the override
        // participates in by-value equality) — it must emit a new event and bump the version, never no-op.
        AgentState state = StateWithContentSafety(ValidCreate()); // active only, no overrides; ContentSafetyPolicyVersion = 1

        var withOverride = SampleContentSafetyConfiguration with { ConfirmationModePolicy = _strictConfirmationOverride };
        var command = new ConfigureAgentContentSafetyPolicy(withOverride);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentContentSafetyPolicyConfigured configured = result.Events[0].ShouldBeOfType<AgentContentSafetyPolicyConfigured>();
        configured.Configuration.ConfirmationModePolicy.ShouldNotBeNull();
        configured.ContentSafetyPolicyVersion.ShouldBe(2);
    }

    // ===== QA gap-fill (AD-13): normalization runs before the idempotency compare =====

    [Fact]
    public void Reconfigure_with_blank_and_duplicate_entries_that_normalize_equal_is_an_idempotent_noop()
    {
        // The recorded policy is the normalized SampleContentSafetyPolicy; re-submitting the same content padded with
        // whitespace/blanks/duplicates normalizes to the identical policy → no-op (normalization precedes the by-value
        // idempotency comparison, so a cosmetically-different-but-equivalent re-assert never appends a duplicate event).
        AgentState state = StateWithContentSafety(ValidCreate());

        var noisy = new AgentContentSafetyPolicy(
            ["  No system-prompt disclosure  ", "No system-prompt disclosure", "   ", null!],
            ["self-harm", "self-harm"],
            [],
            ContentSafetyFailureHandling.BlockAndAudit,
            ContentSafetyAuditTreatment.MetadataOnly);
        var command = new ConfigureAgentContentSafetyPolicy(new AgentContentSafetyConfiguration(noisy, null, null));
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    // ===== QA gap-fill (AC1): the "at least one constraint OR category" rule is met by a category alone =====

    [Fact]
    public void Configure_policy_with_only_restricted_categories_is_valid_and_normalizes_the_restricted_list()
    {
        // A policy with NO prompt constraints and NO blocked categories but ≥1 restricted category satisfies the
        // activation-gate minimum (the OR branch). The restricted list is trimmed/de-duplicated like the others.
        AgentState state = StateWith(ValidCreate());

        var policy = new AgentContentSafetyPolicy(
            [],
            [],
            ["  medical-advice  ", "medical-advice", "financial-advice"],
            ContentSafetyFailureHandling.BlockAndAudit,
            ContentSafetyAuditTreatment.MetadataOnly);
        var command = new ConfigureAgentContentSafetyPolicy(new AgentContentSafetyConfiguration(policy, null, null));
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentContentSafetyPolicy stored = result.Events[0].ShouldBeOfType<AgentContentSafetyPolicyConfigured>().Configuration.ActivePolicy;
        stored.PromptConstraints.ShouldBeEmpty();
        stored.BlockedOutputCategories.ShouldBeEmpty();
        stored.RestrictedOutputCategories.ShouldBe(["medical-advice", "financial-advice"]); // trimmed + de-duplicated
    }
}
