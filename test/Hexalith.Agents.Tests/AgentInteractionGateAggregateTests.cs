using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentInteractionTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Handle-method tests for the Story 2.2 invocation gate on <see cref="AgentInteractionAggregate"/> (AC1–AC4; FR-20,
/// FR-21; AD-12, AD-13). Cover the authorized path, the Denied (authorization-class) and Blocked (readiness-class)
/// decisions across the AD-12 fail-closed outcome vocabulary, Denied precedence on mixed blockers, the
/// not-evaluable rejections (not-requested / empty verdicts), idempotent terminal determinism (no decision flip), and
/// the full reflection-dispatch + JSON round-trip through <c>ProcessAsync</c>.
/// </summary>
public sealed class AgentInteractionGateAggregateTests
{
    // The AD-12 fail-closed outcome vocabulary every blocking check is exercised against.
    private static readonly AgentInteractionGateOutcome[] _blockingOutcomes =
    [
        AgentInteractionGateOutcome.Missing,
        AgentInteractionGateOutcome.Stale,
        AgentInteractionGateOutcome.Ambiguous,
        AgentInteractionGateOutcome.Disabled,
        AgentInteractionGateOutcome.Unavailable,
        AgentInteractionGateOutcome.Unauthorized,
    ];

    private static readonly AgentInteractionGateCheck[] _authorizationClassChecks =
    [
        AgentInteractionGateCheck.TenantAccess,
        AgentInteractionGateCheck.CallerPartyState,
        AgentInteractionGateCheck.SourceConversationAccess,
    ];

    private static readonly AgentInteractionGateCheck[] _readinessClassChecks =
    [
        AgentInteractionGateCheck.AgentLifecycle,
        AgentInteractionGateCheck.AgentPartyIdentity,
        AgentInteractionGateCheck.ProviderModelReadiness,
        AgentInteractionGateCheck.ResponsePolicy,
        AgentInteractionGateCheck.ContentSafetyPolicy,
        AgentInteractionGateCheck.DependencyFreshness,
    ];

    public static TheoryData<AgentInteractionGateCheck, AgentInteractionGateOutcome> AuthorizationBlockers()
        => BlockerData(_authorizationClassChecks);

    public static TheoryData<AgentInteractionGateCheck, AgentInteractionGateOutcome> ReadinessBlockers()
        => BlockerData(_readinessClassChecks);

    // ===== Authorized path (AC1) =====

    [Fact]
    public void All_satisfied_verdicts_authorize_and_record_authorized_state()
    {
        AgentInteractionState state = StateRequested();

        DomainResult result = Gate(AllSatisfied(), state);

        result.IsSuccess.ShouldBeTrue();
        AgentInteractionAuthorized authorized = result.Events[0].ShouldBeOfType<AgentInteractionAuthorized>();
        authorized.AgentInteractionId.ShouldBe(InteractionId);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.Authorized);
        state.GateVerdicts.ShouldBeNull(); // no blockers recorded on the authorized path
    }

    // ===== Denied: each authorization-class blocker × each AD-12 outcome (AC2, AC3) =====

    [Theory]
    [MemberData(nameof(AuthorizationBlockers))]
    public void Authorization_class_blocker_decides_denied(AgentInteractionGateCheck check, AgentInteractionGateOutcome outcome)
    {
        AgentInteractionState state = StateRequested();

        DomainResult result = Gate(SatisfiedExcept(check, outcome), state);

        AgentInteractionGateFailed failed = result.Events[0].ShouldBeOfType<AgentInteractionGateFailed>();
        failed.Decision.ShouldBe(AgentInteractionStatus.Denied);
        failed.AgentInteractionId.ShouldBe(InteractionId);
        AgentInvocationGateVerdict blocker = failed.Blockers.ShouldHaveSingleItem();
        blocker.Check.ShouldBe(check);
        blocker.Outcome.ShouldBe(outcome);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.Denied);
        state.GateVerdicts.ShouldNotBeNull().ShouldHaveSingleItem();
    }

    // ===== Blocked: each readiness-class blocker × each AD-12 outcome (AC1) =====

    [Theory]
    [MemberData(nameof(ReadinessBlockers))]
    public void Readiness_class_blocker_decides_blocked(AgentInteractionGateCheck check, AgentInteractionGateOutcome outcome)
    {
        AgentInteractionState state = StateRequested();

        DomainResult result = Gate(SatisfiedExcept(check, outcome), state);

        AgentInteractionGateFailed failed = result.Events[0].ShouldBeOfType<AgentInteractionGateFailed>();
        failed.Decision.ShouldBe(AgentInteractionStatus.Blocked);
        AgentInvocationGateVerdict blocker = failed.Blockers.ShouldHaveSingleItem();
        blocker.Check.ShouldBe(check);
        blocker.Outcome.ShouldBe(outcome);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.Blocked);
    }

    // ===== Denied precedence on mixed blockers (AC2/AC3 more-restrictive rule) =====

    [Fact]
    public void Mixed_authorization_and_readiness_blockers_decide_denied()
    {
        AgentInteractionState state = StateRequested();
        // One authorization-class blocker (TenantAccess) + one readiness-class blocker (AgentLifecycle): Denied wins.
        IReadOnlyList<AgentInvocationGateVerdict> verdicts = AllSatisfied()
            .Select(v => v.Check switch
            {
                AgentInteractionGateCheck.TenantAccess => v with { Outcome = AgentInteractionGateOutcome.Unauthorized },
                AgentInteractionGateCheck.AgentLifecycle => v with { Outcome = AgentInteractionGateOutcome.Disabled },
                _ => v,
            })
            .ToList();

        DomainResult result = Gate(verdicts, state);

        AgentInteractionGateFailed failed = result.Events[0].ShouldBeOfType<AgentInteractionGateFailed>();
        failed.Decision.ShouldBe(AgentInteractionStatus.Denied);
        failed.Blockers.Count.ShouldBe(2); // both blockers are recorded as Audit Evidence (AC4)
    }

    [Fact]
    public void All_blocking_readiness_verdicts_record_every_blocker_as_evidence()
    {
        AgentInteractionState state = StateRequested();
        // Every readiness check unavailable, all authorization checks satisfied → Blocked with all six blockers.
        IReadOnlyList<AgentInvocationGateVerdict> verdicts = AllSatisfied()
            .Select(v => _readinessClassChecks.Contains(v.Check) ? v with { Outcome = AgentInteractionGateOutcome.Unavailable } : v)
            .ToList();

        DomainResult result = Gate(verdicts, state);

        AgentInteractionGateFailed failed = result.Events[0].ShouldBeOfType<AgentInteractionGateFailed>();
        failed.Decision.ShouldBe(AgentInteractionStatus.Blocked);
        failed.Blockers.Count.ShouldBe(_readinessClassChecks.Length);
    }

    // ===== Not evaluable (AC4) =====

    [Fact]
    public void Gate_on_a_never_requested_interaction_is_not_evaluable_interaction_not_requested()
    {
        DomainResult result = Gate(AllSatisfied(), state: null);

        AssertNotEvaluable(result, AgentInteractionGateNotEvaluableReason.InteractionNotRequested);
    }

    [Fact]
    public void Gate_on_a_rejection_only_stream_is_not_evaluable_interaction_not_requested()
    {
        // A stream with no recorded request (IsRequested false) cannot be gated.
        DomainResult result = Gate(AllSatisfied(), new AgentInteractionState());

        AssertNotEvaluable(result, AgentInteractionGateNotEvaluableReason.InteractionNotRequested);
    }

    [Fact]
    public void Gate_with_empty_verdicts_is_not_evaluable_no_verdicts_provided()
    {
        DomainResult result = Gate([], StateRequested());

        AssertNotEvaluable(result, AgentInteractionGateNotEvaluableReason.NoVerdictsProvided);
    }

    [Fact]
    public void Gate_with_null_verdicts_is_not_evaluable_no_verdicts_provided()
    {
        var command = new EvaluateAgentInteractionGate(InteractionId, null!);

        DomainResult result = AgentInteractionAggregate.Handle(command, StateRequested(), Envelope(command));

        AssertNotEvaluable(result, AgentInteractionGateNotEvaluableReason.NoVerdictsProvided);
    }

    [Fact]
    public void Not_requested_precedes_empty_verdicts()
    {
        // Both preconditions fail; the not-requested rejection dominates (the gate cannot reach the verdict check).
        DomainResult result = Gate([], state: null);

        AssertNotEvaluable(result, AgentInteractionGateNotEvaluableReason.InteractionNotRequested);
    }

    // ===== Idempotent terminal determinism (AD-13) =====

    [Fact]
    public void Re_evaluation_on_an_authorized_interaction_is_a_noop_and_never_flips()
    {
        AgentInteractionState state = StateRequested();
        ApplyAll(state, Gate(AllSatisfied(), state)); // now Authorized
        state.Status.ShouldBe(AgentInteractionStatus.Authorized);

        // A re-issued gate that WOULD deny (tenant unauthorized) must be a clean no-op — the decision never flips.
        DomainResult reissue = Gate(SatisfiedExcept(AgentInteractionGateCheck.TenantAccess, AgentInteractionGateOutcome.Unauthorized), state);

        reissue.IsNoOp.ShouldBeTrue();
        reissue.Events.ShouldBeEmpty();
        state.Status.ShouldBe(AgentInteractionStatus.Authorized);
    }

    [Fact]
    public void Re_evaluation_on_a_denied_interaction_is_a_noop_and_never_flips()
    {
        AgentInteractionState state = StateRequested();
        ApplyAll(state, Gate(SatisfiedExcept(AgentInteractionGateCheck.CallerPartyState, AgentInteractionGateOutcome.Missing), state)); // Denied
        state.Status.ShouldBe(AgentInteractionStatus.Denied);

        // A re-issue that would now authorize must NOT flip a recorded denial.
        DomainResult reissue = Gate(AllSatisfied(), state);

        reissue.IsNoOp.ShouldBeTrue();
        state.Status.ShouldBe(AgentInteractionStatus.Denied);
    }

    [Fact]
    public void Re_evaluation_on_a_blocked_interaction_is_a_noop()
    {
        AgentInteractionState state = StateRequested();
        ApplyAll(state, Gate(SatisfiedExcept(AgentInteractionGateCheck.ProviderModelReadiness, AgentInteractionGateOutcome.Disabled), state)); // Blocked
        state.Status.ShouldBe(AgentInteractionStatus.Blocked);

        DomainResult reissue = Gate(AllSatisfied(), state);

        reissue.IsNoOp.ShouldBeTrue();
        state.Status.ShouldBe(AgentInteractionStatus.Blocked);
    }

    // ===== Full pipeline: reflection dispatch + JSON round-trip =====

    [Fact]
    public async Task Process_async_round_trips_the_gate_command_and_records_authorized()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateRequested();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentInteractionAuthorized>();
        state.Status.ShouldBe(AgentInteractionStatus.Authorized);
    }

    [Fact]
    public async Task Process_async_round_trips_a_blocking_gate_command_and_records_the_blockers()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateRequested();

        DomainResult result = await ProcessAndApplyAsync(
            aggregate,
            state,
            GateCommand(SatisfiedExcept(AgentInteractionGateCheck.SourceConversationAccess, AgentInteractionGateOutcome.Stale)));

        AgentInteractionGateFailed failed = result.Events[0].ShouldBeOfType<AgentInteractionGateFailed>();
        failed.Decision.ShouldBe(AgentInteractionStatus.Denied); // SourceConversationAccess is authorization-class
        AgentInvocationGateVerdict blocker = failed.Blockers.ShouldHaveSingleItem();
        blocker.Check.ShouldBe(AgentInteractionGateCheck.SourceConversationAccess);
        blocker.Outcome.ShouldBe(AgentInteractionGateOutcome.Stale); // survived the JSON round-trip by name
        state.Status.ShouldBe(AgentInteractionStatus.Denied);
    }

    // ===== QA gap coverage: the Unknown sentinel fails closed (AD-12; FR-21) =====

    [Fact]
    public void Unknown_outcome_on_an_authorization_check_is_a_blocker_and_decides_denied()
    {
        // The aggregate's _blockingOutcomes matrix deliberately omits Unknown; this pins that the fail-safe sentinel is
        // itself a blocker (any Outcome != Satisfied blocks) and an authorization-class one decides Denied.
        AgentInteractionState state = StateRequested();

        DomainResult result = Gate(SatisfiedExcept(AgentInteractionGateCheck.TenantAccess, AgentInteractionGateOutcome.Unknown), state);

        AgentInteractionGateFailed failed = result.Events[0].ShouldBeOfType<AgentInteractionGateFailed>();
        failed.Decision.ShouldBe(AgentInteractionStatus.Denied);
        AgentInvocationGateVerdict blocker = failed.Blockers.ShouldHaveSingleItem();
        blocker.Outcome.ShouldBe(AgentInteractionGateOutcome.Unknown);
    }

    [Fact]
    public void Unknown_outcome_on_a_readiness_check_is_a_blocker_and_decides_blocked()
    {
        AgentInteractionState state = StateRequested();

        DomainResult result = Gate(SatisfiedExcept(AgentInteractionGateCheck.AgentLifecycle, AgentInteractionGateOutcome.Unknown), state);

        AgentInteractionGateFailed failed = result.Events[0].ShouldBeOfType<AgentInteractionGateFailed>();
        failed.Decision.ShouldBe(AgentInteractionStatus.Blocked);
    }

    [Fact]
    public void An_unrecognized_check_still_blocks_and_is_classified_readiness_not_authorization()
    {
        // A blocking verdict on an unknown/future check (not in the authorization-class set) must still fail the gate —
        // and be treated as non-authorization (Blocked), never silently dropped.
        AgentInteractionState state = StateRequested();
        IReadOnlyList<AgentInvocationGateVerdict> verdicts =
            [.. AllSatisfied(), Verdict(AgentInteractionGateCheck.Unknown, AgentInteractionGateOutcome.Missing)];

        DomainResult result = Gate(verdicts, state);

        AgentInteractionGateFailed failed = result.Events[0].ShouldBeOfType<AgentInteractionGateFailed>();
        failed.Decision.ShouldBe(AgentInteractionStatus.Blocked);
        failed.Blockers.ShouldHaveSingleItem().Check.ShouldBe(AgentInteractionGateCheck.Unknown);
    }

    // ===== QA gap coverage: full-failure audit evidence + Decide/Evaluate no-drift (AC4; AD-12) =====

    [Fact]
    public void Every_check_blocking_records_all_nine_blockers_as_evidence_and_decides_denied()
    {
        AgentInteractionState state = StateRequested();
        IReadOnlyList<AgentInvocationGateVerdict> verdicts =
            AllSatisfied().Select(v => v with { Outcome = AgentInteractionGateOutcome.Unavailable }).ToList();

        DomainResult result = Gate(verdicts, state);

        AgentInteractionGateFailed failed = result.Events[0].ShouldBeOfType<AgentInteractionGateFailed>();
        failed.Decision.ShouldBe(AgentInteractionStatus.Denied); // authorization checks are among the blockers
        failed.Blockers.Count.ShouldBe(9);
    }

    [Theory]
    [MemberData(nameof(AllBlockerVerdicts))]
    public void Decide_matches_the_aggregate_recorded_decision(AgentInteractionGateCheck check, AgentInteractionGateOutcome outcome)
    {
        // The orchestrator returns AgentInvocationGatePolicy.Decide(...) while the aggregate emits AgentInvocationGate
        // Policy.Evaluate(...); pin that the two can never drift for any single-blocker verdict set.
        IReadOnlyList<AgentInvocationGateVerdict> verdicts = SatisfiedExcept(check, outcome);

        AgentInteractionStatus decided = AgentInvocationGatePolicy.Decide(verdicts);
        DomainResult result = Gate(verdicts, StateRequested());
        AgentInteractionStatus recorded = result.Events[0] switch
        {
            AgentInteractionAuthorized => AgentInteractionStatus.Authorized,
            AgentInteractionGateFailed failed => failed.Decision,
            _ => AgentInteractionStatus.Unknown,
        };

        recorded.ShouldBe(decided);
    }

    [Fact]
    public void Decide_matches_the_aggregate_on_the_all_satisfied_authorized_path()
    {
        IReadOnlyList<AgentInvocationGateVerdict> verdicts = AllSatisfied();

        AgentInvocationGatePolicy.Decide(verdicts).ShouldBe(AgentInteractionStatus.Authorized);
        Gate(verdicts, StateRequested()).Events[0].ShouldBeOfType<AgentInteractionAuthorized>();
    }

    public static TheoryData<AgentInteractionGateCheck, AgentInteractionGateOutcome> AllBlockerVerdicts()
        => BlockerData([.. _authorizationClassChecks, .. _readinessClassChecks]);

    // ===== Helpers =====

    private static DomainResult Gate(IReadOnlyList<AgentInvocationGateVerdict> verdicts, AgentInteractionState? state)
    {
        EvaluateAgentInteractionGate command = GateCommand(verdicts);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static void AssertNotEvaluable(DomainResult result, AgentInteractionGateNotEvaluableReason expected)
    {
        result.IsRejection.ShouldBeTrue();
        AgentInteractionGateNotEvaluableRejection rejection =
            result.Events[0].ShouldBeOfType<AgentInteractionGateNotEvaluableRejection>();
        rejection.Reason.ShouldBe(expected);
        rejection.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.OfType<AgentInteractionAuthorized>().ShouldBeEmpty();
        result.Events.OfType<AgentInteractionGateFailed>().ShouldBeEmpty();
    }

    private static TheoryData<AgentInteractionGateCheck, AgentInteractionGateOutcome> BlockerData(AgentInteractionGateCheck[] checks)
    {
        var data = new TheoryData<AgentInteractionGateCheck, AgentInteractionGateOutcome>();
        foreach (AgentInteractionGateCheck check in checks)
        {
            foreach (AgentInteractionGateOutcome outcome in _blockingOutcomes)
            {
                data.Add(check, outcome);
            }
        }

        return data;
    }
}
