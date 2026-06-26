using System.Linq;
using System.Text.Json;
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
/// Handle-method tests for the Story 2.4 Agent-output generation on <see cref="AgentInteractionAggregate"/> (AC1–AC4;
/// FR-9, FR-10, FR-12; AD-3, AD-5, AD-9, AD-13). Cover the succeeded path (records the approvable version), the safety
/// block (SafetyFailed, never a version, never the unsafe content), every other failure outcome → GenerationFailed with
/// the mapped reason, the not-generatable rejections (not-requested / context-not-ready), terminal idempotency for all
/// three terminal statuses (no decision flip, no duplicate version), the Evaluate/Decide no-drift theory, and the full
/// reflection-dispatch + JSON round-trip through <c>ProcessAsync</c>.
/// </summary>
public sealed class AgentInteractionGenerationAggregateTests
{
    // ===== Success: generated + safety-passed (AC2) =====

    [Fact]
    public void Succeeded_generation_records_generated_with_the_version()
    {
        AgentInteractionState state = StateContextReady();

        DomainResult result = Generate(SucceededGenerationResult(), state);

        result.IsSuccess.ShouldBeTrue();
        AgentOutputGenerated generated = result.Events[0].ShouldBeOfType<AgentOutputGenerated>();
        generated.AgentInteractionId.ShouldBe(InteractionId);
        generated.Version.Kind.ShouldBe(AgentGenerationKind.Generated);
        generated.Version.VersionId.ShouldBe($"version-{GenerationAttemptId}"); // derived deterministically from the attempt id (AD-13)
        generated.Version.AttemptId.ShouldBe(GenerationAttemptId);
        generated.Version.GeneratedContent.ShouldBe(GeneratedContentText);
        generated.Version.ProviderId.ShouldBe(SampleSnapshot.ProviderId);
        generated.Version.ModelId.ShouldBe(SampleSnapshot.ModelId);
        generated.Version.ContentSafetyPolicyVersion.ShouldBe(ContentSafetyPolicyVersion);
        generated.Version.PromptTokenCount.ShouldBe(PromptTokenCount);
        generated.Version.OutputTokenCount.ShouldBe(OutputTokenCount);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.Generated);
        state.GeneratedVersions.ShouldNotBeNull().ShouldHaveSingleItem().VersionId.ShouldBe($"version-{GenerationAttemptId}");
        state.GenerationFailureReason.ShouldBeNull();
    }

    // ===== Safety block: SafetyFailed, NO version, NO content leak (AC2, AC3; AD-5, AD-14) =====

    [Fact]
    public void Content_safety_blocked_records_safety_failed_and_never_emits_a_version_or_the_content()
    {
        AgentInteractionState state = StateContextReady();

        // The result DELIBERATELY still carries the unsafe content — the policy must drop it and emit only safe evidence.
        DomainResult result = Generate(SafetyBlockedGenerationResult(), state);

        result.IsSuccess.ShouldBeTrue();
        AgentOutputGenerationFailed failed = result.Events[0].ShouldBeOfType<AgentOutputGenerationFailed>();
        failed.AgentInteractionId.ShouldBe(InteractionId);
        failed.Decision.ShouldBe(AgentInteractionStatus.SafetyFailed);
        failed.Reason.ShouldBe(AgentOutputGenerationFailureReason.ContentSafetyBlocked);
        failed.Evidence.AttemptId.ShouldBe(GenerationAttemptId);

        // Nothing approvable on a safety failure (AD-5), and the unsafe content never rides on the failure event (AD-14).
        result.Events.OfType<AgentOutputGenerated>().ShouldBeEmpty();
        JsonSerializer.Serialize(failed).ShouldNotContain(GeneratedContentText);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.SafetyFailed);
        state.GeneratedVersions.ShouldBeNull();
        state.GenerationFailureReason.ShouldBe(AgentOutputGenerationFailureReason.ContentSafetyBlocked);
    }

    // ===== Every other failure outcome → GenerationFailed with the mapped reason (AC3) =====

    [Theory]
    [InlineData(AgentGenerationOutcome.ProviderTimeout, AgentOutputGenerationFailureReason.ProviderTimeout)]
    [InlineData(AgentGenerationOutcome.ProviderDisabled, AgentOutputGenerationFailureReason.ProviderDisabled)]
    [InlineData(AgentGenerationOutcome.ProviderUnavailable, AgentOutputGenerationFailureReason.ProviderUnavailable)]
    [InlineData(AgentGenerationOutcome.AdapterFailure, AgentOutputGenerationFailureReason.AdapterFailure)]
    [InlineData(AgentGenerationOutcome.InvalidContext, AgentOutputGenerationFailureReason.InvalidContext)]
    [InlineData(AgentGenerationOutcome.GenerationError, AgentOutputGenerationFailureReason.GenerationError)]
    [InlineData(AgentGenerationOutcome.PolicyFailure, AgentOutputGenerationFailureReason.PolicyFailure)]
    [InlineData(AgentGenerationOutcome.Unknown, AgentOutputGenerationFailureReason.GenerationError)] // an unmapped/garbage outcome fails closed to the generic reason (AD-12)
    public void Each_failure_outcome_records_generation_failed_with_the_mapped_reason(
        AgentGenerationOutcome outcome,
        AgentOutputGenerationFailureReason expected)
    {
        AgentInteractionState state = StateContextReady();

        DomainResult result = Generate(GenerationResult(outcome), state);

        AgentOutputGenerationFailed failed = result.Events[0].ShouldBeOfType<AgentOutputGenerationFailed>();
        failed.Decision.ShouldBe(AgentInteractionStatus.GenerationFailed);
        failed.Reason.ShouldBe(expected);
        result.Events.OfType<AgentOutputGenerated>().ShouldBeEmpty();

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        state.GenerationFailureReason.ShouldBe(expected);
        state.GeneratedVersions.ShouldBeNull();
    }

    // ===== Not generatable: not requested / context not ready (AD-11, AD-12) =====

    [Fact]
    public void Generation_on_a_never_requested_interaction_is_not_generatable_interaction_not_requested()
    {
        DomainResult result = Generate(SucceededGenerationResult(), state: null);

        AssertNotGeneratable(result, AgentOutputNotGeneratableReason.InteractionNotRequested);
    }

    [Fact]
    public void Generation_on_a_rejection_only_stream_is_not_generatable_interaction_not_requested()
    {
        DomainResult result = Generate(SucceededGenerationResult(), new AgentInteractionState());

        AssertNotGeneratable(result, AgentOutputNotGeneratableReason.InteractionNotRequested);
    }

    [Fact]
    public void Generation_on_a_requested_but_context_not_ready_interaction_is_not_generatable_context_not_ready()
    {
        // Status is Requested (context not yet built) — generation must never run before context is ready (AD-11).
        DomainResult result = Generate(SucceededGenerationResult(), StateRequested());

        AssertNotGeneratable(result, AgentOutputNotGeneratableReason.ContextNotReady);
    }

    [Fact]
    public void Generation_on_an_authorized_but_context_not_ready_interaction_is_not_generatable_context_not_ready()
    {
        // Status is Authorized (gate passed, context not built) — still not generatable (AD-11).
        DomainResult result = Generate(SucceededGenerationResult(), StateAuthorized());

        AssertNotGeneratable(result, AgentOutputNotGeneratableReason.ContextNotReady);
    }

    [Fact]
    public void Generation_on_a_context_blocked_interaction_is_not_generatable_context_not_ready()
    {
        // Status is ContextBlocked (context build failed its bounds) — a generate command on this adjacent, non-terminal
        // status is a structural rejection (no state change), distinct from a recorded GenerationFailed decision (AD-11).
        DomainResult result = Generate(SucceededGenerationResult(), StateContextBlocked());

        AssertNotGeneratable(result, AgentOutputNotGeneratableReason.ContextNotReady);
    }

    // ===== Idempotent terminal determinism (AD-13, AC4) — no decision flip, no duplicate version =====

    [Fact]
    public void Re_generate_after_generated_is_a_noop_and_preserves_version_history()
    {
        AgentInteractionState state = StateContextReady();
        ApplyAll(state, Generate(SucceededGenerationResult(), state)); // now Generated with one version
        state.Status.ShouldBe(AgentInteractionStatus.Generated);

        // Re-dispatching a generate command that WOULD fail must be a clean no-op — no flip, no duplicate version (AC4).
        DomainResult reissue = Generate(ProviderTimeoutGenerationResult(), state);

        reissue.IsNoOp.ShouldBeTrue();
        reissue.Events.ShouldBeEmpty();
        state.Status.ShouldBe(AgentInteractionStatus.Generated);
        state.GeneratedVersions.ShouldNotBeNull().Count.ShouldBe(1);
    }

    [Fact]
    public void Re_generate_after_generation_failed_is_a_noop_and_never_flips()
    {
        AgentInteractionState state = StateContextReady();
        ApplyAll(state, Generate(ProviderTimeoutGenerationResult(), state)); // now GenerationFailed
        state.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);

        DomainResult reissue = Generate(SucceededGenerationResult(), state);

        reissue.IsNoOp.ShouldBeTrue();
        state.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        state.GeneratedVersions.ShouldBeNull();
    }

    [Fact]
    public void Re_generate_after_safety_failed_is_a_noop_and_never_flips()
    {
        AgentInteractionState state = StateContextReady();
        ApplyAll(state, Generate(SafetyBlockedGenerationResult(), state)); // now SafetyFailed
        state.Status.ShouldBe(AgentInteractionStatus.SafetyFailed);

        DomainResult reissue = Generate(SucceededGenerationResult(), state);

        reissue.IsNoOp.ShouldBeTrue();
        state.Status.ShouldBe(AgentInteractionStatus.SafetyFailed);
        state.GeneratedVersions.ShouldBeNull();
    }

    // ===== Decide / Evaluate no-drift =====

    [Theory]
    [InlineData(AgentGenerationOutcome.Succeeded, AgentInteractionStatus.Generated)]
    [InlineData(AgentGenerationOutcome.ContentSafetyBlocked, AgentInteractionStatus.SafetyFailed)]
    [InlineData(AgentGenerationOutcome.ProviderTimeout, AgentInteractionStatus.GenerationFailed)]
    [InlineData(AgentGenerationOutcome.ProviderDisabled, AgentInteractionStatus.GenerationFailed)]
    [InlineData(AgentGenerationOutcome.ProviderUnavailable, AgentInteractionStatus.GenerationFailed)]
    [InlineData(AgentGenerationOutcome.AdapterFailure, AgentInteractionStatus.GenerationFailed)]
    [InlineData(AgentGenerationOutcome.InvalidContext, AgentInteractionStatus.GenerationFailed)]
    [InlineData(AgentGenerationOutcome.GenerationError, AgentInteractionStatus.GenerationFailed)]
    [InlineData(AgentGenerationOutcome.PolicyFailure, AgentInteractionStatus.GenerationFailed)]
    [InlineData(AgentGenerationOutcome.Unknown, AgentInteractionStatus.GenerationFailed)] // an unmapped/garbage outcome fails closed (never Generated) — Decide/Evaluate still agree
    public void Decide_matches_the_aggregate_recorded_decision_for_each_outcome(AgentGenerationOutcome outcome, AgentInteractionStatus expected)
    {
        AgentOutputGenerationResult result = outcome == AgentGenerationOutcome.ContentSafetyBlocked
            ? SafetyBlockedGenerationResult()
            : GenerationResult(outcome);

        AgentOutputGenerationPolicy.Decide(result).ShouldBe(expected);

        DomainResult domainResult = Generate(result, StateContextReady());
        AgentInteractionStatus recorded = domainResult.Events[0] switch
        {
            AgentOutputGenerated => AgentInteractionStatus.Generated,
            AgentOutputGenerationFailed failed => failed.Decision,
            _ => AgentInteractionStatus.Unknown,
        };
        recorded.ShouldBe(expected);
    }

    // ===== Full pipeline: reflection dispatch + JSON round-trip =====

    [Fact]
    public async Task Process_async_round_trips_the_generate_command_and_records_generated()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateContextReady();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));

        result.IsSuccess.ShouldBeTrue();
        AgentOutputGenerated generated = result.Events[0].ShouldBeOfType<AgentOutputGenerated>();
        generated.Version.GeneratedContent.ShouldBe(GeneratedContentText); // survived the JSON round-trip
        state.Status.ShouldBe(AgentInteractionStatus.Generated);
    }

    [Fact]
    public async Task Process_async_round_trips_a_failing_generate_command_and_records_the_failure()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateContextReady();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SafetyBlockedGenerationResult()));

        AgentOutputGenerationFailed failed = result.Events[0].ShouldBeOfType<AgentOutputGenerationFailed>();
        failed.Decision.ShouldBe(AgentInteractionStatus.SafetyFailed);
        failed.Reason.ShouldBe(AgentOutputGenerationFailureReason.ContentSafetyBlocked); // survived the round-trip by name
        state.Status.ShouldBe(AgentInteractionStatus.SafetyFailed);
    }

    // ===== Helpers =====

    private static DomainResult Generate(AgentOutputGenerationResult result, AgentInteractionState? state)
    {
        GenerateAgentOutput command = GenerateCommand(result);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static void AssertNotGeneratable(DomainResult result, AgentOutputNotGeneratableReason expected)
    {
        result.IsRejection.ShouldBeTrue();
        AgentOutputNotGeneratableRejection rejection = result.Events[0].ShouldBeOfType<AgentOutputNotGeneratableRejection>();
        rejection.Reason.ShouldBe(expected);
        rejection.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.OfType<AgentOutputGenerated>().ShouldBeEmpty();
        result.Events.OfType<AgentOutputGenerationFailed>().ShouldBeEmpty();
    }
}
