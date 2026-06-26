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
/// Handle-method tests for the Story 4.4 launch-readiness record-and-gate behaviour on <see cref="AgentAggregate"/>
/// (AC1, AC2, AC3, AC4). Covers: recording a readiness decision and bumping both the launch-readiness and configuration
/// versions while lifecycle stays unchanged; structural rejections (incomplete metric, malformed latency target,
/// unknown cost posture); idempotent re-assert no-op; the production-like enablement gate as a pure state check (no
/// envelope verdict beyond the trusted audit-governance flag can affirmatively enable, and a missing flag fails closed);
/// the deterministic blocker order; authorization / not-found fail-closed behaviour; and the launch-readiness gate being
/// distinct from the baseline activation gate.
/// </summary>
public sealed class AgentLaunchReadinessTests
{
    private static RecordAgentLaunchReadiness RecordCommand(AgentLaunchReadiness? readiness = null)
        => new(readiness ?? SampleLaunchReadiness);

    // ===== AC1/AC2/AC3: recording success records the decision, bumps both versions, lifecycle unchanged =====

    [Fact]
    public void Record_readiness_records_the_decision_and_bumps_both_versions()
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1, no readiness

        RecordAgentLaunchReadiness command = RecordCommand();
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentLaunchReadinessRecorded recorded = result.Events[0].ShouldBeOfType<AgentLaunchReadinessRecorded>();
        recorded.AgentId.ShouldBe(AgentId);
        recorded.Readiness.Metrics.Count.ShouldBe(2);
        recorded.Readiness.CostPosture.ShouldBe(CostControlPosture.Budgets);
        recorded.Readiness.ContextPolicyReference.ShouldBe("full-conversation-v1");
        recorded.LaunchReadinessVersion.ShouldBe(1);
        recorded.ConfigurationVersion.ShouldBe(2);

