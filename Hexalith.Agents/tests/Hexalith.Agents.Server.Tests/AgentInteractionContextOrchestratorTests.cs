namespace Hexalith.Agents.Server.Tests;

using System;
using System.Collections.Generic;
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
/// Tests for <see cref="AgentInteractionContextOrchestrator"/> (Story 2.3; AC1–AC4; AD-3, AD-11, AD-12, AD-14). Verify
/// that a loaded+fits read assembles a <c>Loaded</c> measurement and a dispatched <see cref="BuildAgentInteractionContext"/>
/// returning <c>ContextReady</c>; that oversized/stale/unauthorized/unavailable reads and a missing/disabled/invalid
/// budget map to the correct <c>ContextBlocked</c> reason; that a reader/measurer/catalog that throws fails closed (no
/// unhandled exception, no raw error text); that client-supplied measurement is discarded and reserved extensions are
/// stripped; that NO provider adapter / Conversations post / proposal is on the path; that the dispatched command and
/// returned result carry NO raw message text (AD-14); and that all-deferred defaults fail closed to
/// <c>ContextBlocked(ContextUnavailable)</c>.
/// </summary>
public sealed class AgentInteractionContextOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentInteractionId = "interaction-001";
    private const string CallerPartyId = "party-001";
    private const string SourceConversationId = "conversation-001";
    private const string ActorUserId = "caller-user";
    private const string ProviderId = "openai";
    private const string ModelId = "gpt-4o";
    private const int ContextWindowTokenLimit = 128_000;
    private const int MaxOutputTokenLimit = 16_000;
    private const string SecretConversationText = "top-secret-conversation-body-do-not-leak";

    private static readonly DateTimeOffset _sampleCreatedAt = new(2026, 6, 24, 10, 0, 0, TimeSpan.Zero);

    private readonly IConversationContextReader _contextReader = Substitute.For<IConversationContextReader>();
    private readonly IConversationContextTokenMeasurer _tokenMeasurer = Substitute.For<IConversationContextTokenMeasurer>();
    private readonly IProviderCatalogReader _providerCatalogReader = Substitute.For<IProviderCatalogReader>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private CommandEnvelope? _lastDispatched;

    private AgentInteractionContextOrchestrator Orchestrator => new(
        _contextReader,
        _tokenMeasurer,
        _providerCatalogReader,
        _dispatcher);

    // ===== Loaded + fits → ContextReady (AC2) =====

    [Fact]
    public async Task Loaded_and_fits_dispatches_a_loaded_measurement_and_returns_context_ready()
    {
        StubLoaded(tokenCount: 1_000);
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.AgentInteractionId.ShouldBe(AgentInteractionId);
        outcome.Status.ShouldBe(AgentInteractionStatus.ContextReady);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(BuildAgentInteractionContext));
        dispatched.Domain.ShouldBe("agent-interaction");
        dispatched.AggregateId.ShouldBe(AgentInteractionId);

        AgentInteractionContextMeasurement measurement = DispatchedMeasurement();
        measurement.LoadOutcome.ShouldBe(AgentInteractionContextLoadOutcome.Loaded);
        measurement.FullContextTokenCount.ShouldBe(1_000);
        measurement.ContextWindowTokenLimit.ShouldBe(ContextWindowTokenLimit);
        measurement.ReservedOutputTokenCount.ShouldBe(MaxOutputTokenLimit);
        measurement.ApprovedBoundedBehavior.ShouldBeNull(); // V1 "full-conversation-v1" approves no bounded behavior
    }

    // ===== Oversized → ContextBlocked(ExceedsModelBudget) (AC3) =====

    [Fact]
    public async Task Oversized_full_context_returns_context_blocked()
    {
        StubLoaded(tokenCount: 500_000); // > 112000 available, no approved bounded behavior
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        DispatchedMeasurement().FullContextTokenCount.ShouldBe(500_000);
    }

    // ===== Stale / unauthorized / unavailable reads (AC1, AC3) =====

    [Fact]
    public async Task Stale_read_returns_context_blocked_context_not_fresh()
    {
        _contextReader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, ActorUserId, Arg.Any<CancellationToken>())
            .Returns(new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Stale, null, 0, false));
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        DispatchedMeasurement().LoadOutcome.ShouldBe(AgentInteractionContextLoadOutcome.Stale);
    }

    [Theory]
    [InlineData(AgentInteractionContextLoadOutcome.Unauthorized)]
    [InlineData(AgentInteractionContextLoadOutcome.Unavailable)]
    public async Task Hidden_or_unavailable_read_returns_context_blocked_context_unavailable(AgentInteractionContextLoadOutcome loadOutcome)
    {
        _contextReader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, ActorUserId, Arg.Any<CancellationToken>())
            .Returns(new ConversationContextReadResult(loadOutcome, null, 0, false));
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        DispatchedMeasurement().LoadOutcome.ShouldBe(loadOutcome);
    }

    // ===== Budget unobtainable / invalid → ContextBlocked(ModelBudgetUnavailable) (AC2, AC3) =====

    [Fact]
    public async Task Missing_catalog_entry_returns_context_blocked_with_a_zeroed_budget()
    {
        StubLoaded(tokenCount: 1_000);
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null));
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        DispatchedMeasurement().ContextWindowTokenLimit.ShouldBe(0); // zeroed budget → ModelBudgetUnavailable
    }

    [Fact]
    public async Task Disabled_or_non_text_capable_entry_returns_context_blocked()
    {
        StubLoaded(tokenCount: 1_000);
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, Entry(supportsTextGeneration: false)));
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        DispatchedMeasurement().ContextWindowTokenLimit.ShouldBe(0);
    }

    [Fact]
    public async Task Token_measurement_unavailable_returns_context_blocked_model_budget_unavailable()
    {
        StubLoaded(tokenCount: 1_000);
        _tokenMeasurer.MeasureAsync(Arg.Any<IReadOnlyList<ConversationContextMessage>>(), ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(ConversationContextTokenMeasurement.NotAvailable);
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        DispatchedMeasurement().ContextWindowTokenLimit.ShouldBe(0);
    }

    [Fact]
    public async Task Empty_provider_or_model_id_short_circuits_to_context_blocked_without_reading_the_catalog()
    {
        // A snapshot that recorded no provider/model id cannot have a budget — the orchestrator must fail closed to a
        // zeroed budget WITHOUT calling the catalog (no needless cross-tenant read; AC2/AD-9).
        IReadOnlyList<ConversationContextMessage> messages = [new ConversationContextMessage(SecretConversationText, _sampleCreatedAt)];
        _contextReader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, ActorUserId, Arg.Any<CancellationToken>())
            .Returns(new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Loaded, messages, messages.Count, true));
        _tokenMeasurer.MeasureAsync(Arg.Any<IReadOnlyList<ConversationContextMessage>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ConversationContextTokenMeasurement(true, 1_000));
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(providerId: ""), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        DispatchedMeasurement().ContextWindowTokenLimit.ShouldBe(0); // zeroed budget → ModelBudgetUnavailable
        await _providerCatalogReader.DidNotReceive().GetEntryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_loaded_read_with_a_null_messages_payload_is_a_degraded_read_failing_closed_to_unavailable()
    {
        // A reader that reports Loaded but hands back no messages is a degraded/contract-violating read — the orchestrator
        // must NOT proceed to measure a null payload; it fails closed to Unavailable (FR-21; AD-12).
        _contextReader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, ActorUserId, Arg.Any<CancellationToken>())
            .Returns(new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Loaded, null, 0, true));
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        DispatchedMeasurement().LoadOutcome.ShouldBe(AgentInteractionContextLoadOutcome.Unavailable);
        await _tokenMeasurer.DidNotReceive().MeasureAsync(
            Arg.Any<IReadOnlyList<ConversationContextMessage>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ===== Fail closed on throw (AC1, AC3; AD-14) =====

    [Fact]
    public async Task A_context_reader_that_throws_fails_closed_without_an_unhandled_exception_or_raw_error()
    {
        _contextReader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, ActorUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("conversations transport blew up: secret-bearing detail"));
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        DispatchedMeasurement().LoadOutcome.ShouldBe(AgentInteractionContextLoadOutcome.Unavailable);

        // AD-14: no raw error text crosses the boundary.
        JsonSerializer.Serialize(outcome).ShouldNotContain("secret-bearing detail");
        JsonSerializer.Serialize(DispatchedMeasurement()).ShouldNotContain("secret-bearing detail");
    }

    [Fact]
    public async Task A_token_measurer_that_throws_fails_closed_to_context_blocked()
    {
        StubLoaded(tokenCount: 1_000);
        _tokenMeasurer.MeasureAsync(Arg.Any<IReadOnlyList<ConversationContextMessage>>(), ProviderId, ModelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("tokenizer blew up"));
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
    }

    [Fact]
    public async Task A_provider_catalog_reader_that_throws_fails_closed_to_context_blocked()
    {
        StubLoaded(tokenCount: 1_000);
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("catalog projection blew up"));
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        DispatchedMeasurement().ContextWindowTokenLimit.ShouldBe(0);
    }

    [Fact]
    public async Task A_genuine_cancellation_propagates_and_no_command_is_dispatched()
    {
        _contextReader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, ActorUserId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Orchestrator.ExecuteAsync(Request(), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Trust model: client measurement discarded, reserved extensions stripped (AC3) =====

    [Fact]
    public async Task Strips_client_forged_reserved_extensions_and_preserves_benign_ones()
    {
        StubLoaded(tokenCount: 1_000);
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
        StubLoaded(tokenCount: 1_000);
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.TenantId.ShouldBe(TenantId);
        dispatched.MessageId.ShouldBe("msg-1");
        dispatched.CorrelationId.ShouldBe("corr-1");
        dispatched.UserId.ShouldBe(ActorUserId);
        dispatched.CausationId.ShouldBeNull();
    }

    // ===== No side effects (AC3) =====

    [Fact]
    public async Task Only_reads_and_one_context_dispatch_no_provider_invocation_or_conversation_post()
    {
        StubLoaded(tokenCount: 1_000);
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        // The orchestrator's only collaborators are read ports + the command dispatcher — there is structurally no
        // provider adapter, no IConversationClient.AppendMessageAsync, and no proposal creation on this path (AC3). The
        // conversation seam is a READ (IConversationContextReader.ReadAsync), and exactly one context command is dispatched.
        await _contextReader.Received(1).ReadAsync(TenantId, SourceConversationId, CallerPartyId, ActorUserId, Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
        LastDispatched().ShouldNotBeNull().CommandType.ShouldBe(nameof(BuildAgentInteractionContext));
    }

    // ===== Raw content never crosses the boundary (AD-14) =====

    [Fact]
    public async Task The_dispatched_command_and_returned_result_carry_no_raw_message_text()
    {
        StubLoaded(tokenCount: 1_000); // the loaded messages carry SecretConversationText
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        // The raw conversation text is used only transiently for measurement and then discarded — it must appear nowhere
        // on the dispatched command/envelope or the returned outcome.
        JsonSerializer.Serialize(outcome).ShouldNotContain(SecretConversationText);
        System.Text.Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(SecretConversationText);
        JsonSerializer.Serialize(DispatchedMeasurement()).ShouldNotContain(SecretConversationText);
    }

    // ===== Cross-seam: the dispatched context command drives the real pure aggregate to the same decision =====

    [Fact]
    public async Task End_to_end_the_dispatched_context_command_drives_the_aggregate_to_the_same_decision()
    {
        StubLoaded(tokenCount: 500_000); // oversized → ContextBlocked
        CaptureDispatch();

        AgentInteractionContextOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        var command = JsonSerializer.Deserialize<BuildAgentInteractionContext>(dispatched.Payload)!;

        var state = new AgentInteractionState();
        state.Apply(new InteractionRequested(
            AgentInteractionId, "hexa", CallerPartyId, SourceConversationId, SampleSnapshot(), "prompt", "idem"));
        state.Apply(new AgentInteractionAuthorized(AgentInteractionId));
        DomainResult result = AgentInteractionAggregate.Handle(command, state, dispatched);

        AgentInteractionContextBlocked blocked = result.Events[0].ShouldBeOfType<AgentInteractionContextBlocked>();
        blocked.Reason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
        // The orchestrator's returned status and the aggregate's recorded decision agree (shared policy — no drift).
        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
    }

    // ===== All-deferred defaults fail closed to ContextBlocked(ContextUnavailable) (AC1; FR-21) =====

    [Fact]
    public async Task All_deferred_defaults_fail_closed_to_context_blocked()
    {
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        CommandEnvelope? captured = null;
        _ = dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => captured = e), Arg.Any<CancellationToken>());

        var orchestrator = new AgentInteractionContextOrchestrator(
            new DeferredConversationContextReader(),
            new DeferredConversationContextTokenMeasurer(),
            new DeferredProviderCatalogReader(),
            dispatcher);

        AgentInteractionContextOutcomeResult outcome = await orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        await dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
        var command = JsonSerializer.Deserialize<BuildAgentInteractionContext>(captured!.Payload)!;
        command.Measurement.LoadOutcome.ShouldBe(AgentInteractionContextLoadOutcome.Unavailable);
    }

    // ===== Helpers =====

    private void StubLoaded(int tokenCount)
    {
        IReadOnlyList<ConversationContextMessage> messages = [new ConversationContextMessage(SecretConversationText, _sampleCreatedAt)];
        _contextReader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, ActorUserId, Arg.Any<CancellationToken>())
            .Returns(new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Loaded, messages, messages.Count, true));
        _tokenMeasurer.MeasureAsync(Arg.Any<IReadOnlyList<ConversationContextMessage>>(), ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ConversationContextTokenMeasurement(true, tokenCount));
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, Entry(supportsTextGeneration: true)));
    }

    private static ProviderCatalogEntryView Entry(bool supportsTextGeneration)
        => new(
            ProviderId,
            ModelId,
            "OpenAI GPT-4o",
            ProviderModelStatus.Enabled,
            supportsTextGeneration,
            ContextWindowTokenLimit,
            MaxOutputTokenLimit,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o",
            IsSelectableForNewActiveUse: true,
            CapabilityVersion: 1);

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

    private static AgentInteractionContextRequest Request(
        IReadOnlyDictionary<string, string>? clientExtensions = null,
        string providerId = ProviderId,
        string modelId = ModelId)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentInteractionId,
            SourceConversationId,
            CallerPartyId,
            providerId,
            modelId,
            ProviderCapabilityVersion: 1,
            AgentInteractionSnapshot.DefaultContextPolicyReference,
            ActorUserId,
            ClientCorrelationId: "client-corr-1",
            clientExtensions);

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;

    private AgentInteractionContextMeasurement DispatchedMeasurement()
        => JsonSerializer.Deserialize<BuildAgentInteractionContext>(LastDispatched()!.Payload)!.Measurement;
}
