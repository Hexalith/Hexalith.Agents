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
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>
/// Tests for <see cref="AgentInteractionProposalOrchestrator"/> (Story 3.1; AC1–AC4; AD-3, AD-5, AD-6, AD-13, AD-14).
/// Verify the happy path dispatches <see cref="CreateProposedAgentReply"/> with the right domain/aggregate id and a
/// deterministic <c>ProposalId</c> derived from interaction+version, returning <c>ProposalCreated</c> with NO content in the
/// envelope or result (the version reader's content is deliberately ignored — AD-14); that version-not-available /
/// reader-throws fail closed to <c>ProposalCreationFailed</c> without leaking; that a genuine cancellation propagates; that
/// the all-deferred default graph fails closed; that a retry reuses the deterministic id; that reserved trust keys are
/// stripped; that confirmation-vs-automatic short-circuits correctly; and that a configured expiry flows onto the result.
/// The shared proposal policy is not visible to this assembly, so decisions are asserted via the returned status +
/// dispatched outcome.
/// </summary>
public sealed class AgentInteractionProposalOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentInteractionId = "interaction-001";
    private const string AgentId = "hexa";
    private const string SourceConversationId = "conversation-001";
    private const string ActorUserId = "caller-user";
    private const int ApproverPolicyVersion = 1;
    private const int ContentSafetyPolicyVersion = 1;
    private const string VersionId = "version-attempt-1";
    private const string GeneratedContentText = "the-generated-answer-do-not-leak";
    private const string ExpiresAt = "2026-12-31T23:59:59Z";

    private readonly IAgentGeneratedVersionReader _versionReader = Substitute.For<IAgentGeneratedVersionReader>();
    private readonly IProposalExpiryPolicyReader _expiryReader = Substitute.For<IProposalExpiryPolicyReader>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private CommandEnvelope? _lastDispatched;

    private AgentInteractionProposalOrchestrator Orchestrator => new(
        _versionReader,
        _expiryReader,
        _dispatcher);

    // ===== Happy path → ProposalCreated (AC1, AC2) =====

    [Fact]
    public async Task Created_dispatches_create_with_the_deterministic_proposal_id_and_returns_proposal_created()
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionProposalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.AgentInteractionId.ShouldBe(AgentInteractionId);
        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(CreateProposedAgentReply));
        dispatched.Domain.ShouldBe("agent-interaction");
        dispatched.AggregateId.ShouldBe(AgentInteractionId);

        AgentProposalCreationResult result = DispatchedResult();
        result.Outcome.ShouldBe(AgentProposalCreationOutcome.Created);
        result.ProposalId.ShouldBe(AgentProposalIdentity.DeriveProposalId(AgentInteractionId, VersionId)); // deterministic (AD-13)
        result.SourceConversationId.ShouldBe(SourceConversationId);
        result.ProposedVersionId.ShouldBe(VersionId);
        result.ApproverPolicyVersion.ShouldBe(ApproverPolicyVersion);
        result.ContentSafetyPolicyVersion.ShouldBe(ContentSafetyPolicyVersion);
    }

    [Fact]
    public async Task The_dispatched_command_and_returned_outcome_carry_no_generated_content()
    {
        // The version reader returns content, but the orchestrator deliberately ignores it: it rides into the aggregate
        // command/result NOWHERE and never returns to the caller (AD-14).
        StubHappy();
        CaptureDispatch();

        AgentInteractionProposalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(GeneratedContentText);
        JsonSerializer.Serialize(outcome).ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public async Task A_configured_expiry_flows_onto_the_result()
    {
        StubVersionAvailable();
        _expiryReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(new ProposalExpiryPolicyResult(ExpiresAt));
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        DispatchedResult().ExpiresAt.ShouldBe(ExpiresAt);
    }

    [Fact]
    public async Task No_configured_expiry_records_a_null_expiry()
    {
        StubHappy(); // expiry reader returns None
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        DispatchedResult().ExpiresAt.ShouldBeNull();
    }

    // ===== Version not available → ProposalCreationFailed, no content (AC3; AD-12) =====

    [Fact]
    public async Task Version_not_available_returns_proposal_creation_failed_without_content()
    {
        _versionReader.ReadSelectedVersionAsync(TenantId, AgentInteractionId, Arg.Any<CancellationToken>())
            .Returns(AgentGeneratedVersionReadResult.NotAvailable);
        StubExpiryNone();
        CaptureDispatch();

        AgentInteractionProposalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalCreationFailed);
        AgentProposalCreationResult result = DispatchedResult();
        result.Outcome.ShouldBe(AgentProposalCreationOutcome.GeneratedVersionUnavailable);
        result.ProposedVersionId.ShouldBeEmpty(); // no version id known on a pre-version failure
        result.ProposalId.ShouldBeEmpty();
        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public async Task A_version_read_that_throws_fails_closed_to_proposal_creation_failed_without_leaking()
    {
        _versionReader.ReadSelectedVersionAsync(TenantId, AgentInteractionId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("read blew up: secret-bearing detail"));
        StubExpiryNone();
        CaptureDispatch();

        AgentInteractionProposalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalCreationFailed);
        DispatchedResult().Outcome.ShouldBe(AgentProposalCreationOutcome.GeneratedVersionUnavailable);
        string payload = Encoding.UTF8.GetString(LastDispatched()!.Payload);
        payload.ShouldNotContain("secret-bearing detail");
        payload.ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public async Task An_expiry_read_that_throws_still_creates_the_proposal_with_no_expiry()
    {
        // The expiry read is best-effort: a throw fails closed to no expiry rather than blocking an otherwise-valid proposal.
        StubVersionAvailable();
        _expiryReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("expiry read blew up"));
        CaptureDispatch();

        AgentInteractionProposalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);
        DispatchedResult().ExpiresAt.ShouldBeNull();
    }

    // ===== All-deferred default graph fails closed (AC1; FR-21) =====

    [Fact]
    public async Task All_deferred_defaults_fail_closed_to_proposal_creation_failed()
    {
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        CommandEnvelope? captured = null;
        _ = dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => captured = e), Arg.Any<CancellationToken>());

        var orchestrator = new AgentInteractionProposalOrchestrator(
            new DeferredAgentGeneratedVersionReader(),
            new DeferredProposalExpiryPolicyReader(),
            dispatcher);

        AgentInteractionProposalOutcomeResult outcome = await orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalCreationFailed);
        await dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
        var command = JsonSerializer.Deserialize<CreateProposedAgentReply>(captured!.Payload)!;
        // The deferred version reader returns not-available → GeneratedVersionUnavailable.
        command.Result.Outcome.ShouldBe(AgentProposalCreationOutcome.GeneratedVersionUnavailable);
    }

    // ===== Deterministic id reuse on retry (AC4; AD-13) =====

    [Fact]
    public async Task A_retried_creation_reuses_the_same_deterministic_proposal_id()
    {
        StubHappy();
        var dispatched = new List<CommandEnvelope>();
        _ = _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(dispatched.Add), Arg.Any<CancellationToken>());

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);
        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        dispatched.Count.ShouldBe(2);
        string first = JsonSerializer.Deserialize<CreateProposedAgentReply>(dispatched[0].Payload)!.Result.ProposalId;
        string second = JsonSerializer.Deserialize<CreateProposedAgentReply>(dispatched[1].Payload)!.Result.ProposalId;
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

    // ===== Confirmation-vs-automatic short-circuit (no dispatch) (AC1) =====

    [Fact]
    public async Task An_automatic_mode_request_short_circuits_without_dispatching()
    {
        // The durable owner only invokes this for confirmation mode; an automatic-mode request defensively short-circuits
        // (the aggregate would reject it anyway) — nothing is read, and no command is dispatched.
        CaptureDispatch();

        AgentInteractionProposalOutcomeResult outcome = await Orchestrator.ExecuteAsync(
            Request(responseMode: AgentResponseMode.Automatic), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Generated); // unchanged — no proposal created
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
        await _versionReader.DidNotReceive().ReadSelectedVersionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ===== Cross-seam: the dispatched command drives the real aggregate to the same decision =====

    [Fact]
    public async Task End_to_end_the_dispatched_create_command_drives_the_aggregate_to_the_same_decision()
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionProposalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        var command = JsonSerializer.Deserialize<CreateProposedAgentReply>(dispatched.Payload)!;

        var state = new AgentInteractionState();
        state.Apply(new InteractionRequested(
            AgentInteractionId, AgentId, "party-001", SourceConversationId, ConfirmationSnapshot(), "prompt", "idem"));
        state.Apply(new AgentInteractionAuthorized(AgentInteractionId));
        state.Apply(new AgentInteractionContextReady(AgentInteractionId, SampleContextEvidence()));
        state.Apply(new AgentOutputGenerated(AgentInteractionId, SampleVersion()));
        DomainResult result = AgentInteractionAggregate.Handle(command, state, dispatched);

        ProposedAgentReplyCreated created = result.Events[0].ShouldBeOfType<ProposedAgentReplyCreated>();
        created.Evidence.ProposedVersionId.ShouldBe(VersionId);
        // The orchestrator's returned status and the aggregate's recorded decision agree (shared policy — no drift).
        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);
    }

    // ===== Cancellation propagates (AC3) =====

    [Fact]
    public async Task A_genuine_cancellation_propagates_and_no_command_is_dispatched()
    {
        _versionReader.ReadSelectedVersionAsync(TenantId, AgentInteractionId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Orchestrator.ExecuteAsync(Request(), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_cancellation_during_the_expiry_read_propagates_and_no_command_is_dispatched()
    {
        // The expiry read's fail-closed catch deliberately excludes OperationCanceledException, so a genuine cancellation
        // there must propagate (not silently degrade to no-expiry) and no create command is dispatched.
        StubVersionAvailable();
        _expiryReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Orchestrator.ExecuteAsync(Request(), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Helpers =====

    private void StubHappy()
    {
        StubVersionAvailable();
        StubExpiryNone();
    }

    private void StubVersionAvailable()
        => _versionReader.ReadSelectedVersionAsync(TenantId, AgentInteractionId, Arg.Any<CancellationToken>())
            .Returns(new AgentGeneratedVersionReadResult(AgentGeneratedVersionReadOutcome.Available, VersionId, GeneratedContentText));

    private void StubExpiryNone()
        => _expiryReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(ProposalExpiryPolicyResult.None);

    private static AgentInteractionSnapshot ConfirmationSnapshot()
        => new(
            ConfigurationVersion: 3,
            InstructionsVersion: 2,
            AgentResponseMode.Confirmation,
            ApproverPolicyVersion,
            "openai",
            "gpt-4o",
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion,
            AgentInteractionSnapshot.DefaultContextPolicyReference);

    private static AgentInteractionContextEvidence SampleContextEvidence()
        => new(
            AgentInteractionContextMode.Full,
            FullContextTokenCount: 1_000,
            UsedContextTokenCount: 1_000,
            MessageCount: 3,
            ReservedOutputTokenCount: 16_000,
            ContextWindowTokenLimit: 128_000,
            ProviderCapabilityVersion: 1,
            AgentInteractionSnapshot.DefaultContextPolicyReference,
            BoundedBehaviorReference: null);

    private static AgentGeneratedVersion SampleVersion()
        => new(
            VersionId,
            AttemptId: "attempt-1",
            AgentGenerationKind.Generated,
            GeneratedContentText,
            "openai",
            "gpt-4o",
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion,
            PromptTokenCount: 1_200,
            OutputTokenCount: 350);

    private static AgentInteractionProposalRequest Request(
        IReadOnlyDictionary<string, string>? clientExtensions = null,
        AgentResponseMode responseMode = AgentResponseMode.Confirmation)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentInteractionId,
            AgentId,
            SourceConversationId,
            responseMode,
            ApproverPolicyVersion,
            ContentSafetyPolicyVersion,
            ActorUserId,
            ClientCorrelationId: "client-corr-1",
            clientExtensions);

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;

    private AgentProposalCreationResult DispatchedResult()
        => JsonSerializer.Deserialize<CreateProposedAgentReply>(LastDispatched()!.Payload)!.Result;
}