        ApplyAll(state, result);
        state.LaunchReadiness.ShouldNotBeNull();
        state.LaunchReadinessVersion.ShouldBe(1);
        state.ConfigurationVersion.ShouldBe(2);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft); // recording readiness never changes lifecycle
    }

    [Fact]
    public void Record_readiness_normalizes_metric_descriptor_whitespace()
    {
        AgentState state = StateWith(ValidCreate());

        var readiness = SampleLaunchReadiness with
        {
            Metrics =
            [
                new LaunchMetricDefinition("  SM-2  ", LaunchMetricClassification.Primary, "  numerator ", " denominator ", 0.3m, " window ", " cohort "),
            ],
        };
        DomainResult result = AgentAggregate.Handle(RecordCommand(readiness), state, Envelope(RecordCommand(readiness)));

        result.IsSuccess.ShouldBeTrue();
        LaunchMetricDefinition stored = result.Events[0].ShouldBeOfType<AgentLaunchReadinessRecorded>().Readiness.Metrics[0];
        stored.MetricId.ShouldBe("SM-2");
        stored.Numerator.ShouldBe("numerator");
        stored.LaunchCohort.ShouldBe("cohort");
    }

    // ===== AC2/AC3: structural rejections → AgentLaunchReadinessRejection (no value echoed) =====

    public static TheoryData<AgentLaunchReadiness> StructurallyInvalidReadiness() =>
    [
        // A metric missing its numerator.
        SampleLaunchReadiness with { Metrics = [new LaunchMetricDefinition("SM-2", LaunchMetricClassification.Primary, "  ", "denominator", 0.3m, "window", "cohort")] },
        // A metric with an unset classification.
        SampleLaunchReadiness with { Metrics = [new LaunchMetricDefinition("SM-2", LaunchMetricClassification.Unknown, "numerator", "denominator", 0.3m, "window", "cohort")] },
        // A latency target with an unspecified mode.
        SampleLaunchReadiness with { LatencyTargets = [new ResponseModeLatencyTarget(AgentResponseMode.Unknown, 4000)] },
        // A latency target with a non-positive milliseconds value.
        SampleLaunchReadiness with { LatencyTargets = [new ResponseModeLatencyTarget(AgentResponseMode.Automatic, 0)] },
        // Duplicate response modes in the latency list.
        SampleLaunchReadiness with { LatencyTargets = [new ResponseModeLatencyTarget(AgentResponseMode.Automatic, 4000), new ResponseModeLatencyTarget(AgentResponseMode.Automatic, 5000)] },
        // An unspecified cost posture.
        SampleLaunchReadiness with { CostPosture = CostControlPosture.Unknown },
    ];

    [Theory]
    [MemberData(nameof(StructurallyInvalidReadiness))]
    public void Record_structurally_invalid_readiness_is_rejected_and_records_nothing(AgentLaunchReadiness readiness)
    {
        AgentState state = StateWith(ValidCreate());

        RecordAgentLaunchReadiness command = RecordCommand(readiness);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentLaunchReadinessRejection>();

        ApplyAll(state, result);
        state.LaunchReadiness.ShouldBeNull(); // nothing recorded on a rejected readiness
    }

    [Fact]
    public void Record_null_readiness_is_rejected_as_invalid()
    {
        AgentState state = StateWith(ValidCreate());

        var command = new RecordAgentLaunchReadiness(null!);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentLaunchReadinessRejection>();
    }

    [Fact]
    public void Record_invalid_readiness_rejection_reason_never_echoes_metric_descriptor_content()
    {
        // AD-14: the rejection reason is a SAFE classification only — it must never leak a configured descriptor.
        const string secretLikeDescriptor = "DO-NOT-LEAK-numerator-x1y2z3";
        AgentState state = StateWith(ValidCreate());

        var readiness = SampleLaunchReadiness with
        {
            Metrics = [new LaunchMetricDefinition("SM-2", LaunchMetricClassification.Unknown, secretLikeDescriptor, "denominator", 0.3m, "window", "cohort")],
        };
        RecordAgentLaunchReadiness command = RecordCommand(readiness);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        AgentLaunchReadinessRejection rejection = result.Events[0].ShouldBeOfType<AgentLaunchReadinessRejection>();
        rejection.Reason.ShouldNotContain(secretLikeDescriptor);
    }

    // ===== AD-13: idempotent identical readiness, changed readiness bumps version + future-only =====

    [Fact]
    public void Re_record_identical_readiness_is_an_idempotent_noop()
    {
        AgentState state = StateWithLaunchReadiness(ValidCreate());

        RecordAgentLaunchReadiness command = RecordCommand();
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Changing_the_readiness_emits_a_new_event_and_bumps_the_launch_readiness_version()
    {
        AgentState state = StateWithLaunchReadiness(ValidCreate()); // LaunchReadinessVersion = 1

        var changed = SampleLaunchReadiness with { CostPosture = CostControlPosture.Quotas };
        RecordAgentLaunchReadiness command = RecordCommand(changed);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentLaunchReadinessRecorded recorded = result.Events[0].ShouldBeOfType<AgentLaunchReadinessRecorded>();
        recorded.Readiness.CostPosture.ShouldBe(CostControlPosture.Quotas);
        recorded.LaunchReadinessVersion.ShouldBe(2); // bumped from 1
    }

    // ===== Record not-found / authorization fail closed =====

    [Fact]
    public void Record_on_a_missing_agent_is_rejected_as_not_found()
    {
        RecordAgentLaunchReadiness command = RecordCommand();

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentNotFoundRejection>();
    }

    [Fact]
    public void Record_without_agents_admin_is_denied()
    {
        AgentState state = StateWith(ValidCreate());
        RecordAgentLaunchReadiness command = RecordCommand();

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command, isAgentsAdmin: false, actorUserId: "intruder"));

        result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>()
            .CommandName.ShouldBe(nameof(RecordAgentLaunchReadiness));
    }

    // ===== AC1/AC4: production-like enablement gate (pure state check; fail closed) =====

    [Fact]
    public void Enable_on_a_launch_ready_agent_with_resolved_audit_governance_succeeds()
    {
        AgentState state = StateLaunchReady(ValidCreate());
        int expectedConfigVersion = state.ConfigurationVersion + 1;

        DomainResult result = AgentAggregate.Handle(new EnableProductionLikeGeneration(), state, EnableEnvelope(auditGovernanceResolved: true));

        result.IsSuccess.ShouldBeTrue();
        AgentProductionLikeGenerationEnabled enabled = result.Events[0].ShouldBeOfType<AgentProductionLikeGenerationEnabled>();
        enabled.AgentId.ShouldBe(AgentId);
        enabled.ConfigurationVersion.ShouldBe(expectedConfigVersion);

        ApplyAll(state, result);
        state.ProductionLikeGenerationEnabled.ShouldBeTrue();
        state.ConfigurationVersion.ShouldBe(expectedConfigVersion);
    }

    [Fact]
    public void Enable_on_a_launch_ready_agent_without_audit_governance_fails_closed_with_unresolved_audit_governance()
    {
        // The whole launch-readiness gate is otherwise satisfied; only the trusted audit-governance flag is absent (a
        // direct-gateway enablement that never resolved it). It must fail closed and keep generation disabled (AC4).
        AgentState state = StateLaunchReady(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new EnableProductionLikeGeneration(), state, EnableEnvelope(includeAuditGovernance: false));

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<AgentProductionLikeGenerationBlockedRejection>()
            .Blockers.ShouldBe([AgentLaunchReadinessBlocker.UnresolvedAuditGovernance]);

        ApplyAll(state, result);
        state.ProductionLikeGenerationEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Enable_on_a_launch_ready_agent_with_a_non_true_audit_governance_flag_fails_closed()
    {
        // A surprising/forged value can never resolve audit governance — only the exact, case-sensitive "true" does.
        AgentState state = StateLaunchReady(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new EnableProductionLikeGeneration(), state, EnableEnvelope(auditGovernanceResolved: false));

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<AgentProductionLikeGenerationBlockedRejection>()
            .Blockers.ShouldContain(AgentLaunchReadinessBlocker.UnresolvedAuditGovernance);
    }

    [Fact]
    public void Enable_with_no_readiness_recorded_blocks_with_the_full_deterministic_order()
    {
        // A bare created Agent: no content safety, no recorded readiness, and no resolved audit governance. Every gate
        // fails closed, in the documented stable order (AC1, AC4).
        AgentState state = StateWith(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new EnableProductionLikeGeneration(), state, EnableEnvelope(includeAuditGovernance: false));

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<AgentProductionLikeGenerationBlockedRejection>()
            .Blockers.ShouldBe([
                AgentLaunchReadinessBlocker.MissingContentSafetyPolicy,
                AgentLaunchReadinessBlocker.MissingContextPolicy,
                AgentLaunchReadinessBlocker.MissingLaunchMetrics,
                AgentLaunchReadinessBlocker.MissingAutomaticLatencyTarget,
                AgentLaunchReadinessBlocker.MissingConfirmationLatencyTarget,
                AgentLaunchReadinessBlocker.MissingCostControlPosture,
                AgentLaunchReadinessBlocker.UnresolvedAuditGovernance,
            ]);
    }

    [Fact]
    public void Enable_blocked_when_a_per_mode_latency_target_is_missing()
    {
        // A readiness recorded with only the Automatic latency target — recording accepts a well-formed partial list, and
        // the gate blocks enablement with the per-mode MissingConfirmationLatencyTarget blocker (AC3, AC4).
        var partial = SampleLaunchReadiness with { LatencyTargets = [new ResponseModeLatencyTarget(AgentResponseMode.Automatic, 4000)] };
        AgentState state = StateLaunchReady(ValidCreate(), partial);

        DomainResult result = AgentAggregate.Handle(new EnableProductionLikeGeneration(), state, EnableEnvelope(auditGovernanceResolved: true));

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<AgentProductionLikeGenerationBlockedRejection>()
            .Blockers.ShouldBe([AgentLaunchReadinessBlocker.MissingConfirmationLatencyTarget]);
    }

    [Fact]
    public void Enable_is_idempotent_once_already_enabled()
    {
        AgentState state = StateLaunchReady(ValidCreate());
        ApplyAll(state, AgentAggregate.Handle(new EnableProductionLikeGeneration(), state, EnableEnvelope(auditGovernanceResolved: true)));
        state.ProductionLikeGenerationEnabled.ShouldBeTrue();

        DomainResult result = AgentAggregate.Handle(new EnableProductionLikeGeneration(), state, EnableEnvelope(auditGovernanceResolved: true));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Enable_on_a_missing_agent_is_rejected_as_not_found()
    {
        DomainResult result = AgentAggregate.Handle(new EnableProductionLikeGeneration(), state: null, EnableEnvelope());

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentNotFoundRejection>();
    }

    [Fact]
    public void Enable_without_agents_admin_is_denied()
    {
        AgentState state = StateLaunchReady(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new EnableProductionLikeGeneration(), state, EnableEnvelope(isAgentsAdmin: false, actorUserId: "intruder"));

        result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>()
            .CommandName.ShouldBe(nameof(EnableProductionLikeGeneration));
    }

    // ===== AC4 separation: launch readiness is a distinct gate from baseline activation =====

    [Fact]
    public void Baseline_activation_does_not_include_launch_readiness_blockers()
    {
        // A fully baseline-ready Agent (content safety present, Automatic mode) activates — the baseline ActivateAgent
        // gate is untouched and never folds in launch-readiness blockers (the two gates are separate).
        AgentState state = StateWithSelectedProvider(ValidCreate());
        state.ProductionLikeGenerationEnabled.ShouldBeFalse();

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope());

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentActivated>();
    }

    // ===== Replay / rehydration through Apply =====

    [Fact]
    public void Apply_readiness_and_enablement_track_state_and_bump_configuration_version()
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1

        state.Apply(new AgentLaunchReadinessRecorded(AgentId, SampleLaunchReadiness, LaunchReadinessVersion: 1, ConfigurationVersion: 2));
        state.LaunchReadiness.ShouldNotBeNull();
        state.LaunchReadinessVersion.ShouldBe(1);
        state.ConfigurationVersion.ShouldBe(2);

        state.Apply(new AgentProductionLikeGenerationEnabled(AgentId, ConfigurationVersion: 3));
        state.ProductionLikeGenerationEnabled.ShouldBeTrue();
        state.ConfigurationVersion.ShouldBe(3);
    }

    [Fact]
    public void Apply_readiness_before_create_is_ignored()
    {
        var state = new AgentState();

        state.Apply(new AgentLaunchReadinessRecorded(AgentId, SampleLaunchReadiness, LaunchReadinessVersion: 1, ConfigurationVersion: 2));

        state.LaunchReadiness.ShouldBeNull();
        state.IsCreated.ShouldBeFalse();
    }

    // ===== AC4: the inspection read path surfaces blockers in lock-step with what enablement would reject =====

    [Fact]
    public void Inspection_surfaces_a_launch_ready_view_and_no_blockers_when_audit_governance_is_resolved()
    {
        AgentState state = StateLaunchReady(ValidCreate());

        AgentLaunchReadinessView view = AgentLaunchReadinessInspection
            .GetLaunchReadiness(state, isAuthorized: true, auditGovernanceResolved: true)
            .Readiness.ShouldNotBeNull();

        view.HasContentSafetyPolicy.ShouldBeTrue();
        view.HasContextPolicy.ShouldBeTrue();
        view.Metrics.Count.ShouldBe(2);
        view.Blockers.ShouldBeEmpty();
        view.ProductionLikeGenerationEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Inspection_fails_closed_for_an_unauthorized_or_missing_agent_indistinguishably()
    {
        AgentState state = StateLaunchReady(ValidCreate());

        AgentLaunchReadinessInspectionResult unauthorized = AgentLaunchReadinessInspection.GetLaunchReadiness(state, isAuthorized: false, auditGovernanceResolved: true);
        AgentLaunchReadinessInspectionResult missing = AgentLaunchReadinessInspection.GetLaunchReadiness(state: null, isAuthorized: true, auditGovernanceResolved: true);

        unauthorized.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        unauthorized.Readiness.ShouldBeNull();
        missing.Status.ShouldBe(AgentInspectionStatus.AgentNotFound);
        missing.Readiness.ShouldBeNull();
    }

    [Fact]
    public void Inspection_surfaces_unresolved_audit_governance_blocker_when_not_resolved()
    {
        AgentState state = StateLaunchReady(ValidCreate());

        AgentLaunchReadinessView view = AgentLaunchReadinessInspection
            .GetLaunchReadiness(state, isAuthorized: true, auditGovernanceResolved: false)
            .Readiness.ShouldNotBeNull();

        view.Blockers.ShouldBe([AgentLaunchReadinessBlocker.UnresolvedAuditGovernance]);
    }

    // ===== AC2: metric-completeness gate — the pure policy distinguishes "no metrics" from "incomplete metric" =====
    // The aggregate's recording path rejects an incomplete metric, so IncompleteLaunchMetricDefinition can only be
    // computed by the read/gate policy itself (its own doc comment promises it "keeps the read path robust to any
    // future recording path"). These exercise that branch directly so the defensive blocker is not dead code.

    [Fact]
    public void Compute_blockers_flags_a_present_but_incomplete_metric_as_incomplete_not_missing()
    {
        // A metric present but missing one of its five governance descriptors (blank denominator) is INCOMPLETE — every
        // other gate passes, so the only blocker is IncompleteLaunchMetricDefinition (AC2).
        var incomplete = SampleLaunchReadiness with
        {
            Metrics = [new LaunchMetricDefinition("SM-2", LaunchMetricClassification.Primary, "numerator", "  ", 0.3m, "window", "cohort")],
        };

        IReadOnlyList<AgentLaunchReadinessBlocker> blockers = AgentLaunchReadinessPolicy.ComputeLaunchReadinessBlockers(
            hasContentSafetyPolicy: true,
            hasContextPolicy: true,
            incomplete,
            auditGovernanceResolved: true);

        blockers.ShouldBe([AgentLaunchReadinessBlocker.IncompleteLaunchMetricDefinition]);
    }

    [Fact]
    public void Compute_blockers_flags_absent_metrics_as_missing_not_incomplete()
    {
        // No metrics at all is MISSING (the two metric blockers are mutually exclusive — absent ≠ incomplete; AC2).
        var noMetrics = SampleLaunchReadiness with { Metrics = [] };

        IReadOnlyList<AgentLaunchReadinessBlocker> blockers = AgentLaunchReadinessPolicy.ComputeLaunchReadinessBlockers(
            hasContentSafetyPolicy: true,
            hasContextPolicy: true,
            noMetrics,
            auditGovernanceResolved: true);

        blockers.ShouldBe([AgentLaunchReadinessBlocker.MissingLaunchMetrics]);
        blockers.ShouldNotContain(AgentLaunchReadinessBlocker.IncompleteLaunchMetricDefinition);
    }

    [Theory]
    [InlineData("", "denominator", "window", "cohort", LaunchMetricClassification.Primary)] // blank metric id
    [InlineData("SM-2", "", "window", "cohort", LaunchMetricClassification.Primary)] // blank denominator
    [InlineData("SM-2", "denominator", "  ", "cohort", LaunchMetricClassification.Primary)] // whitespace window
    [InlineData("SM-2", "denominator", "window", "", LaunchMetricClassification.Primary)] // blank cohort
    [InlineData("SM-2", "denominator", "window", "cohort", LaunchMetricClassification.Unknown)] // unset classification
    public void Is_metric_complete_is_false_when_any_descriptor_or_classification_is_absent(
        string metricId, string denominator, string window, string cohort, LaunchMetricClassification classification)
    {
        var metric = new LaunchMetricDefinition(metricId, classification, "numerator", denominator, 0.3m, window, cohort);

        AgentLaunchReadinessPolicy.IsMetricComplete(metric).ShouldBeFalse();
    }

    [Fact]
    public void Is_metric_complete_is_true_when_all_five_descriptors_and_a_classification_are_present()
    {
        var metric = new LaunchMetricDefinition("SM-3", LaunchMetricClassification.Secondary, "numerator", "denominator", 0.9m, "window", "cohort");

        AgentLaunchReadinessPolicy.IsMetricComplete(metric).ShouldBeTrue();
    }
}
