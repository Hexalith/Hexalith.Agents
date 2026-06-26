namespace Hexalith.Agents.Server.Tests;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>
/// Tests for <see cref="AgentInteractionGenerationOrchestrator"/> (Story 2.4; AC1–AC4; AD-3, AD-5, AD-9, AD-13, AD-14).
/// Verify the happy path dispatches <see cref="GenerateAgentOutput"/> with the right domain/aggregate id and returns
/// <c>Generated</c>; that a safety block returns <c>SafetyFailed</c> with NO content in the dispatched envelope or result;
/// that a provider throw fails closed to <c>GenerationFailed</c> without leaking the raw error; that disabled / not-found /
/// timeout / re-read failure / safety-policy-not-available each map correctly; that the all-deferred default graph fails
/// closed; that the dispatched command drives the real aggregate to the same decision; and that a genuine cancellation
/// propagates without dispatching.
/// </summary>
public sealed class AgentInteractionGenerationOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentInteractionId = "interaction-001";
    private const string AgentId = "hexa";
    private const string SourceConversationId = "conversation-001";
    private const string ActorUserId = "caller-user";
    private const string ProviderId = "openai";
    private const string ModelId = "gpt-4o";
    private const int ContextWindowTokenLimit = 128_000;
    private const int MaxOutputTokenLimit = 16_000;
    private const string GeneratedContentText = "the-generated-answer-do-not-leak";
    private const string SourceConversationText = "source-conversation-secret-body-do-not-leak";

    private static readonly DateTimeOffset _sampleCreatedAt = new(2026, 6, 24, 10, 0, 0, TimeSpan.Zero);

    private readonly IConversationContextReader _contextReader = Substitute.For<IConversationContextReader>();
    private readonly IProviderCatalogReader _providerCatalogReader = Substitute.For<IProviderCatalogReader>();
    private readonly IAgentGenerationProvider _generationProvider = Substitute.For<IAgentGenerationProvider>();
    private readonly IAgentContentSafetyPolicyReader _policyReader = Substitute.For<IAgentContentSafetyPolicyReader>();
    private readonly IContentSafetyEvaluator _safetyEvaluator = Substitute.For<IContentSafetyEvaluator>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private CommandEnvelope? _lastDispatched;

    private AgentInteractionGenerationOrchestrator Orchestrator => new(
        _contextReader,
        _providerCatalogReader,
        _generationProvider,
        _policyReader,
        _safetyEvaluator,
        _dispatcher);

    // ===== Happy path → Generated (AC1, AC2) =====

    [Fact]
    public async Task Succeeded_generation_dispatches_generate_and_returns_generated()
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.AgentInteractionId.ShouldBe(AgentInteractionId);
        outcome.Status.ShouldBe(AgentInteractionStatus.Generated);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(GenerateAgentOutput));
        dispatched.Domain.ShouldBe("agent-interaction");
        dispatched.AggregateId.ShouldBe(AgentInteractionId);

        AgentOutputGenerationResult result = DispatchedResult();
        result.Outcome.ShouldBe(AgentGenerationOutcome.Succeeded);
        result.GeneratedContent.ShouldBe(GeneratedContentText);
        result.AttemptId.ShouldBe($"attempt-{AgentInteractionId}"); // deterministic from the interaction id (AD-13)
    }

    // ===== Safety block → SafetyFailed, NO content anywhere (AC2; AD-5, AD-14) =====

    [Fact]
    public async Task Content_safety_blocked_returns_safety_failed_with_no_content_in_the_envelope_or_result()
    {
        StubHappy();
        _safetyEvaluator.EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ContentSafetyEvaluationResult(ContentSafetyVerdict.Blocked, "violence"));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.SafetyFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ContentSafetyBlocked);
        DispatchedResult().GeneratedContent.ShouldBeNull();

        // AD-14: the unsafe content appears nowhere on the dispatched envelope or the returned outcome.
        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(GeneratedContentText);
        JsonSerializer.Serialize(outcome).ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public async Task A_safety_evaluator_that_throws_fails_closed_to_safety_failed_without_leaking_content_or_the_error()
    {
        // An evaluator that cannot clear the content blocks it (fail closed) — the generation is recorded SafetyFailed,
        // with neither the generated content nor the raw evaluator error crossing the boundary (AD-12, AD-14).
        StubHappy();
        _safetyEvaluator.EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("safety engine blew up: secret-bearing detail"));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.SafetyFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ContentSafetyBlocked);
        DispatchedResult().GeneratedContent.ShouldBeNull();
        string payload = Encoding.UTF8.GetString(LastDispatched()!.Payload);
        payload.ShouldNotContain(GeneratedContentText);
        payload.ShouldNotContain("secret-bearing detail");
    }

    // ===== Provider throw → fail closed, no raw error (AC3; AD-9, AD-14) =====

    [Fact]
    public async Task A_provider_that_throws_fails_closed_to_generation_failed_without_leaking_the_error()
    {
        StubLoadedConversation();
        StubEnabledCatalog();
        _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("provider sdk blew up: secret-bearing detail"));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.AdapterFailure);
        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain("secret-bearing detail");
        JsonSerializer.Serialize(outcome).ShouldNotContain("secret-bearing detail");
    }

    // ===== Provider disabled / not-found / timeout (AC3) =====

    [Fact]
    public async Task A_disabled_provider_entry_returns_generation_failed_provider_disabled_without_invoking_the_provider()
    {
        StubLoadedConversation();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, Entry(ProviderModelStatus.Disabled, supportsTextGeneration: true)));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ProviderDisabled);
        await _generationProvider.DidNotReceive().GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_non_text_capable_provider_entry_returns_generation_failed_provider_disabled()
    {
        StubLoadedConversation();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, Entry(ProviderModelStatus.Enabled, supportsTextGeneration: false)));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ProviderDisabled);
    }

    [Fact]
    public async Task A_not_found_catalog_entry_returns_generation_failed_provider_unavailable()
    {
        StubLoadedConversation();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ProviderUnavailable);
    }

    [Fact]
    public async Task A_catalog_reader_that_throws_fails_closed_to_provider_unavailable()
    {
        StubLoadedConversation();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("catalog projection blew up"));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ProviderUnavailable);
    }

    [Fact]
    public async Task An_empty_provider_id_returns_provider_unavailable_without_reading_the_catalog_or_invoking_the_provider()
    {
        // A request with no provider identity fails closed before any catalog read or model invocation (no degraded call).
        StubLoadedConversation();
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(providerId: ""), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ProviderUnavailable);
        await _providerCatalogReader.DidNotReceive().GetEntryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _generationProvider.DidNotReceive().GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_provider_timeout_outcome_returns_generation_failed_provider_timeout()
    {
        StubLoadedConversation();
        StubEnabledCatalog();
        _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentGenerationProviderResult(AgentGenerationOutcome.ProviderTimeout, null, 0, 0));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ProviderTimeout);
    }

    [Fact]
    public async Task A_provider_generation_error_outcome_returns_generation_failed_generation_error()
    {
        StubLoadedConversation();
        StubEnabledCatalog();
        _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentGenerationProviderResult(AgentGenerationOutcome.GenerationError, null, 0, 0));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.GenerationError);
    }

    [Fact]
    public async Task A_provider_success_with_no_content_returns_generation_failed_generation_error_and_skips_safety()
    {
        // A "Succeeded" provider result with NO content is a degraded result — it must fail closed to GenerationError and
        // never reach the content-safety gate (there is nothing to evaluate), let alone be recorded as an approvable version.
        StubLoadedConversation();
        StubEnabledCatalog();
        _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentGenerationProviderResult(AgentGenerationOutcome.Succeeded, null, 1_200, 0));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.GenerationError);
        DispatchedResult().GeneratedContent.ShouldBeNull();
        await _safetyEvaluator.DidNotReceive().EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>());
    }

    // ===== Conversation re-read failure → InvalidContext (AC3) =====

    [Fact]
    public async Task A_conversation_re_read_failure_returns_generation_failed_invalid_context_without_invoking_the_provider()
    {
        _contextReader.ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, Arg.Any<CancellationToken>())
            .Returns(ConversationContextReadResult.Unavailable);
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.InvalidContext);
        await _generationProvider.DidNotReceive().GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_context_reader_that_throws_fails_closed_to_invalid_context()
    {
        _contextReader.ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("conversations transport blew up: secret"));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.InvalidContext);
        JsonSerializer.Serialize(DispatchedResult()).ShouldNotContain("secret");
    }

    // ===== Safety policy not available → PolicyFailure (AC2, AC3) =====

    [Fact]
    public async Task A_not_available_safety_policy_returns_generation_failed_policy_failure_and_dispatches_no_content()
    {
        StubLoadedConversation();
        StubEnabledCatalog();
        _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentGenerationProviderResult(AgentGenerationOutcome.Succeeded, GeneratedContentText, 1_200, 350));
        _policyReader.ReadAsync(TenantId, AgentId, Arg.Any<AgentResponseMode>(), Arg.Any<CancellationToken>())
            .Returns(AgentContentSafetyPolicyReadResult.NotAvailable);
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.PolicyFailure);
        DispatchedResult().GeneratedContent.ShouldBeNull();
        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(GeneratedContentText);
        // Safety evaluation is never attempted without a resolved policy (fail closed).
        await _safetyEvaluator.DidNotReceive().EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_safety_policy_reader_that_throws_fails_closed_to_policy_failure_without_evaluating_or_leaking()
    {
        // A policy that cannot be read drives PolicyFailure (never a skipped safety gate); no content and no raw error
        // cross the boundary, and the evaluator is never reached without a resolved policy (AD-12, AD-14).
        StubLoadedConversation();
        StubEnabledCatalog();
        _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentGenerationProviderResult(AgentGenerationOutcome.Succeeded, GeneratedContentText, 1_200, 350));
        _policyReader.ReadAsync(TenantId, AgentId, Arg.Any<AgentResponseMode>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("agent read-model projection blew up: secret"));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.PolicyFailure);
        DispatchedResult().GeneratedContent.ShouldBeNull();
        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(GeneratedContentText);
        JsonSerializer.Serialize(DispatchedResult()).ShouldNotContain("secret");
        await _safetyEvaluator.DidNotReceive().EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>());
    }

    // ===== All-deferred default graph fails closed (AC1; FR-21) =====

    [Fact]
    public async Task All_deferred_defaults_fail_closed_to_generation_failed()
    {
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        CommandEnvelope? captured = null;
        _ = dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => captured = e), Arg.Any<CancellationToken>());

        var orchestrator = new AgentInteractionGenerationOrchestrator(
            new DeferredConversationContextReader(),
            new DeferredProviderCatalogReader(),
            new DeferredAgentGenerationProvider(),
            new DeferredAgentContentSafetyPolicyReader(),
            new DeferredContentSafetyEvaluator(),
            dispatcher);

        AgentInteractionGenerationOutcomeResult outcome = await orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        await dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
        var command = JsonSerializer.Deserialize<GenerateAgentOutput>(captured!.Payload)!;
        // The first deferred seam on the path is the conversation re-read → InvalidContext.
        command.Result.Outcome.ShouldBe(AgentGenerationOutcome.InvalidContext);
        command.Result.GeneratedContent.ShouldBeNull();
    }

    [Fact]
    public async Task The_deferred_provider_fails_closed_to_generation_failed_provider_unavailable()
    {
        // With the conversation loaded and the catalog enabled, the default deferred provider blocks content-bearing
        // generation safely (AD-14): ProviderUnavailable → GenerationFailed — the provider SDK never enters the graph.
        StubLoadedConversation();
        StubEnabledCatalog();
        CaptureDispatch();

        var orchestrator = new AgentInteractionGenerationOrchestrator(
            _contextReader,
            _providerCatalogReader,
            new DeferredAgentGenerationProvider(),
            new DeferredAgentContentSafetyPolicyReader(),
            new DeferredContentSafetyEvaluator(),
            _dispatcher);

        AgentInteractionGenerationOutcomeResult outcome = await orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ProviderUnavailable);
    }

    // ===== Trust model + envelope scope (AC3) =====

    [Fact]
    public async Task Strips_client_forged_reserved_extensions_and_preserves_benign_ones()
    {
        StubHappy();
        CaptureDispatch();

        var clientExtensions = new Dictionary<string, string>
        {
            ["actor:agentsAdmin"] = "true",
            ["provider:selectionValidation"] = "Valid",
            ["approver:policyValidation"] = "Valid",
            ["party:linkValidation"] = "Valid",
            ["trace"] = "abc-123",
        };

        await Orchestrator.ExecuteAsync(Request(clientExtensions: clientExtensions), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions.ShouldNotContainKey("actor:agentsAdmin");
        dispatched.Extensions.ShouldNotContainKey("provider:selectionValidation");
        dispatched.Extensions.ShouldNotContainKey("approver:policyValidation");
        dispatched.Extensions.ShouldNotContainKey("party:linkValidation");
        dispatched.Extensions["trace"].ShouldBe("abc-123");
    }

    [Fact]
    public async Task Builds_the_envelope_with_the_trusted_scope_from_the_request()
    {
        StubHappy();
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.TenantId.ShouldBe(TenantId);
        dispatched.MessageId.ShouldBe("msg-1");
        dispatched.CorrelationId.ShouldBe("corr-1");
        dispatched.UserId.ShouldBe(ActorUserId);
        dispatched.CausationId.ShouldBeNull();
    }

    // ===== No side effects: only reads + the provider invocation + one dispatch (AC3) =====

    [Fact]
    public async Task Only_reads_invokes_the_provider_and_dispatches_once_no_conversation_post()
    {
        StubHappy();
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        // The orchestrator's only collaborators are read ports + the provider adapter + the command dispatcher — there is
        // structurally no IConversationClient.AppendMessageAsync and no proposal creation on this path (Story 2.5/3.1).
        await _contextReader.Received(1).ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, Arg.Any<CancellationToken>());
        await _generationProvider.Received(1).GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
        LastDispatched().ShouldNotBeNull().CommandType.ShouldBe(nameof(GenerateAgentOutput));
    }

    // ===== Raw source conversation text never crosses the boundary (AD-14) =====

    [Fact]
    public async Task The_dispatched_command_carries_no_raw_source_conversation_text()
    {
        StubHappy(); // the loaded messages carry SourceConversationText, used only transiently to build the model input
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(SourceConversationText);
    }

    // ===== Cross-seam: the dispatched command drives the real aggregate to the same decision =====

    [Fact]
    public async Task End_to_end_the_dispatched_generate_command_drives_the_aggregate_to_the_same_decision()
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        var command = JsonSerializer.Deserialize<GenerateAgentOutput>(dispatched.Payload)!;

        var state = new AgentInteractionState();
        state.Apply(new InteractionRequested(
            AgentInteractionId, AgentId, "party-001", SourceConversationId, SampleSnapshot(), "prompt", "idem"));
        state.Apply(new AgentInteractionAuthorized(AgentInteractionId));
        state.Apply(new AgentInteractionContextReady(AgentInteractionId, SampleContextEvidence()));
        DomainResult result = AgentInteractionAggregate.Handle(command, state, dispatched);

        AgentOutputGenerated generated = result.Events[0].ShouldBeOfType<AgentOutputGenerated>();
        generated.Version.GeneratedContent.ShouldBe(GeneratedContentText);
        // The orchestrator's returned status and the aggregate's recorded decision agree (shared policy — no drift).
        outcome.Status.ShouldBe(AgentInteractionStatus.Generated);
    }

    // ===== Cancellation propagates (AC3) =====

    [Fact]
    public async Task A_genuine_cancellation_propagates_and_no_command_is_dispatched()
    {
        _contextReader.ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Orchestrator.ExecuteAsync(Request(), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Helpers =====

    private void StubHappy()
    {
        StubLoadedConversation();
        StubEnabledCatalog();
        _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentGenerationProviderResult(AgentGenerationOutcome.Succeeded, GeneratedContentText, 1_200, 350));
        _policyReader.ReadAsync(TenantId, AgentId, Arg.Any<AgentResponseMode>(), Arg.Any<CancellationToken>())
            .Returns(new AgentContentSafetyPolicyReadResult(true, SamplePolicy(), 2));
        _safetyEvaluator.EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ContentSafetyEvaluationResult(ContentSafetyVerdict.Passed, null));
    }

    private void StubLoadedConversation()
    {
        IReadOnlyList<ConversationContextMessage> messages = [new ConversationContextMessage(SourceConversationText, _sampleCreatedAt)];
        _contextReader.ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, Arg.Any<CancellationToken>())
            .Returns(new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Loaded, messages, messages.Count, true));
    }

    private void StubEnabledCatalog()
        => _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, Entry(ProviderModelStatus.Enabled, supportsTextGeneration: true)));

    private static ProviderCatalogEntryView Entry(ProviderModelStatus status, bool supportsTextGeneration)
        => new(
            ProviderId,
            ModelId,
            "OpenAI GPT-4o",
            status,
            supportsTextGeneration,
            ContextWindowTokenLimit,
            MaxOutputTokenLimit,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o",
            IsSelectableForNewActiveUse: true,
            CapabilityVersion: 1);

    private static AgentContentSafetyPolicy SamplePolicy()
        => new(
            PromptConstraints: [],
            BlockedOutputCategories: ["violence"],
            RestrictedOutputCategories: ["medical"],
            ContentSafetyFailureHandling.BlockAndAudit,
            ContentSafetyAuditTreatment.MetadataOnly);

    private static AgentInteractionSnapshot SampleSnapshot()
        => new(
            ConfigurationVersion: 3,
            InstructionsVersion: 2,
            AgentResponseMode.Automatic,
            ApproverPolicyVersion: 1,
            ProviderId,
            ModelId,
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion: 1,
            AgentInteractionSnapshot.DefaultContextPolicyReference);

    private static AgentInteractionContextEvidence SampleContextEvidence()
        => new(
            AgentInteractionContextMode.Full,
            FullContextTokenCount: 1_000,
            UsedContextTokenCount: 1_000,
            MessageCount: 3,
            ReservedOutputTokenCount: MaxOutputTokenLimit,
            ContextWindowTokenLimit,
            ProviderCapabilityVersion: 1,
            AgentInteractionSnapshot.DefaultContextPolicyReference,
            BoundedBehaviorReference: null);

    private static AgentInteractionGenerationRequest Request(
        IReadOnlyDictionary<string, string>? clientExtensions = null,
        string providerId = ProviderId,
        string modelId = ModelId)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentInteractionId,
            AgentId,
            SourceConversationId,
            providerId,
            modelId,
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion: 1,
            AgentResponseMode.Automatic,
            ActorUserId,
            ClientCorrelationId: "client-corr-1",
            clientExtensions);

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;

    private AgentOutputGenerationResult DispatchedResult()
        => JsonSerializer.Deserialize<GenerateAgentOutput>(LastDispatched()!.Payload)!.Result;
}
