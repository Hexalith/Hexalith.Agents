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
/// Tests for <see cref="AgentInteractionPostingOrchestrator"/> (Story 2.5; AC1–AC4; AD-3, AD-6, AD-7, AD-13, AD-14). Verify
/// the happy path dispatches <see cref="PostAgentResponse"/> with the right domain/aggregate id, author = the Agent Party,
/// and a deterministic <c>MessageId</c> derived from interaction+version, returning <c>Posted</c> with NO content in the
/// envelope or result; that membership-seam-unavailable skips the append; that party-not-available / append-rejected /
/// Conversations-throw each map correctly without leaking; that the all-deferred default graph fails closed; that a retry
/// reuses the deterministic ids; that confirmation mode short-circuits without dispatch; that the dispatched command drives
/// the real aggregate to the same decision; and that a genuine cancellation propagates without dispatching. The shared
/// posting policy is not visible to this assembly, so decisions are asserted via the returned status + dispatched outcome.
/// </summary>
public sealed class AgentInteractionPostingOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentInteractionId = "interaction-001";
    private const string AgentId = "hexa";
    private const string SourceConversationId = "conversation-001";
    private const string ActorUserId = "caller-user";
    private const string AgentPartyId = "agent-party-001";
    private const string VersionId = "version-attempt-1";
    private const string GeneratedContentText = "the-generated-answer-do-not-leak";

    private readonly IAgentPartyReader _partyReader = Substitute.For<IAgentPartyReader>();
    private readonly IAgentGeneratedVersionReader _versionReader = Substitute.For<IAgentGeneratedVersionReader>();
    private readonly IConversationResponsePoster _poster = Substitute.For<IConversationResponsePoster>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private CommandEnvelope? _lastDispatched;
    private ConversationAppendRequest? _lastAppend;

    private AgentInteractionPostingOrchestrator Orchestrator => new(
        _partyReader,
        _versionReader,
        _poster,
        _dispatcher);

    // ===== Happy path → Posted (AC1, AC2, AC3) =====

    [Fact]
    public async Task Posted_dispatches_post_with_the_agent_party_author_deterministic_id_and_returns_posted()
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionPostingOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.AgentInteractionId.ShouldBe(AgentInteractionId);
        outcome.Status.ShouldBe(AgentInteractionStatus.Posted);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(PostAgentResponse));
        dispatched.Domain.ShouldBe("agent-interaction");
        dispatched.AggregateId.ShouldBe(AgentInteractionId);

        AgentResponsePostingResult result = DispatchedResult();
        result.Outcome.ShouldBe(AgentResponsePostingOutcome.Posted);
        result.AgentPartyId.ShouldBe(AgentPartyId); // authored by the Agent Party identity, not the caller (AC2)
        result.PostedVersionId.ShouldBe(VersionId);
        result.MessageId.ShouldBe(AgentResponsePostingIdentity.DeriveMessageId(AgentInteractionId, VersionId)); // deterministic (AD-13)

        // The append was authored by the Agent Party with the deterministic message id + idempotency key (AC2, AC3).
        ConversationAppendRequest append = LastAppend().ShouldNotBeNull();
        append.AuthorPartyId.ShouldBe(AgentPartyId);
        append.MessageId.ShouldBe(AgentResponsePostingIdentity.DeriveMessageId(AgentInteractionId, VersionId));
        append.IdempotencyKey.ShouldBe(AgentResponsePostingIdentity.DeriveIdempotencyKey(AgentInteractionId, VersionId));
    }

    [Fact]
    public async Task The_dispatched_command_and_returned_outcome_carry_no_generated_content()
    {
        // The content is handed to the poster's append (transient, inside the adapter) but never rides into the aggregate
        // command/result or back to the caller (AD-14).
        StubHappy();
        CaptureDispatch();

        AgentInteractionPostingOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldNotContain(GeneratedContentText);
        JsonSerializer.Serialize(outcome).ShouldNotContain(GeneratedContentText);
        // ... but the content WAS handed to the poster's append (proving it flows only there).
        LastAppend().ShouldNotBeNull().Text.ShouldBe(GeneratedContentText);
    }

    // ===== Membership seam unavailable → MembershipUnavailable, append never called (AC1) =====

    [Fact]
    public async Task Membership_seam_unavailable_returns_membership_unavailable_and_never_appends()
    {
        _partyReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyReadResult(AgentPartyReadOutcome.Available, AgentPartyId));
        StubVersionAvailable();
        _poster.EnsureAiAgentParticipantAsync(Arg.Any<ConversationMembershipRequest>(), Arg.Any<CancellationToken>())
            .Returns(ConversationMembershipResult.SeamUnavailable);
        CaptureDispatch();

        AgentInteractionPostingOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        DispatchedResult().Outcome.ShouldBe(AgentResponsePostingOutcome.MembershipUnavailable);
        await _poster.DidNotReceive().AppendAgentMessageAsync(Arg.Any<ConversationAppendRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Membership_rejected_returns_membership_rejected()
    {
        _partyReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyReadResult(AgentPartyReadOutcome.Available, AgentPartyId));
        StubVersionAvailable();
        _poster.EnsureAiAgentParticipantAsync(Arg.Any<ConversationMembershipRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ConversationMembershipResult(ConversationMembershipOutcome.MembershipRejected));
        CaptureDispatch();

        AgentInteractionPostingOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        DispatchedResult().Outcome.ShouldBe(AgentResponsePostingOutcome.MembershipRejected);
    }

    // ===== Party not available → PartyIdentityUnavailable, nothing else read (AC1) =====

    [Fact]
    public async Task Party_not_available_returns_party_identity_unavailable_without_reading_version_or_posting()
    {
        _partyReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyReadResult(AgentPartyReadOutcome.NotLinked, null));
        CaptureDispatch();

        AgentInteractionPostingOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        DispatchedResult().Outcome.ShouldBe(AgentResponsePostingOutcome.PartyIdentityUnavailable);
        await _versionReader.DidNotReceive().ReadSelectedVersionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _poster.DidNotReceive().EnsureAiAgentParticipantAsync(Arg.Any<ConversationMembershipRequest>(), Arg.Any<CancellationToken>());
        await _poster.DidNotReceive().AppendAgentMessageAsync(Arg.Any<ConversationAppendRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Version_not_available_returns_adapter_failure_without_posting()
    {
        _partyReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyReadResult(AgentPartyReadOutcome.Available, AgentPartyId));
        _versionReader.ReadSelectedVersionAsync(TenantId, AgentInteractionId, Arg.Any<CancellationToken>())
            .Returns(AgentGeneratedVersionReadResult.NotAvailable);
        CaptureDispatch();

        AgentInteractionPostingOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        DispatchedResult().Outcome.ShouldBe(AgentResponsePostingOutcome.AdapterFailure);
        await _poster.DidNotReceive().AppendAgentMessageAsync(Arg.Any<ConversationAppendRequest>(), Arg.Any<CancellationToken>());
    }

    // ===== Append rejected / Conversations throw → fail closed, no leak (AC3, AC4; AD-14) =====

    [Fact]
    public async Task Append_rejected_returns_post_rejected()
    {
        StubPartyAndVersionAndMembership();
        _poster.AppendAgentMessageAsync(Arg.Any<ConversationAppendRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ConversationAppendResult(ConversationAppendOutcome.PostRejected));
        CaptureDispatch();

        AgentInteractionPostingOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        DispatchedResult().Outcome.ShouldBe(AgentResponsePostingOutcome.PostRejected);
    }

    [Fact]
    public async Task An_append_that_throws_fails_closed_to_adapter_failure_without_leaking_the_error_or_content()
    {
        StubPartyAndVersionAndMembership();
        _poster.AppendAgentMessageAsync(Arg.Any<ConversationAppendRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("conversations transport blew up: secret-bearing detail"));
        CaptureDispatch();

        AgentInteractionPostingOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        DispatchedResult().Outcome.ShouldBe(AgentResponsePostingOutcome.AdapterFailure);
        string payload = Encoding.UTF8.GetString(LastDispatched()!.Payload);
        payload.ShouldNotContain("secret-bearing detail");
        payload.ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public async Task A_membership_check_that_throws_fails_closed_to_conversation_unavailable()
    {
        _partyReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyReadResult(AgentPartyReadOutcome.Available, AgentPartyId));
        StubVersionAvailable();
        _poster.EnsureAiAgentParticipantAsync(Arg.Any<ConversationMembershipRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("conversations read blew up: secret"));
        CaptureDispatch();

        AgentInteractionPostingOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        DispatchedResult().Outcome.ShouldBe(AgentResponsePostingOutcome.ConversationUnavailable);
        JsonSerializer.Serialize(DispatchedResult()).ShouldNotContain("secret");
    }

    // ===== All-deferred default graph fails closed (AC1; FR-21) =====

    [Fact]
    public async Task All_deferred_defaults_fail_closed_to_posting_failed()
    {
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        CommandEnvelope? captured = null;
        _ = dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => captured = e), Arg.Any<CancellationToken>());

        var orchestrator = new AgentInteractionPostingOrchestrator(
            new DeferredAgentPartyReader(),
            new DeferredAgentGeneratedVersionReader(),
            new DeferredConversationResponsePoster(),
            dispatcher);

        AgentInteractionPostingOutcomeResult outcome = await orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        await dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
        var command = JsonSerializer.Deserialize<PostAgentResponse>(captured!.Payload)!;
        // The first deferred seam on the path is the Agent-Party read → PartyIdentityUnavailable.
        command.Result.Outcome.ShouldBe(AgentResponsePostingOutcome.PartyIdentityUnavailable);
    }

    // ===== Deterministic id reuse on retry (AC3; AD-13) =====

    [Fact]
    public async Task A_retried_post_reuses_the_same_deterministic_message_id_and_idempotency_key()
    {
        StubHappy();
        var appends = new List<ConversationAppendRequest>();
        _poster.AppendAgentMessageAsync(Arg.Do<ConversationAppendRequest>(appends.Add), Arg.Any<CancellationToken>())
            .Returns(new ConversationAppendResult(ConversationAppendOutcome.Posted));

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);
        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        appends.Count.ShouldBe(2);
        appends[1].MessageId.ShouldBe(appends[0].MessageId);
        appends[1].IdempotencyKey.ShouldBe(appends[0].IdempotencyKey);
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

    // ===== Confirmation-mode short-circuit (no dispatch) (AC1) =====

    [Fact]
    public async Task A_confirmation_mode_request_short_circuits_without_dispatching()
    {
        // The durable owner only invokes this for automatic mode; a confirmation-mode request defensively short-circuits
        // (the aggregate would reject it anyway) — nothing is read or posted, and no command is dispatched.
        CaptureDispatch();

        AgentInteractionPostingOutcomeResult outcome = await Orchestrator.ExecuteAsync(
            Request(responseMode: AgentResponseMode.Confirmation), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Generated); // unchanged — not posted
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
        await _partyReader.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _poster.DidNotReceive().AppendAgentMessageAsync(Arg.Any<ConversationAppendRequest>(), Arg.Any<CancellationToken>());
    }

    // ===== Cross-seam: the dispatched command drives the real aggregate to the same decision =====

    [Fact]
    public async Task End_to_end_the_dispatched_post_command_drives_the_aggregate_to_the_same_decision()
    {
        StubHappy();
        CaptureDispatch();

        AgentInteractionPostingOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        var command = JsonSerializer.Deserialize<PostAgentResponse>(dispatched.Payload)!;

        var state = new AgentInteractionState();
        state.Apply(new InteractionRequested(
            AgentInteractionId, AgentId, "party-001", SourceConversationId, SampleSnapshot(), "prompt", "idem"));
        state.Apply(new AgentInteractionAuthorized(AgentInteractionId));
        state.Apply(new AgentInteractionContextReady(AgentInteractionId, SampleContextEvidence()));
        state.Apply(new AgentOutputGenerated(AgentInteractionId, SampleVersion()));
        DomainResult result = AgentInteractionAggregate.Handle(command, state, dispatched);

        AgentResponsePosted posted = result.Events[0].ShouldBeOfType<AgentResponsePosted>();
        posted.Evidence.AgentPartyId.ShouldBe(AgentPartyId);
        // The orchestrator's returned status and the aggregate's recorded decision agree (shared policy — no drift).
        outcome.Status.ShouldBe(AgentInteractionStatus.Posted);
    }

    // ===== Cancellation propagates (AC3) =====

    [Fact]
    public async Task A_genuine_cancellation_propagates_and_no_command_is_dispatched()
    {
        _partyReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Orchestrator.ExecuteAsync(Request(), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Helpers =====

    private void StubHappy()
    {
        StubPartyAndVersionAndMembership();
        _poster.AppendAgentMessageAsync(Arg.Do<ConversationAppendRequest>(r => _lastAppend = r), Arg.Any<CancellationToken>())
            .Returns(new ConversationAppendResult(ConversationAppendOutcome.Posted));
    }

    private void StubPartyAndVersionAndMembership()
    {
        _partyReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyReadResult(AgentPartyReadOutcome.Available, AgentPartyId));
        StubVersionAvailable();
        _poster.EnsureAiAgentParticipantAsync(Arg.Any<ConversationMembershipRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ConversationMembershipResult(ConversationMembershipOutcome.Present));
    }

    private void StubVersionAvailable()
        => _versionReader.ReadSelectedVersionAsync(TenantId, AgentInteractionId, Arg.Any<CancellationToken>())
            .Returns(new AgentGeneratedVersionReadResult(AgentGeneratedVersionReadOutcome.Available, VersionId, GeneratedContentText));

    private static AgentInteractionSnapshot SampleSnapshot()
        => new(
            ConfigurationVersion: 3,
            InstructionsVersion: 2,
            AgentResponseMode.Automatic,
            ApproverPolicyVersion: 1,
            "openai",
            "gpt-4o",
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion: 1,
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
            ContentSafetyPolicyVersion: 1,
            PromptTokenCount: 1_200,
            OutputTokenCount: 350);

    private static AgentInteractionPostingRequest Request(
        IReadOnlyDictionary<string, string>? clientExtensions = null,
        AgentResponseMode responseMode = AgentResponseMode.Automatic)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentInteractionId,
            AgentId,
            SourceConversationId,
            responseMode,
            ActorUserId,
            ClientCorrelationId: "client-corr-1",
            clientExtensions);

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;

    private ConversationAppendRequest? LastAppend() => _lastAppend;

    private AgentResponsePostingResult DispatchedResult()
        => JsonSerializer.Deserialize<PostAgentResponse>(LastDispatched()!.Payload)!.Result;
}
