using System.Text;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentInteractionTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Handle-method tests for <see cref="AgentInteractionAggregate"/> (Story 2.1; AC1–AC4). Cover request creation with
/// the full AD-4 snapshot, structural-validation rejections with safe classifications (AC1, AC4), deterministic
/// idempotency (exact-duplicate no-op vs conflicting-payload rejection; AC2/AD-13), sensitive-content non-disclosure
/// on rejections (AC4/AD-14), and the full reflection-dispatch + JSON round-trip through <c>ProcessAsync</c>.
/// </summary>
public sealed class AgentInteractionAggregateTests
{
    // ===== Request creation (AC1) =====

    [Fact]
    public void Request_with_no_state_produces_interaction_requested_with_full_snapshot()
    {
        RequestAgentInteraction command = ValidRequest();

        DomainResult result = AgentInteractionAggregate.Handle(command, state: null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        InteractionRequested requested = result.Events[0].ShouldBeOfType<InteractionRequested>();
        requested.AgentInteractionId.ShouldBe(InteractionId);
        requested.AgentId.ShouldBe(AgentId);
        requested.CallerPartyId.ShouldBe(CallerPartyId);
        requested.SourceConversationId.ShouldBe(SourceConversationId);
        requested.Prompt.ShouldBe(Prompt);
        requested.IdempotencyKey.ShouldBe(IdempotencyKey);
        requested.Snapshot.ShouldBe(SampleSnapshot);
        requested.Snapshot.ContextPolicyReference.ShouldBe(AgentInteractionSnapshot.DefaultContextPolicyReference);
    }

    [Fact]
    public void Request_with_no_state_records_requested_state_with_the_prompt_captured()
    {
        RequestAgentInteraction command = ValidRequest();

        DomainResult result = AgentInteractionAggregate.Handle(command, state: null, Envelope(command));
        var state = new AgentInteractionState();
        ApplyAll(state, result);

        state.IsRequested.ShouldBeTrue();
        state.Prompt.ShouldBe(Prompt); // the durable state is the only safe home for the prompt (AD-14)
        state.Snapshot.ShouldBe(SampleSnapshot);
    }

    // ===== Structural validation (AC1, AC4) =====

    [Fact]
    public void Request_with_missing_caller_produces_invalid_missing_caller()
        => AssertInvalid(ValidRequest(callerPartyId: "   "), AgentInteractionRequestValidationStatus.MissingCaller);

    [Fact]
    public void Request_with_missing_source_conversation_produces_invalid_missing_source()
        => AssertInvalid(ValidRequest(sourceConversationId: ""), AgentInteractionRequestValidationStatus.MissingSourceConversation);

    [Fact]
    public void Request_with_missing_prompt_produces_invalid_missing_prompt()
        => AssertInvalid(ValidRequest(prompt: "   "), AgentInteractionRequestValidationStatus.MissingPrompt);

    [Fact]
    public void Request_with_absent_snapshot_produces_invalid_missing_snapshot()
        => AssertInvalid(ValidRequest() with { Snapshot = null }, AgentInteractionRequestValidationStatus.MissingAgentSnapshot);

    [Fact]
    public void Request_with_empty_snapshot_scalar_produces_invalid_missing_snapshot()
        => AssertInvalid(
            ValidRequest() with { Snapshot = SampleSnapshot with { ProviderId = "" } },
            AgentInteractionRequestValidationStatus.MissingAgentSnapshot);

    [Fact]
    public void Request_with_empty_snapshot_model_id_produces_invalid_missing_snapshot()
        => AssertInvalid(
            ValidRequest() with { Snapshot = SampleSnapshot with { ModelId = "" } },
            AgentInteractionRequestValidationStatus.MissingAgentSnapshot);

    [Fact]
    public void Request_with_blank_snapshot_context_policy_reference_produces_invalid_missing_snapshot()
        => AssertInvalid(
            ValidRequest() with { Snapshot = SampleSnapshot with { ContextPolicyReference = "   " } },
            AgentInteractionRequestValidationStatus.MissingAgentSnapshot);

    [Fact]
    public void Request_with_blank_agent_id_produces_invalid_missing_snapshot()
        // A forged/empty target Agent yields no resolvable snapshot — HasUsableSnapshot guards the agent id too.
        => AssertInvalid(
            ValidRequest(agentId: "  "),
            AgentInteractionRequestValidationStatus.MissingAgentSnapshot);

    [Fact]
    public void Validation_precedence_reports_caller_before_other_missing_fields()
        // Deterministic order: caller → source → prompt → snapshot. With everything missing, MissingCaller wins.
        => AssertInvalid(
            ValidRequest(callerPartyId: "", sourceConversationId: "", prompt: "") with { Snapshot = null },
            AgentInteractionRequestValidationStatus.MissingCaller);

    [Fact]
    public void Invalid_rejection_does_not_echo_the_prompt_or_caller_content()
    {
        RequestAgentInteraction command = ValidRequest() with { Snapshot = null };

        DomainResult result = AgentInteractionAggregate.Handle(command, state: null, Envelope(command));

        InvalidAgentInteractionRequestRejection rejection =
            result.Events[0].ShouldBeOfType<InvalidAgentInteractionRequestRejection>();
        // AD-14: the rejection carries only the safe id + classification — never the prompt or caller reference.
        string json = Encoding.UTF8.GetString(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(rejection));
        json.ShouldNotContain(Prompt);
        json.ShouldNotContain(CallerPartyId);
        rejection.AgentInteractionId.ShouldBe(InteractionId);
    }

    // ===== Idempotency (AC2; AD-13) =====

    [Fact]
    public void Request_exact_duplicate_produces_noop()
    {
        RequestAgentInteraction command = ValidRequest();
        AgentInteractionState state = StateWith(command);

        DomainResult result = AgentInteractionAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Request_conflicting_prompt_on_same_id_produces_already_requested_and_no_mutation()
    {
        AgentInteractionState state = StateWith(ValidRequest());
        RequestAgentInteraction conflicting = ValidRequest(prompt: "An entirely different instruction for the agent.");

        DomainResult result = AgentInteractionAggregate.Handle(conflicting, state, Envelope(conflicting));

        result.IsRejection.ShouldBeTrue();
        AgentInteractionAlreadyRequestedRejection rejection =
            result.Events[0].ShouldBeOfType<AgentInteractionAlreadyRequestedRejection>();
        rejection.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.ShouldNotContain(e => e is InteractionRequested);
    }

    [Fact]
    public void Request_conflicting_snapshot_on_same_id_produces_already_requested()
    {
        AgentInteractionState state = StateWith(ValidRequest());
        RequestAgentInteraction conflicting = ValidRequest() with { Snapshot = SampleSnapshot with { ConfigurationVersion = 99 } };

        DomainResult result = AgentInteractionAggregate.Handle(conflicting, state, Envelope(conflicting));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentInteractionAlreadyRequestedRejection>();
    }

    // Exhaustively exercises the by-field snapshot comparison (SnapshotsEqual): a re-issue under the same
    // deterministic id whose server-assembled snapshot differs on ANY scalar (the Agent config changed between
    // calls) must conflict, never silently no-op (AD-13). ConfigurationVersion has its own fact above; this Theory
    // guards the remaining eight fields so a dropped comparison cannot pass unnoticed.
    [Theory]
    [InlineData("InstructionsVersion")]
    [InlineData("ResponseMode")]
    [InlineData("ApproverPolicyVersion")]
    [InlineData("ProviderId")]
    [InlineData("ModelId")]
    [InlineData("ProviderCapabilityVersion")]
    [InlineData("ContentSafetyPolicyVersion")]
    [InlineData("ContextPolicyReference")]
    public void Request_conflicting_snapshot_scalar_on_same_id_produces_already_requested(string changedField)
    {
        AgentInteractionState state = StateWith(ValidRequest());
        RequestAgentInteraction conflicting = ValidRequest() with { Snapshot = MutateSnapshot(SampleSnapshot, changedField) };

        DomainResult result = AgentInteractionAggregate.Handle(conflicting, state, Envelope(conflicting));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentInteractionAlreadyRequestedRejection>();
        result.Events.ShouldNotContain(e => e is InteractionRequested);
    }

    [Fact]
    public void Request_with_identical_snapshot_but_differing_response_mode_is_not_treated_as_a_duplicate()
    {
        // SampleSnapshot is Automatic; the conflict Theory above relies on Confirmation differing — pin that here so
        // the chosen mutation can never silently equal the recorded value (which would make the Theory vacuous).
        SampleSnapshot.ResponseMode.ShouldBe(AgentResponseMode.Automatic);
        AgentResponseMode.Confirmation.ShouldNotBe(SampleSnapshot.ResponseMode);
    }

    // ===== Full pipeline: reflection dispatch + JSON round-trip =====

    [Fact]
    public async Task Process_async_round_trips_the_command_and_records_the_request()
    {
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, ValidRequest());

        result.IsSuccess.ShouldBeTrue();
        state.IsRequested.ShouldBeTrue();
        state.AgentId.ShouldBe(AgentId);
        state.Prompt.ShouldBe(Prompt);
        state.Snapshot.ShouldNotBeNull();
        state.Snapshot!.ProviderId.ShouldBe("openai");
        state.Snapshot.ModelId.ShouldBe("gpt-4o");
    }

    [Fact]
    public async Task Process_async_re_issue_of_the_same_request_is_a_noop_through_the_full_pipeline()
    {
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        _ = await ProcessAndApplyAsync(aggregate, state, ValidRequest());
        DomainResult reissue = await ProcessAndApplyAsync(aggregate, state, ValidRequest());

        reissue.IsNoOp.ShouldBeTrue();
        reissue.Events.ShouldBeEmpty();
    }

    private static void AssertInvalid(RequestAgentInteraction command, AgentInteractionRequestValidationStatus expected)
    {
        DomainResult result = AgentInteractionAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        InvalidAgentInteractionRequestRejection rejection =
            result.Events[0].ShouldBeOfType<InvalidAgentInteractionRequestRejection>();
        rejection.Status.ShouldBe(expected);
        rejection.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.ShouldNotContain(e => e is InteractionRequested);
    }

    // Returns a copy of the snapshot with exactly one scalar changed to a value distinct from the original, so the
    // by-field comparison must observe the difference.
    private static AgentInteractionSnapshot MutateSnapshot(AgentInteractionSnapshot snapshot, string field) => field switch
    {
        "InstructionsVersion" => snapshot with { InstructionsVersion = snapshot.InstructionsVersion + 1 },
        "ResponseMode" => snapshot with { ResponseMode = AgentResponseMode.Confirmation },
        "ApproverPolicyVersion" => snapshot with { ApproverPolicyVersion = snapshot.ApproverPolicyVersion + 1 },
        "ProviderId" => snapshot with { ProviderId = "anthropic" },
        "ModelId" => snapshot with { ModelId = "claude-opus-4-8" },
        "ProviderCapabilityVersion" => snapshot with { ProviderCapabilityVersion = snapshot.ProviderCapabilityVersion + 1 },
        "ContentSafetyPolicyVersion" => snapshot with { ContentSafetyPolicyVersion = snapshot.ContentSafetyPolicyVersion + 1 },
        "ContextPolicyReference" => snapshot with { ContextPolicyReference = "different-context-policy-v2" },
        _ => throw new System.ArgumentOutOfRangeException(nameof(field), field, "Unknown snapshot field selector."),
    };
}
