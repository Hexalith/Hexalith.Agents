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
/// Tests for <see cref="AgentInteractionProposalRegenerationOrchestrator"/> (Story 3.4; AC1–AC4; AD-3, AD-5, AD-9, AD-12,
/// AD-13, AD-14). Verify AC4 (a terminal proposal never invokes the provider or dispatches), the fail-closed approver
/// authorization (no provider call, no dispatch), the Story 2.4 provider + content-safety mapping (timeout/disabled/
/// not-found/re-read failure/safety-block/policy-not-available), the happy path dispatching
/// <see cref="RegenerateProposedAgentReply"/> with the deterministic version id and content on the COMMAND but NONE on the
/// returned outcome (AD-14), deterministic id reuse on retry, reserved-extension stripping, cancellation propagation, and
/// that the dispatched command drives the real aggregate to the same decision. The shared regeneration policy is not visible
/// to this assembly, so decisions are asserted via the returned status.
/// </summary>
public sealed class AgentInteractionProposalRegenerationOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentInteractionId = "interaction-001";
    private const string ProposalId = "proposal-001";
    private const string AgentId = "hexa";
    private const string SourceConversationId = "conversation-001";
    private const string ProviderId = "openai";
    private const string ModelId = "gpt-4o";
    private const string RequesterPartyId = "requester-party-1";
    private const string RegenerationAttemptId = "regeneration-attempt-1";
    private const string ActorUserId = "approver-user";
    private const int ApproverPolicyVersion = 1;
    private const int ContentSafetyPolicyVersion = 1;
    private const int ContextWindowTokenLimit = 128_000;
    private const int MaxOutputTokenLimit = 16_000;
    private const string RegeneratedContentText = "the-regenerated-answer-do-not-leak";
    private const string SourceConversationText = "source-conversation-secret-body-do-not-leak";

    private static readonly DateTimeOffset _sampleCreatedAt = new(2026, 6, 24, 10, 0, 0, TimeSpan.Zero);

    private readonly IConversationContextReader _contextReader = Substitute.For<IConversationContextReader>();
    private readonly IProviderCatalogReader _providerCatalogReader = Substitute.For<IProviderCatalogReader>();
    private readonly IAgentGenerationProvider _generationProvider = Substitute.For<IAgentGenerationProvider>();
    private readonly IAgentContentSafetyPolicyReader _policyReader = Substitute.For<IAgentContentSafetyPolicyReader>();
    private readonly IContentSafetyEvaluator _safetyEvaluator = Substitute.For<IContentSafetyEvaluator>();
    private readonly IApproverPolicyResolver _resolver = Substitute.For<IApproverPolicyResolver>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private CommandEnvelope? _lastDispatched;

    private AgentInteractionProposalRegenerationOrchestrator Orchestrator => new(
        _contextReader,
        _providerCatalogReader,
        _generationProvider,
        _policyReader,
        _safetyEvaluator,
        _resolver,
        _dispatcher);

    // ===== Happy path → ProposalRegenerated (AC1, AC2) =====

    [Fact]
    public async Task Authorized_regeneration_dispatches_with_the_deterministic_version_id_and_returns_proposal_regenerated()
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerated);
        outcome.NotRegeneratableReason.ShouldBe(AgentProposedReplyNotRegeneratableReason.Unknown);
        outcome.RegeneratedVersionId.ShouldBe(AgentProposalRegenerationIdentity.DeriveVersionId(AgentInteractionId, SourceConversationId, RegenerationAttemptId));

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(RegenerateProposedAgentReply));
        dispatched.Domain.ShouldBe("agent-interaction");
        dispatched.AggregateId.ShouldBe(AgentInteractionId);

        AgentProposalRegenerationResult result = DispatchedResult();
        result.Outcome.ShouldBe(AgentProposalRegenerationOutcome.Regenerated);
        result.AuthorizationVerdict.ShouldBe(ApproverPolicyValidationStatus.Valid);
        result.ProposalId.ShouldBe(ProposalId);
        result.RequesterPartyId.ShouldBe(RequesterPartyId);
        result.RegeneratedVersion.ShouldNotBeNull().Kind.ShouldBe(AgentGenerationKind.Regenerated);
        result.RegeneratedVersion.GeneratedContent.ShouldBe(RegeneratedContentText);
    }

    [Fact]
    public async Task The_command_carries_the_regenerated_content_but_the_returned_outcome_carries_none()
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldContain(RegeneratedContentText); // the write-path command home
        JsonSerializer.Serialize(outcome).ShouldNotContain(RegeneratedContentText); // the safe outcome is content-free
    }

    [Fact]
    public async Task The_dispatched_command_carries_no_raw_source_conversation_text()
    {
        StubHappy();
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(SourceConversationText);
    }

    // ===== AC4 — a terminal proposal never invokes the provider or dispatches =====

    [Theory]
    [InlineData(ProposedAgentReplyState.Unknown)]
    public async Task A_terminal_or_missing_proposal_denies_without_invoking_the_provider_or_dispatching(ProposedAgentReplyState state)
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(proposalState: state), CancellationToken.None);

        outcome.NotRegeneratableReason.ShouldBe(AgentProposedReplyNotRegeneratableReason.ProposalNotPending);
        outcome.Status.ShouldBe(AgentInteractionStatus.Unknown);
        outcome.RegeneratedVersionId.ShouldBeEmpty();
        await _generationProvider.DidNotReceive().GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>());
        await _contextReader.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ProposedAgentReplyState.Pending)]
    [InlineData(ProposedAgentReplyState.Edited)]
    [InlineData(ProposedAgentReplyState.Regenerated)]
    public async Task A_retryable_proposal_state_proceeds_to_regeneration(ProposedAgentReplyState state)
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(proposalState: state), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerated);
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Fail-closed authorization → no-dispatch, NO provider call (FR-16; AD-12) =====

    [Theory]
    [InlineData(ApproverSourceOutcome.Missing)]
    [InlineData(ApproverSourceOutcome.Disabled)]
    [InlineData(ApproverSourceOutcome.Ambiguous)]
    [InlineData(ApproverSourceOutcome.Unavailable)]
    [InlineData(ApproverSourceOutcome.Unauthorized)]
    [InlineData(ApproverSourceOutcome.Unknown)]
    public async Task A_non_resolved_approver_source_denies_without_dispatching_or_invoking_the_provider(ApproverSourceOutcome sourceOutcome)
    {
        StubLoadedConversation();
        StubEnabledCatalog();
        StubProviderSucceeded();
        StubResolver(sourceOutcome);
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.NotRegeneratableReason.ShouldBe(AgentProposedReplyNotRegeneratableReason.NotAuthorized);
        outcome.Status.ShouldBe(AgentInteractionStatus.Unknown);
        outcome.RegeneratedVersionId.ShouldBeEmpty();
        await _generationProvider.DidNotReceive().GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>());
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_null_or_empty_approver_policy_denies_without_dispatching()
    {
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(
            Request(policy: new AgentApproverPolicy([], ApproverPolicyBasisDisclosure.OperatorOnly)), CancellationToken.None);

        outcome.NotRegeneratableReason.ShouldBe(AgentProposedReplyNotRegeneratableReason.NotAuthorized);
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_resolver_that_throws_fails_closed_to_a_no_dispatch_denial_without_leaking()
    {
        _resolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("resolve blew up: secret-bearing detail"));
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.NotRegeneratableReason.ShouldBe(AgentProposedReplyNotRegeneratableReason.NotAuthorized);
        JsonSerializer.Serialize(outcome).ShouldNotContain("secret-bearing detail");
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task All_deferred_defaults_deny_the_regeneration_without_dispatching()
    {
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        var orchestrator = new AgentInteractionProposalRegenerationOrchestrator(
            new DeferredConversationContextReader(),
            new DeferredProviderCatalogReader(),
            new DeferredAgentGenerationProvider(),
            new DeferredAgentContentSafetyPolicyReader(),
            new DeferredContentSafetyEvaluator(),
            new DeferredApproverPolicyResolver(),
            dispatcher);

        AgentInteractionProposalRegenerationOutcomeResult outcome = await orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        // The first fail-closed seam on the path is the approver resolver → no-dispatch NotAuthorized denial (no provider call).
        outcome.NotRegeneratableReason.ShouldBe(AgentProposedReplyNotRegeneratableReason.NotAuthorized);
        await dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Provider + content-safety failure mapping (AC3) =====

    [Fact]
    public async Task Content_safety_blocked_returns_regeneration_failed_with_no_content_in_the_envelope_or_result()
    {
        StubHappy();
        _safetyEvaluator.EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ContentSafetyEvaluationResult(ContentSafetyVerdict.Blocked, "violence"));
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.ContentSafetyBlocked);
        DispatchedResult().RegeneratedVersion.ShouldBeNull();
        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(RegeneratedContentText);
        JsonSerializer.Serialize(outcome).ShouldNotContain(RegeneratedContentText);
    }

    [Fact]
    public async Task An_unrecognized_content_safety_verdict_fails_closed_to_regeneration_failed_content_safety_blocked()
    {
        // ContentSafetyVerdict.Unknown is the absent/unrecognized sentinel — it is NOT Passed, so the gate must block it (the
        // safety check is `!= Passed`, never `== Blocked`), fail closed, and carry no version (AD-5, AD-14).
        StubHappy();
        _safetyEvaluator.EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ContentSafetyEvaluationResult(ContentSafetyVerdict.Unknown, null));
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.ContentSafetyBlocked);
        DispatchedResult().RegeneratedVersion.ShouldBeNull();
        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(RegeneratedContentText);
    }

    [Fact]
    public async Task A_content_safety_evaluator_that_throws_fails_closed_to_content_safety_blocked_without_leaking()
    {
        // An evaluator that faults must NOT skip the gate: it fails closed to a blocked verdict → ContentSafetyBlocked, with no
        // version and no raw error text crossing the boundary (AD-9, AD-14).
        StubHappy();
        _safetyEvaluator.EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("safety evaluator blew up: secret-bearing detail"));
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.ContentSafetyBlocked);
        DispatchedResult().RegeneratedVersion.ShouldBeNull();
        string payload = Encoding.UTF8.GetString(LastDispatched()!.Payload);
        payload.ShouldNotContain(RegeneratedContentText);
        payload.ShouldNotContain("secret-bearing detail");
    }

    [Fact]
    public async Task A_provider_entry_without_text_generation_returns_regeneration_failed_provider_disabled_without_invoking_the_provider()
    {
        // An Enabled entry that does not support text generation is unusable for regeneration → ProviderDisabled before any
        // provider call (a distinct branch from a Disabled status).
        StubResolved();
        StubLoadedConversation();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, Entry(ProviderModelStatus.Enabled, supportsTextGeneration: false)));
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.ProviderDisabled);
        await _generationProvider.DidNotReceive().GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_blank_provider_id_returns_regeneration_failed_provider_unavailable_without_reading_the_catalog_or_invoking_the_provider()
    {
        // A snapshot missing the provider/model id cannot be regenerated → ProviderUnavailable, short-circuiting before the
        // catalog read and the provider call.
        StubResolved();
        StubLoadedConversation();
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(providerId: " "), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.ProviderUnavailable);
        await _providerCatalogReader.DidNotReceive().GetEntryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _generationProvider.DidNotReceive().GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_provider_that_throws_fails_closed_to_regeneration_failed_adapter_without_leaking()
    {
        StubResolved();
        StubLoadedConversation();
        StubEnabledCatalog();
        _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("provider sdk blew up: secret-bearing detail"));
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.AdapterFailure);
        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain("secret-bearing detail");
    }

    [Fact]
    public async Task A_disabled_provider_entry_returns_regeneration_failed_provider_disabled_without_invoking_the_provider()
    {
        StubResolved();
        StubLoadedConversation();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, Entry(ProviderModelStatus.Disabled, supportsTextGeneration: true)));
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.ProviderDisabled);
        await _generationProvider.DidNotReceive().GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_not_found_catalog_entry_returns_regeneration_failed_provider_unavailable()
    {
        StubResolved();
        StubLoadedConversation();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null));
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.ProviderUnavailable);
    }

    [Fact]
    public async Task A_provider_timeout_outcome_returns_regeneration_failed_provider_timeout()
    {
        StubResolved();
        StubLoadedConversation();
        StubEnabledCatalog();
        _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentGenerationProviderResult(AgentGenerationOutcome.ProviderTimeout, null, 0, 0));
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.ProviderTimeout);
    }

    [Fact]
    public async Task A_provider_success_with_no_content_fails_closed_to_adapter_failure_and_skips_safety()
    {
        StubResolved();
        StubLoadedConversation();
        StubEnabledCatalog();
        _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentGenerationProviderResult(AgentGenerationOutcome.Succeeded, null, 1_200, 0));
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.AdapterFailure);
        DispatchedResult().RegeneratedVersion.ShouldBeNull();
        await _safetyEvaluator.DidNotReceive().EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_conversation_re_read_failure_returns_regeneration_failed_invalid_context_without_invoking_the_provider()
    {
        StubResolved();
        _contextReader.ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, Arg.Any<CancellationToken>())
            .Returns(ConversationContextReadResult.Unavailable);
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.InvalidContext);
        await _generationProvider.DidNotReceive().GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_not_available_safety_policy_returns_regeneration_failed_policy_failure_and_dispatches_no_content()
    {
        StubResolved();
        StubLoadedConversation();
        StubEnabledCatalog();
        StubProviderSucceeded();
        _policyReader.ReadAsync(TenantId, AgentId, Arg.Any<AgentResponseMode>(), Arg.Any<CancellationToken>())
            .Returns(AgentContentSafetyPolicyReadResult.NotAvailable);
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalRegenerationOutcome.PolicyFailure);
        DispatchedResult().RegeneratedVersion.ShouldBeNull();
        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(RegeneratedContentText);
        await _safetyEvaluator.DidNotReceive().EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>());
    }

    // ===== Deterministic id reuse on retry (AC2; AD-13) =====

    [Fact]
    public async Task A_retried_regeneration_reuses_the_same_deterministic_version_id()
    {
        StubHappy();
        var dispatched = new List<CommandEnvelope>();
        _ = _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(dispatched.Add), Arg.Any<CancellationToken>());

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);
        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        dispatched.Count.ShouldBe(2);
        string first = JsonSerializer.Deserialize<RegenerateProposedAgentReply>(dispatched[0].Payload)!.Result.RegeneratedVersionId;
        string second = JsonSerializer.Deserialize<RegenerateProposedAgentReply>(dispatched[1].Payload)!.Result.RegeneratedVersionId;
        second.ShouldBe(first);
    }

    // ===== Trust model + envelope scope (AC4) =====

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

    // ===== Cancellation propagates (AC3) =====

    [Fact]
    public async Task A_genuine_cancellation_propagates_and_no_command_is_dispatched()
    {
        _resolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Orchestrator.ExecuteAsync(Request(), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Cross-seam: the dispatched command drives the real aggregate to the same decision =====

    [Fact]
    public async Task End_to_end_the_dispatched_regenerate_command_drives_the_aggregate_to_proposal_regenerated()
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionProposalRegenerationOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        var command = JsonSerializer.Deserialize<RegenerateProposedAgentReply>(dispatched.Payload)!;

        AgentInteractionState state = ProposalCreatedState();
        DomainResult result = AgentInteractionAggregate.Handle(command, state, dispatched);

        ProposedAgentReplyRegenerated regenerated = result.Events[0].ShouldBeOfType<ProposedAgentReplyRegenerated>();
        regenerated.RegeneratedVersion.GeneratedContent.ShouldBe(RegeneratedContentText);
        // The orchestrator's returned status and the aggregate's recorded decision agree (shared policy — no drift).
        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerated);
    }

    // ===== Helpers =====

    private void StubHappy()
    {
        StubResolved();
        StubLoadedConversation();
        StubEnabledCatalog();
        StubProviderSucceeded();
        _policyReader.ReadAsync(TenantId, AgentId, Arg.Any<AgentResponseMode>(), Arg.Any<CancellationToken>())
            .Returns(new AgentContentSafetyPolicyReadResult(true, SamplePolicy(), 2));
        _safetyEvaluator.EvaluateAsync(Arg.Any<ContentSafetyEvaluationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ContentSafetyEvaluationResult(ContentSafetyVerdict.Passed, null));
    }

    private void StubResolved() => StubResolver(ApproverSourceOutcome.Resolved);

    private void StubResolver(ApproverSourceOutcome sourceOutcome)
        => _resolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .Returns(new ApproverPolicyResolutionResult([new ApproverSourceResolution(ApproverPolicySourceKind.Caller, sourceOutcome)]));

    private void StubLoadedConversation()
    {
        IReadOnlyList<ConversationContextMessage> messages = [new ConversationContextMessage(SourceConversationText, _sampleCreatedAt)];
        _contextReader.ReadAsync(TenantId, SourceConversationId, Arg.Any<string>(), ActorUserId, Arg.Any<CancellationToken>())
            .Returns(new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Loaded, messages, messages.Count, true));
    }

    private void StubEnabledCatalog()
        => _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, Entry(ProviderModelStatus.Enabled, supportsTextGeneration: true)));

    private void StubProviderSucceeded()
        => _generationProvider.GenerateAsync(Arg.Any<AgentGenerationProviderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentGenerationProviderResult(AgentGenerationOutcome.Succeeded, RegeneratedContentText, 1_200, 350));

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

    private static AgentApproverPolicy SingleSourcePolicy()
        => new([new ApproverPolicySource(ApproverPolicySourceKind.Caller, null, null)], ApproverPolicyBasisDisclosure.OperatorOnly);

    private static AgentInteractionProposalRegenerationRequest Request(
        ProposedAgentReplyState proposalState = ProposedAgentReplyState.Pending,
        AgentApproverPolicy? policy = null,
        IReadOnlyDictionary<string, string>? clientExtensions = null,
        string providerId = ProviderId,
        string modelId = ModelId)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentInteractionId,
            ProposalId,
            proposalState,
            AgentId,
            SourceConversationId,
            providerId,
            modelId,
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion,
            AgentResponseMode.Confirmation,
            RequesterPartyId,
            RegenerationAttemptId,
            policy ?? SingleSourcePolicy(),
            ApproverPolicyVersion,
            ActorUserId,
            ClientCorrelationId: "client-corr-1",
            clientExtensions);

    private void CaptureDispatch()
        => _ = _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;

    private AgentProposalRegenerationResult DispatchedResult()
        => JsonSerializer.Deserialize<RegenerateProposedAgentReply>(LastDispatched()!.Payload)!.Result;

    private static AgentInteractionState ProposalCreatedState()
    {
        var state = new AgentInteractionState();
        state.Apply(new InteractionRequested(
            AgentInteractionId, AgentId, "party-001", SourceConversationId, ConfirmationSnapshot(), "prompt", "idem"));
        state.Apply(new AgentInteractionAuthorized(AgentInteractionId));
        state.Apply(new AgentInteractionContextReady(AgentInteractionId, SampleContextEvidence()));
        state.Apply(new AgentOutputGenerated(AgentInteractionId, SampleGeneratedVersion()));
        state.Apply(new ProposedAgentReplyCreated(
            AgentInteractionId,
            new AgentProposedReplyEvidence(ProposalId, SourceConversationId, "version-attempt-1", ApproverPolicyVersion, ContentSafetyPolicyVersion, ExpiresAt: null)));
        return state;
    }

    private static AgentInteractionSnapshot ConfirmationSnapshot()
        => new(
            ConfigurationVersion: 3,
            InstructionsVersion: 2,
            AgentResponseMode.Confirmation,
            ApproverPolicyVersion,
            ProviderId,
            ModelId,
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion,
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

    private static AgentGeneratedVersion SampleGeneratedVersion()
        => new(
            "version-attempt-1",
            AttemptId: "attempt-1",
            AgentGenerationKind.Generated,
            "the-original-generated-answer",
            ProviderId,
            ModelId,
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion,
            PromptTokenCount: 1_200,
            OutputTokenCount: 350);
}
