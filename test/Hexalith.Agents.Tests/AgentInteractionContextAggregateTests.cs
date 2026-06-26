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
/// Handle-method tests for the Story 2.3 Conversation context build on <see cref="AgentInteractionAggregate"/>
/// (AC1–AC4; FR-9; AD-3, AD-11, AD-12, AD-13). Cover the full-fits and approved-bounded ready paths, the oversized /
/// not-fresh / unavailable / untrustworthy-budget blocks (never a silent truncation), the not-buildable rejections
/// (not-requested / not-authorized), idempotent terminal determinism (no decision flip), and the full reflection-dispatch
/// + JSON round-trip through <c>ProcessAsync</c>.
/// </summary>
public sealed class AgentInteractionContextAggregateTests
{
    // ===== Ready: full context fits (AC2) =====

    [Fact]
    public void Full_context_that_fits_records_context_ready_full()
    {
        AgentInteractionState state = StateAuthorized();

        DomainResult result = Context(FullFitsMeasurement(), state);

        result.IsSuccess.ShouldBeTrue();
        AgentInteractionContextReady ready = result.Events[0].ShouldBeOfType<AgentInteractionContextReady>();
        ready.AgentInteractionId.ShouldBe(InteractionId);
        ready.Evidence.Mode.ShouldBe(AgentInteractionContextMode.Full);
        ready.Evidence.UsedContextTokenCount.ShouldBe(ready.Evidence.FullContextTokenCount);
        ready.Evidence.BoundedBehaviorReference.ShouldBeNull();
        ready.Evidence.ContextWindowTokenLimit.ShouldBe(ContextWindowTokenLimit);
        ready.Evidence.ReservedOutputTokenCount.ShouldBe(ReservedOutputTokenCount);
        ready.Evidence.ContextPolicyReference.ShouldBe(AgentInteractionSnapshot.DefaultContextPolicyReference);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ContextReady);
        state.ContextEvidence.ShouldNotBeNull().Mode.ShouldBe(AgentInteractionContextMode.Full);
        state.ContextBlockReason.ShouldBeNull();
    }

    // ===== Ready: approved bounded behavior on an oversized conversation (AC4) =====

    [Fact]
    public void Oversized_with_an_approved_bounded_behavior_records_context_ready_bounded()
    {
        AgentInteractionState state = StateAuthorized();

        DomainResult result = Context(BoundedApprovedMeasurement(boundedLimit: 50_000), state);

        AgentInteractionContextReady ready = result.Events[0].ShouldBeOfType<AgentInteractionContextReady>();
        ready.Evidence.Mode.ShouldBe(AgentInteractionContextMode.Bounded);
        ready.Evidence.BoundedBehaviorReference.ShouldBe("bounded-conversation-test-v1");
        ready.Evidence.UsedContextTokenCount.ShouldBe(50_000); // Min(full, bounded limit) — the bounds are recorded (never silent)
        ready.Evidence.FullContextTokenCount.ShouldBe(200_000);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ContextReady);
        state.ContextEvidence.ShouldNotBeNull().BoundedBehaviorReference.ShouldBe("bounded-conversation-test-v1");
    }

    // ===== Block: oversized + no approved bounded behavior — the AD-17 "context-too-large blocking" gate (AC3) =====

    [Fact]
    public void Oversized_with_no_approved_bounded_behavior_blocks_and_never_silently_truncates()
    {
        AgentInteractionState state = StateAuthorized();

        DomainResult result = Context(OversizedMeasurement(), state);

        AgentInteractionContextBlocked blocked = result.Events[0].ShouldBeOfType<AgentInteractionContextBlocked>();
        blocked.Reason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
        // Evidence records the overflow so audit shows WHY (FullContextTokenCount vs available budget).
        blocked.Evidence.FullContextTokenCount.ShouldBe(200_000);
        blocked.Evidence.ContextWindowTokenLimit.ShouldBe(ContextWindowTokenLimit);
        blocked.Evidence.UsedContextTokenCount.ShouldBe(0);

        // NEVER a silent truncation: no ContextReady is emitted (AC3/AC4).
        result.Events.OfType<AgentInteractionContextReady>().ShouldBeEmpty();

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        state.ContextBlockReason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
    }

    [Fact]
    public void An_approved_bounded_behavior_that_itself_exceeds_the_budget_still_blocks()
    {
        // A bounded limit that does NOT fit the available budget cannot be used — the oversized case blocks (never silent).
        AgentInteractionState state = StateAuthorized();

        DomainResult result = Context(BoundedApprovedMeasurement(boundedLimit: 130_000), state);

        AgentInteractionContextBlocked blocked = result.Events[0].ShouldBeOfType<AgentInteractionContextBlocked>();
        blocked.Reason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
        result.Events.OfType<AgentInteractionContextReady>().ShouldBeEmpty();
    }

    // ===== Block: load-failure outcomes (AC1, AC3) =====

    [Fact]
    public void Stale_load_blocks_with_context_not_fresh()
    {
        AgentInteractionState state = StateAuthorized();

        DomainResult result = Context(Measurement(loadOutcome: AgentInteractionContextLoadOutcome.Stale), state);

        AgentInteractionContextBlocked blocked = result.Events[0].ShouldBeOfType<AgentInteractionContextBlocked>();
        blocked.Reason.ShouldBe(AgentInteractionContextBlockReason.ContextNotFresh);
        // Load failure → numerics zeroed (no cross-tenant disclosure of size/shape).
        blocked.Evidence.FullContextTokenCount.ShouldBe(0);
        blocked.Evidence.ContextWindowTokenLimit.ShouldBe(0);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        state.ContextBlockReason.ShouldBe(AgentInteractionContextBlockReason.ContextNotFresh);
    }

    [Theory]
    [InlineData(AgentInteractionContextLoadOutcome.Unauthorized)]
    [InlineData(AgentInteractionContextLoadOutcome.Unavailable)]
    [InlineData(AgentInteractionContextLoadOutcome.Unknown)]
    public void Unauthorized_or_unavailable_load_blocks_with_context_unavailable(AgentInteractionContextLoadOutcome loadOutcome)
    {
        AgentInteractionState state = StateAuthorized();

        DomainResult result = Context(Measurement(loadOutcome: loadOutcome), state);

        AgentInteractionContextBlocked blocked = result.Events[0].ShouldBeOfType<AgentInteractionContextBlocked>();
        blocked.Reason.ShouldBe(AgentInteractionContextBlockReason.ContextUnavailable);
        blocked.Evidence.FullContextTokenCount.ShouldBe(0);
    }

    // ===== Block: untrustworthy budget (AC2, AC3) =====

    [Theory]
    [InlineData(0, ReservedOutputTokenCount, ProviderCapabilityVersion)]   // non-positive window
    [InlineData(ContextWindowTokenLimit, -1, ProviderCapabilityVersion)]    // negative reserved
    [InlineData(100, 100, ProviderCapabilityVersion)]                       // reserved >= window
    [InlineData(ContextWindowTokenLimit, ReservedOutputTokenCount, 0)]      // non-positive capability version
    public void Invalid_or_zero_budget_blocks_with_model_budget_unavailable(int windowLimit, int reservedOut, int capabilityVersion)
    {
        AgentInteractionState state = StateAuthorized();

        DomainResult result = Context(
            Measurement(
                fullContextTokenCount: 10,
                contextWindowTokenLimit: windowLimit,
                reservedOutputTokenCount: reservedOut,
                providerCapabilityVersion: capabilityVersion),
            state);

        AgentInteractionContextBlocked blocked = result.Events[0].ShouldBeOfType<AgentInteractionContextBlocked>();
        blocked.Reason.ShouldBe(AgentInteractionContextBlockReason.ModelBudgetUnavailable);
        result.Events.OfType<AgentInteractionContextReady>().ShouldBeEmpty();
    }

    // ===== Budget boundaries: the exact "fits" edge (AC2 vs AC3) =====

    [Fact]
    public void Full_context_exactly_at_the_available_budget_records_context_ready_full()
    {
        // The fit rule is FullContextTokenCount <= (window - reserved). At the EXACT boundary the full context must
        // still fit (inclusive). available = 128000 - 16000 = 112000.
        AgentInteractionState state = StateAuthorized();

        DomainResult result = Context(Measurement(fullContextTokenCount: ContextWindowTokenLimit - ReservedOutputTokenCount), state);

        AgentInteractionContextReady ready = result.Events[0].ShouldBeOfType<AgentInteractionContextReady>();
        ready.Evidence.Mode.ShouldBe(AgentInteractionContextMode.Full);
        ready.Evidence.UsedContextTokenCount.ShouldBe(112_000);
        result.Events.OfType<AgentInteractionContextBlocked>().ShouldBeEmpty();
    }

    [Fact]
    public void Full_context_one_token_over_the_available_budget_blocks_and_never_silently_truncates()
    {
        // One token past the boundary with no approved bounded behavior must block — never quietly trim to fit (AC3).
        AgentInteractionState state = StateAuthorized();

        DomainResult result = Context(Measurement(fullContextTokenCount: ContextWindowTokenLimit - ReservedOutputTokenCount + 1), state);

        AgentInteractionContextBlocked blocked = result.Events[0].ShouldBeOfType<AgentInteractionContextBlocked>();
        blocked.Reason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
        result.Events.OfType<AgentInteractionContextReady>().ShouldBeEmpty();
    }

    // ===== Bounded behavior boundaries (AC4 — never silent) =====

    [Fact]
    public void An_approved_bounded_behavior_with_a_non_positive_limit_is_not_usable_and_blocks()
    {
        // A degenerate approved behavior (limit <= 0) cannot stand in for real bounds — it fails the > 0 guard and the
        // oversized case blocks rather than recording a meaningless zero-token bounded context (AC4 — never silent).
        AgentInteractionState state = StateAuthorized();

        DomainResult result = Context(BoundedApprovedMeasurement(boundedLimit: 0), state);

        AgentInteractionContextBlocked blocked = result.Events[0].ShouldBeOfType<AgentInteractionContextBlocked>();
        blocked.Reason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
        result.Events.OfType<AgentInteractionContextReady>().ShouldBeEmpty();
    }

    [Fact]
    public void An_approved_bounded_behavior_whose_limit_equals_the_available_budget_is_used()
    {
        // The bounded limit fits the available budget exactly (inclusive boundary) → bounded context with the bounds
        // recorded as evidence (AC4).
        AgentInteractionState state = StateAuthorized();

        DomainResult result = Context(BoundedApprovedMeasurement(boundedLimit: ContextWindowTokenLimit - ReservedOutputTokenCount), state);

        AgentInteractionContextReady ready = result.Events[0].ShouldBeOfType<AgentInteractionContextReady>();
        ready.Evidence.Mode.ShouldBe(AgentInteractionContextMode.Bounded);
        ready.Evidence.UsedContextTokenCount.ShouldBe(112_000); // Min(200000 full, 112000 bounded) — the recorded bounds
        ready.Evidence.BoundedBehaviorReference.ShouldBe("bounded-conversation-test-v1");
    }

    // ===== Not buildable: not requested / not authorized (AD-11, AD-12) =====

    [Fact]
    public void Context_on_a_never_requested_interaction_is_not_buildable_interaction_not_requested()
    {
        DomainResult result = Context(FullFitsMeasurement(), state: null);

        AssertNotBuildable(result, AgentInteractionContextNotBuildableReason.InteractionNotRequested);
    }

    [Fact]
    public void Context_on_a_rejection_only_stream_is_not_buildable_interaction_not_requested()
    {
        DomainResult result = Context(FullFitsMeasurement(), new AgentInteractionState());

        AssertNotBuildable(result, AgentInteractionContextNotBuildableReason.InteractionNotRequested);
    }

    [Fact]
    public void Context_on_a_requested_but_not_authorized_interaction_is_not_buildable_interaction_not_authorized()
    {
        // Status is Requested (gate not yet run) — context must never be built before the gate authorizes (AD-11).
        DomainResult result = Context(FullFitsMeasurement(), StateRequested());

        AssertNotBuildable(result, AgentInteractionContextNotBuildableReason.InteractionNotAuthorized);
    }

    [Fact]
    public void Context_on_a_denied_interaction_is_not_buildable_interaction_not_authorized()
    {
        AgentInteractionState state = StateDenied();

        DomainResult result = Context(FullFitsMeasurement(), state);

        AssertNotBuildable(result, AgentInteractionContextNotBuildableReason.InteractionNotAuthorized);
        // The recorded gate denial is untouched — context never builds on a call that failed the gate.
        state.Status.ShouldBe(AgentInteractionStatus.Denied);
    }

    [Fact]
    public void Context_on_a_gate_blocked_interaction_is_not_buildable_interaction_not_authorized()
    {
        AgentInteractionState state = StateGateBlocked();

        DomainResult result = Context(FullFitsMeasurement(), state);

        AssertNotBuildable(result, AgentInteractionContextNotBuildableReason.InteractionNotAuthorized);
        state.Status.ShouldBe(AgentInteractionStatus.Blocked);
    }

    // ===== Idempotent terminal determinism (AD-13) — the decision never flips =====

    [Fact]
    public void Re_build_on_a_context_ready_interaction_is_a_noop_and_never_flips()
    {
        AgentInteractionState state = StateAuthorized();
        ApplyAll(state, Context(FullFitsMeasurement(), state)); // now ContextReady
        state.Status.ShouldBe(AgentInteractionStatus.ContextReady);

        // A re-issued build that WOULD block (oversized) must be a clean no-op — the decision never flips.
        DomainResult reissue = Context(OversizedMeasurement(), state);

        reissue.IsNoOp.ShouldBeTrue();
        reissue.Events.ShouldBeEmpty();
        state.Status.ShouldBe(AgentInteractionStatus.ContextReady);
    }

    [Fact]
    public void Re_build_on_a_context_blocked_interaction_is_a_noop_and_never_flips()
    {
        AgentInteractionState state = StateAuthorized();
        ApplyAll(state, Context(OversizedMeasurement(), state)); // now ContextBlocked
        state.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);

        // A re-issue that would now succeed must NOT flip a recorded block.
        DomainResult reissue = Context(FullFitsMeasurement(), state);

        reissue.IsNoOp.ShouldBeTrue();
        state.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        state.ContextBlockReason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
    }

    // ===== Decide / Evaluate no-drift =====

    [Fact]
    public void Decide_matches_the_aggregate_recorded_decision_for_each_outcome()
    {
        AssertDecideMatches(FullFitsMeasurement(), AgentInteractionStatus.ContextReady);
        AssertDecideMatches(BoundedApprovedMeasurement(), AgentInteractionStatus.ContextReady);
        AssertDecideMatches(OversizedMeasurement(), AgentInteractionStatus.ContextBlocked);
        AssertDecideMatches(Measurement(loadOutcome: AgentInteractionContextLoadOutcome.Stale), AgentInteractionStatus.ContextBlocked);
        AssertDecideMatches(Measurement(loadOutcome: AgentInteractionContextLoadOutcome.Unauthorized), AgentInteractionStatus.ContextBlocked);
        AssertDecideMatches(Measurement(contextWindowTokenLimit: 0), AgentInteractionStatus.ContextBlocked);
    }

    // ===== Full pipeline: reflection dispatch + JSON round-trip =====

    [Fact]
    public async Task Process_async_round_trips_the_context_command_and_records_context_ready()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateAuthorized();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentInteractionContextReady>();
        state.Status.ShouldBe(AgentInteractionStatus.ContextReady);
    }

    [Fact]
    public async Task Process_async_round_trips_a_blocking_context_command_and_records_the_block()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateAuthorized();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, ContextCommand(OversizedMeasurement()));

        AgentInteractionContextBlocked blocked = result.Events[0].ShouldBeOfType<AgentInteractionContextBlocked>();
        blocked.Reason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget); // survived the JSON round-trip by name
        state.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
    }

    // ===== Helpers =====

    private static AgentInteractionState StateDenied()
    {
        AgentInteractionState state = StateRequested();
        state.Apply(new AgentInteractionGateFailed(
            InteractionId,
            AgentInteractionStatus.Denied,
            [Verdict(AgentInteractionGateCheck.TenantAccess, AgentInteractionGateOutcome.Unauthorized)]));
        return state;
    }

    private static AgentInteractionState StateGateBlocked()
    {
        AgentInteractionState state = StateRequested();
        state.Apply(new AgentInteractionGateFailed(
            InteractionId,
            AgentInteractionStatus.Blocked,
            [Verdict(AgentInteractionGateCheck.ProviderModelReadiness, AgentInteractionGateOutcome.Disabled)]));
        return state;
    }

    private static void AssertDecideMatches(AgentInteractionContextMeasurement measurement, AgentInteractionStatus expected)
    {
        AgentInteractionContextPolicy.Decide(measurement).ShouldBe(expected);
        DomainResult result = Context(measurement, StateAuthorized());
        AgentInteractionStatus recorded = result.Events[0] switch
        {
            AgentInteractionContextReady => AgentInteractionStatus.ContextReady,
            AgentInteractionContextBlocked => AgentInteractionStatus.ContextBlocked,
            _ => AgentInteractionStatus.Unknown,
        };
        recorded.ShouldBe(expected);
    }

    private static DomainResult Context(AgentInteractionContextMeasurement measurement, AgentInteractionState? state)
    {
        BuildAgentInteractionContext command = ContextCommand(measurement);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static void AssertNotBuildable(DomainResult result, AgentInteractionContextNotBuildableReason expected)
    {
        result.IsRejection.ShouldBeTrue();
        AgentInteractionContextNotBuildableRejection rejection =
            result.Events[0].ShouldBeOfType<AgentInteractionContextNotBuildableRejection>();
        rejection.Reason.ShouldBe(expected);
        rejection.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.OfType<AgentInteractionContextReady>().ShouldBeEmpty();
        result.Events.OfType<AgentInteractionContextBlocked>().ShouldBeEmpty();
    }
}
