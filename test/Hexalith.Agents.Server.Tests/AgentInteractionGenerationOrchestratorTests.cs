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
    private readonly IConversationContextTokenMeasurer _tokenMeasurer = Substitute.For<IConversationContextTokenMeasurer>();
    private readonly IProviderCatalogReader _providerCatalogReader = Substitute.For<IProviderCatalogReader>();
    private readonly IAgentGenerationProvider _generationProvider = Substitute.For<IAgentGenerationProvider>();
    private readonly IAgentContentSafetyPolicyReader _policyReader = Substitute.For<IAgentContentSafetyPolicyReader>();
    private readonly IContentSafetyEvaluator _safetyEvaluator = Substitute.For<IContentSafetyEvaluator>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private AgentCommandIntent? _lastDispatched;

    private AgentInteractionGenerationOrchestrator Orchestrator => new(
        _contextReader,
        _providerCatalogReader,
        _generationProvider,
        _policyReader,
        _safetyEvaluator,
        _dispatcher,
        _tokenMeasurer);

    // ===== Happy path → Generated (AC1, AC2) =====

    [Fact]
    public async Task Succeeded_generation_dispatches_generate_and_returns_generated()
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.AgentInteractionId.ShouldBe(AgentInteractionId);
        outcome.Status.ShouldBe(AgentInteractionStatus.Generated);

        AgentCommandIntent dispatched = LastDispatched().ShouldNotBeNull();
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
        StubEnabledCatalog(capabilityVersion: 5);
        _safetyEvaluator.EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ContentSafetyEvaluationResult(ContentSafetyVerdict.Blocked, "violence"));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.SafetyFailed);
        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ContentSafetyBlocked);
        DispatchedResult().ProviderCapabilityVersion.ShouldBe(5);
        DispatchedResult().PromptTokenCount.ShouldBe(1_200);
        DispatchedResult().OutputTokenCount.ShouldBe(350);
        AgentCommandIntent dispatched = LastDispatched().ShouldNotBeNull();
        var command = JsonSerializer.Deserialize<GenerateAgentOutput>(dispatched.Payload)!;
        AgentInteractionState state = ContextReadyState();
        DomainResult decision = AgentInteractionAggregate.Handle(command, state, dispatched.ToFixtureEnvelope());
        var failure = decision.Events[0].ShouldBeOfType<AgentOutputGenerationFailed>();
        state.Apply(JsonSerializer.Deserialize<AgentOutputGenerationFailed>(JsonSerializer.Serialize(failure))!);
        AgentGenerationAttemptEvidence evidence = AgentInteractionAuditInspection.GetGenerationEvidence(state, isAuthorized: true).ShouldNotBeNull();
        evidence.ProviderCapabilityVersion.ShouldBe(5);
        evidence.PromptTokenCount.ShouldBe(1_200);
        evidence.OutputTokenCount.ShouldBe(350);
        AgentInteractionAuditInspection.GetContextEvidence(state, isAuthorized: true).Evidence.ShouldNotBeNull()
            .GenerationAttemptContext.ShouldNotBeNull().Budget.ShouldNotBeNull().AvailableContextTokenBudget.ShouldBe(99_200);
        AgentInteractionAuditInspection.GetGenerationEvidence(state, isAuthorized: false).ShouldBeNull();
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
        _contextReader.ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, "corr-1", Arg.Any<CancellationToken>())
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
        _contextReader.ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, "corr-1", Arg.Any<CancellationToken>())
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
        AgentCommandIntent? captured = null;
        _ = dispatcher.DispatchAsync(Arg.Do<AgentCommandIntent>(e => captured = e), Arg.Any<CancellationToken>());

        var orchestrator = new AgentInteractionGenerationOrchestrator(
            new DeferredConversationContextReader(),
            new DeferredProviderCatalogReader(),
            new DeferredAgentGenerationProvider(),
            new DeferredAgentContentSafetyPolicyReader(),
            new DeferredContentSafetyEvaluator(),
            dispatcher);

        AgentInteractionGenerationOutcomeResult outcome = await orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        await dispatcher.Received(1).DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>());
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
            _dispatcher,
            _tokenMeasurer);

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

        AgentCommandIntent dispatched = LastDispatched().ShouldNotBeNull();
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

        AgentCommandIntent dispatched = LastDispatched().ShouldNotBeNull();
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
        await _contextReader.Received(1).ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, "corr-1", Arg.Any<CancellationToken>());
        await _generationProvider.Received(1).GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>());
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
        var entry = Entry(ProviderModelStatus.Enabled, true) with
        {
            CapabilityVersion = 3,
            ContextWindowTokenLimit = 20_000,
            TimeoutPolicy = new(600_000, 10),
        };
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, entry));
        CaptureDispatch();

        AgentInteractionGenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        AgentCommandIntent dispatched = LastDispatched().ShouldNotBeNull();
        var command = JsonSerializer.Deserialize<GenerateAgentOutput>(dispatched.Payload)!;

        AgentInteractionState state = ContextReadyState();
        DomainResult result = AgentInteractionAggregate.Handle(command, state, dispatched.ToFixtureEnvelope());

        AgentOutputGenerated generated = result.Events[0].ShouldBeOfType<AgentOutputGenerated>();
        generated.Version.GeneratedContent.ShouldBe(GeneratedContentText);
        generated.Version.ProviderCapabilityVersion.ShouldBe(3);
        DispatchedResult().ProviderCapabilityVersion.ShouldBe(3);
        state.Apply(JsonSerializer.Deserialize<AgentOutputGenerated>(JsonSerializer.Serialize(generated))!);
        var context = AgentInteractionAuditInspection.GetContextEvidence(state, isAuthorized: true).Evidence.ShouldNotBeNull();
        context.GenerationAttemptContext.ShouldNotBeNull().Budget.ShouldNotBeNull().AvailableContextTokenBudget.ShouldBe(2_000);
        string audit = JsonSerializer.Serialize(context);
        audit.ShouldNotContain(GeneratedContentText);
        audit.ShouldNotContain(SourceConversationText);
        AgentInteractionAuditInspection.GetContextEvidence(state, isAuthorized: false).Evidence.ShouldBeNull();
        await _generationProvider.Received(1).GenerateAsync(
            Arg.Is<AgentGenerationProviderRequest>(invocation => invocation.ProviderCapabilityVersion == 3
                && invocation.RequestTimeoutMilliseconds == 600_000 && invocation.MaxRetries == 10
                && invocation.MaxOutputTokenLimit == MaxOutputTokenLimit), Arg.Any<CancellationToken>());
        // The orchestrator's returned status and the aggregate's recorded decision agree (shared policy — no drift).
        outcome.Status.ShouldBe(AgentInteractionStatus.Generated);
    }

    // ===== Cancellation propagates (AC3) =====

    [Fact]
    public async Task A_genuine_cancellation_propagates_and_no_command_is_dispatched()
    {
        _contextReader.ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, "corr-1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Orchestrator.ExecuteAsync(Request(), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(GenerationCatalogRefusals.Cases), MemberType = typeof(GenerationCatalogRefusals))]
    public async Task RejectedCatalogEvidencePreventsInvocation(string defect)
    {
        StubHappy();
        ProviderCatalogEntryView entry = Entry(ProviderModelStatus.Enabled, true) with { CapabilityVersion = 2 };
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, GenerationCatalogRefusals.Apply(defect, entry)));
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request() with { ProviderCapabilityVersion = 2 }, CancellationToken.None);

        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ProviderUnavailable);
        DispatchedResult().GeneratedContent.ShouldBeNull();
        using JsonDocument payload = JsonDocument.Parse(LastDispatched().ShouldNotBeNull().Payload);
        payload.RootElement.GetProperty("Result").TryGetProperty("AttemptContext", out _).ShouldBeFalse();
        await _tokenMeasurer.DidNotReceiveWithAnyArgs().MeasurePayloadAsync(default!, default!, default!, default, default);
        await _generationProvider.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidSnapshotFloorPreventsInvocation(int floor)
    {
        StubHappy();
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request() with { ProviderCapabilityVersion = floor }, CancellationToken.None);

        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ProviderUnavailable);
        DispatchedResult().ProviderCapabilityVersion.ShouldBe(0);
        await _generationProvider.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default);
    }

    [Fact]
    public async Task InvokedCapabilityVersionSurvivesProviderFailure()
    {
        StubHappy();
        StubEnabledCatalog(capabilityVersion: 4);
        _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentGenerationProviderResult(AgentGenerationOutcome.ProviderTimeout, null, 0, 0));
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.ProviderTimeout);
        DispatchedResult().ProviderCapabilityVersion.ShouldBe(4);
        await _generationProvider.Received(1).GenerateAsync(
            Arg.Is<AgentGenerationProviderRequest>(request => request.ProviderCapabilityVersion == 4), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("missing-read")]
    [InlineData("unauthorized")]
    [InlineData("stale")]
    [InlineData("missing-messages")]
    [InlineData("count-mismatch")]
    [InlineData("null-message")]
    [InlineData("blank-message")]
    [InlineData("invalid-timestamp")]
    public async Task InvalidFreshReadStopsBeforeCatalogMeasurementOrInvocation(string defect)
    {
        StubHappy();
        CaptureDispatch();
        ConversationContextMessage[] messages = [new(SourceConversationText, _sampleCreatedAt)];
        var valid = new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Loaded, messages, 1, true);
        ConversationContextReadResult invalid = defect switch
        {
            "missing-read" => null!,
            "unauthorized" => valid with { Outcome = AgentInteractionContextLoadOutcome.Unauthorized },
            "stale" => valid with { IsFresh = false },
            "missing-messages" => valid with { Messages = null },
            "count-mismatch" => valid with { MessageCount = 0 },
            "null-message" => valid with { Messages = [null!] },
            "blank-message" => valid with { Messages = [new("  ", _sampleCreatedAt)] },
            "invalid-timestamp" => valid with { Messages = [new(SourceConversationText, default)] },
            _ => throw new ArgumentOutOfRangeException(nameof(defect)),
        };
        _contextReader.ReadAsync(TenantId, SourceConversationId, string.Empty, ActorUserId, "corr-1", Arg.Any<CancellationToken>())
            .Returns(invalid);

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.InvalidContext);
        DispatchedResult().GeneratedContent.ShouldBeNull();
        AgentAttemptContextEvidence context = DispatchedResult().AttemptContext.ShouldNotBeNull();
        AgentInteractionContextLoadOutcome expectedOutcome = defect switch
        {
            "unauthorized" => AgentInteractionContextLoadOutcome.Unauthorized,
            "stale" => AgentInteractionContextLoadOutcome.Stale,
            _ => AgentInteractionContextLoadOutcome.Unavailable,
        };
        context.LoadOutcome.ShouldBe(expectedOutcome);
        context.Mode.ShouldBe(AgentInteractionContextMode.Blocked);
        context.BlockReason.ShouldBe(expectedOutcome switch
        {
            AgentInteractionContextLoadOutcome.Unauthorized => AgentInteractionContextBlockReason.ContextUnavailable,
            AgentInteractionContextLoadOutcome.Stale => AgentInteractionContextBlockReason.ContextNotFresh,
            _ => AgentInteractionContextBlockReason.ContextReadUnavailable,
        });
        context.Budget.ShouldBeNull();
        ReplayContextRefusal().ShouldBe(context);
        string dispatchedJson = Encoding.UTF8.GetString(LastDispatched()!.Payload);
        dispatchedJson.ShouldNotContain(SourceConversationText);
        using JsonDocument payload = JsonDocument.Parse(dispatchedJson);
        payload.RootElement.GetProperty("Result").GetProperty("AttemptContext").TryGetProperty("Budget", out _).ShouldBeFalse();
        await _providerCatalogReader.DidNotReceiveWithAnyArgs().GetEntryAsync(default!, default!, default!, default);
        await _tokenMeasurer.DidNotReceiveWithAnyArgs().MeasurePayloadAsync(default!, default!, default!, default, default);
        await _generationProvider.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, -1)]
    [InlineData(true, 0)]
    [InlineData(true, 99_201)]
    public async Task UnavailableInvalidOrOversizeMeasurementPreventsInvocation(bool isAvailable, int count)
    {
        StubHappy();
        StubEnabledCatalog(capabilityVersion: 4);
        CaptureDispatch();
        _tokenMeasurer.MeasurePayloadAsync(Arg.Any<string>(), ProviderId, ModelId, 4, Arg.Any<CancellationToken>())
            .Returns(new ConversationContextTokenMeasurement(isAvailable, count));

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.InvalidContext);
        DispatchedResult().GeneratedContent.ShouldBeNull();
        DispatchedResult().ProviderCapabilityVersion.ShouldBe(4);
        DispatchedResult().PromptTokenCount.ShouldBe(0);
        DispatchedResult().OutputTokenCount.ShouldBe(0);
        AgentAttemptContextEvidence context = DispatchedResult().AttemptContext.ShouldNotBeNull();
        context.LoadOutcome.ShouldBe(AgentInteractionContextLoadOutcome.Loaded);
        context.Mode.ShouldBe(AgentInteractionContextMode.Blocked);
        if (count == 99_201)
        {
            context.BlockReason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
            context.Budget.ShouldBe(new AgentAttemptContextBudgetEvidence(
                count, 4, ContextWindowTokenLimit, MaxOutputTokenLimit, 10, 12_800, 99_200));
        }
        else
        {
            context.BlockReason.ShouldBe(AgentInteractionContextBlockReason.ModelBudgetUnavailable);
            context.Budget.ShouldBeNull();
        }

        ReplayContextRefusal().ShouldBe(context);
        await _generationProvider.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default);
    }

    [Fact]
    public async Task TokenizerFaultRefusesWithoutLeakingDetailsOrCallingProvider()
    {
        StubHappy();
        CaptureDispatch();
        _tokenMeasurer.MeasurePayloadAsync(Arg.Any<string>(), ProviderId, ModelId, 1, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(SourceConversationText));

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.InvalidContext);
        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(SourceConversationText);
        await _generationProvider.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactCapacitySendsTheMeasuredPayloadForTheAdmittedVersion(bool emptyTimeline)
    {
        StubHappy();
        ProviderCatalogEntryView entry = Entry(ProviderModelStatus.Enabled, supportsTextGeneration: true) with
        {
            CapabilityVersion = 4,
            ContextWindowTokenLimit = 801,
            MaxOutputTokenLimit = 500,
        };
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, entry));
        CaptureDispatch();
        ConversationContextMessage[] messages = emptyTimeline ? [] :
        [
            new("later", _sampleCreatedAt.AddMinutes(1)),
            new(SourceConversationText, _sampleCreatedAt),
        ];
        _contextReader.ReadAsync(TenantId, SourceConversationId, string.Empty, ActorUserId, "corr-1", Arg.Any<CancellationToken>())
            .Returns(new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Loaded, messages, messages.Length, true));
        string payload = emptyTimeline ? string.Empty : $"{SourceConversationText}{Environment.NewLine}later{Environment.NewLine}";
        _tokenMeasurer.MeasurePayloadAsync(payload, ProviderId, ModelId, 4, Arg.Any<CancellationToken>())
            .Returns(new ConversationContextTokenMeasurement(true, emptyTimeline ? 0 : 220));

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.Succeeded);
        AgentAttemptContextEvidence context = DispatchedResult().AttemptContext.ShouldNotBeNull();
        context.LoadOutcome.ShouldBe(AgentInteractionContextLoadOutcome.Loaded);
        context.Mode.ShouldBe(AgentInteractionContextMode.Full);
        context.BlockReason.ShouldBeNull();
        context.Budget.ShouldBe(new AgentAttemptContextBudgetEvidence(
            emptyTimeline ? 0 : 220, 4, 801, 500, 10, 81, 220));
        await _tokenMeasurer.Received(1).MeasurePayloadAsync(payload, ProviderId, ModelId, 4, CancellationToken.None);
        await _generationProvider.Received(1).GenerateAsync(
            Arg.Is<AgentGenerationProviderRequest>(request => request.ContextPayload == payload && request.ProviderCapabilityVersion == 4 && request.MaxOutputTokenLimit == 500),
            CancellationToken.None);
    }

    [Fact]
    public async Task ATokenizerReturningAfterCancellationCannotStartAnInvocation()
    {
        StubHappy();
        using var cancellation = new CancellationTokenSource();
        var measurement = new TaskCompletionSource<ConversationContextTokenMeasurement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _tokenMeasurer.MeasurePayloadAsync(Arg.Any<string>(), ProviderId, ModelId, 1, cancellation.Token)
            .Returns(measurement.Task);

        Task request = Orchestrator.ExecuteAsync(Request(), cancellation.Token);
        request.IsCompleted.ShouldBeFalse();
        _ = _tokenMeasurer.Received(1).MeasurePayloadAsync(Arg.Any<string>(), ProviderId, ModelId, 1, cancellation.Token);
        await cancellation.CancelAsync();
        measurement.SetResult(new ConversationContextTokenMeasurement(true, 1));

        await Should.ThrowAsync<OperationCanceledException>(async () => await request);
        await _generationProvider.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default);
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task NonCallerConversationTimeoutRefusesWithoutMeasuringOrInvoking()
    {
        StubHappy();
        CaptureDispatch();
        _contextReader.ReadAsync(TenantId, SourceConversationId, string.Empty, ActorUserId, "corr-1", Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException(SourceConversationText));

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.InvalidContext);
        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(SourceConversationText);
        await _tokenMeasurer.DidNotReceiveWithAnyArgs().MeasurePayloadAsync(default!, default!, default!, default, default);
        await _generationProvider.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default);
    }

    [Fact]
    public async Task ARejectedCatalogReadReturningAfterCancellationCannotDispatch()
    {
        StubHappy();
        using var cancellation = new CancellationTokenSource();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, cancellation.Token)
            .Returns(_ =>
            {
                cancellation.Cancel();
                return new ProviderCatalogEntryReadResult(
                    ProviderCatalogInspectionStatus.Success, Entry(ProviderModelStatus.Disabled, supportsTextGeneration: true));
            });

        await Should.ThrowAsync<OperationCanceledException>(async () => await Orchestrator.ExecuteAsync(Request(), cancellation.Token));

        await _tokenMeasurer.DidNotReceiveWithAnyArgs().MeasurePayloadAsync(default!, default!, default!, default, default);
        await _generationProvider.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default);
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task ATokenizerOfferingOnlyTimelineCountsCannotAuthorizeInvocation()
    {
        StubHappy();
        CaptureDispatch();
        var orchestrator = new AgentInteractionGenerationOrchestrator(
            _contextReader,
            _providerCatalogReader,
            _generationProvider,
            _policyReader,
            _safetyEvaluator,
            _dispatcher,
            new TimelineOnlyConversationContextTokenMeasurer());

        await orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        DispatchedResult().Outcome.ShouldBe(AgentGenerationOutcome.InvalidContext);
        await _generationProvider.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default);
    }

    // ===== Helpers =====

    private static AgentInteractionState ContextReadyState()
    {
        var state = new AgentInteractionState();
        state.Apply(new InteractionRequested(
            AgentInteractionId, AgentId, "party-001", SourceConversationId, SampleSnapshot(), "prompt", "idem"));
        state.Apply(new AgentInteractionAuthorized(AgentInteractionId));
        state.Apply(new AgentInteractionContextReady(AgentInteractionId, SampleContextEvidence()));
        return state;
    }

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
        _contextReader.ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, "corr-1", Arg.Any<CancellationToken>())
            .Returns(new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Loaded, messages, messages.Count, true));
        _tokenMeasurer.MeasurePayloadAsync(Arg.Any<string>(), ProviderId, ModelId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new ConversationContextTokenMeasurement(true, 1));
    }

    private void StubEnabledCatalog(int capabilityVersion = 1)
        => _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success,
                Entry(ProviderModelStatus.Enabled, supportsTextGeneration: true) with { CapabilityVersion = capabilityVersion }));

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
            CapabilityVersion: 1,
            new ProviderModelPricing("USD", 0.002m, 0.008m, 1));

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
        => _dispatcher.DispatchAsync(Arg.Do<AgentCommandIntent>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private AgentAttemptContextEvidence? ReplayContextRefusal()
    {
        AgentCommandIntent dispatched = LastDispatched().ShouldNotBeNull();
        var command = JsonSerializer.Deserialize<GenerateAgentOutput>(dispatched.Payload)!;
        AgentInteractionState state = ContextReadyState();
        DomainResult decision = AgentInteractionAggregate.Handle(command, state, dispatched.ToFixtureEnvelope());
        var failure = decision.Events[0].ShouldBeOfType<AgentOutputGenerationFailed>();
        state.Apply(JsonSerializer.Deserialize<AgentOutputGenerationFailed>(JsonSerializer.Serialize(failure))!);
        state.Status.ShouldBe(AgentInteractionStatus.GenerationFailed);
        return AgentInteractionAuditInspection.GetContextEvidence(state, isAuthorized: true).Evidence.ShouldNotBeNull().GenerationAttemptContext;
    }

    private AgentCommandIntent? LastDispatched() => _lastDispatched;

    private AgentOutputGenerationResult DispatchedResult()
        => JsonSerializer.Deserialize<GenerateAgentOutput>(LastDispatched()!.Payload)!.Result;
}
