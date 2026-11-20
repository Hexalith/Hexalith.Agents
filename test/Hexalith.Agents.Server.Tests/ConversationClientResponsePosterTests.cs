namespace Hexalith.Agents.Server.Tests;

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>
/// Tests for the live <see cref="ConversationClientResponsePoster"/> over the public <c>IConversationClient</c> (Story 2.5;
/// AC1, AC2, AC3; AD-6, AD-7, AD-12, AD-13, AD-14). This is the real Conversations boundary the dev-story orchestrator tests
/// stub out: it verifies the Agent's <c>AiAgent</c> membership via <c>GetConversationAsync</c> participants (Present only when
/// the Agent <c>PartyId</c> is present <em>and</em> typed <c>AiAgent</c>; otherwise fail closed — there is no public
/// establish seam, so absence → <c>SeamUnavailable</c>), and appends the message authored by the Agent Party with the
/// deterministic <c>MessageId</c>/idempotency key. Every transport/read fault fails closed without propagating the raw error
/// (AD-12, AD-14); a genuine cancellation still propagates. The sensitive append <c>Text</c> stays inside the adapter.
/// </summary>
public sealed class ConversationClientResponsePosterTests
{
    private const string TenantId = "acme";
    private const string SourceConversationId = "conversation-001";
    private const string AgentPartyId = "agent-party-001";
    private const string OtherPartyId = "human-party-999";
    private const string ActorPrincipalId = "principal-1";
    private const string CorrelationId = "corr-1";
    private const string MessageIdValue = "post-message-hash-001";
    private const string IdempotencyKeyValue = "post-idempotency-hash-001";
    private const string GeneratedContentText = "the-generated-answer-do-not-leak";

    private static readonly DateTimeOffset _eventTimestamp = new(2026, 6, 24, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _generatedAt = new(2026, 6, 24, 9, 0, 5, TimeSpan.Zero);

    private readonly IConversationClient _client = Substitute.For<IConversationClient>();

    private ConversationClientResponsePoster Poster => new(_client);

    public ConversationClientResponsePosterTests()
    {
        _client.GetAgentConversationAsync(Arg.Any<ConversationAgentReadQuery>(), Arg.Any<CancellationToken>())
            .Returns(Observation(ConversationAgentsOutcome.Absent));
    }

    [Theory]
    [InlineData("other", SourceConversationId)]
    [InlineData(TenantId, "other")]
    public async Task Foreign_details_cannot_prove_membership(string tenant, string conversation)
    {
        var details = new ConversationDetailsV1(SchemaVersion.Current, new TenantId(tenant),
            new ConversationId(conversation), FreshFreshness(), "Active", Participants: [Participant(AgentPartyId, ParticipantType.AiAgent)]);
        StubGetConversation(Visible(details));

        (await EnsureMembership()).Outcome.ShouldBe(ConversationMembershipOutcome.ConversationUnavailable);
    }

    // ===== Membership verify: present only when the Agent PartyId is an AiAgent participant (AC1) =====

    [Fact]
    public async Task Agent_present_as_ai_agent_maps_to_present()
    {
        StubGetConversation(Visible(Details(Participant(AgentPartyId, ParticipantType.AiAgent))));

        ConversationMembershipResult result = await EnsureMembership();

        result.Outcome.ShouldBe(ConversationMembershipOutcome.Present);
    }

    [Fact]
    public async Task Agent_present_only_as_a_human_participant_fails_closed_to_seam_unavailable()
    {
        // The same Party present but typed Human (not AiAgent) is NOT the AiAgent membership AC1 requires — and there is no
        // public establish seam to upgrade it, so fail closed (AD-6, AD-7). This is the type-discrimination at the heart of AC1.
        StubGetConversation(Visible(Details(Participant(AgentPartyId, ParticipantType.Human))));

        ConversationMembershipResult result = await EnsureMembership();

        result.Outcome.ShouldBe(ConversationMembershipOutcome.SeamUnavailable);
    }

    [Fact]
    public async Task A_different_party_typed_ai_agent_does_not_satisfy_the_agent_membership()
    {
        // A different Party is the conversation's AiAgent — the Agent's own PartyId is absent, so membership fails closed.
        StubGetConversation(Visible(Details(Participant(OtherPartyId, ParticipantType.AiAgent))));

        ConversationMembershipResult result = await EnsureMembership();

        result.Outcome.ShouldBe(ConversationMembershipOutcome.SeamUnavailable);
    }

    [Fact]
    public async Task No_participants_fails_closed_to_seam_unavailable()
    {
        StubGetConversation(Visible(Details()));

        ConversationMembershipResult result = await EnsureMembership();

        result.Outcome.ShouldBe(ConversationMembershipOutcome.SeamUnavailable);
    }

    [Fact]
    public async Task Agent_party_match_is_case_and_whitespace_insensitive()
    {
        // PartyId normalizes (trim + ordinal lowercase) — a request whose AgentPartyId differs from the recorded participant
        // only by surrounding whitespace/casing must still match, closing the substitution-bypass where a near-identical id
        // would look like a different identity (AD-7). The participant is the canonical lowercase id.
        StubGetConversation(Visible(Details(Participant(AgentPartyId, ParticipantType.AiAgent))));

        ConversationMembershipResult result = await EnsureMembership(agentPartyId: "  AGENT-PARTY-001 ");

        result.Outcome.ShouldBe(ConversationMembershipOutcome.Present);
    }

    [Fact]
    public async Task A_visible_but_stale_projection_fails_closed_to_conversation_unavailable_even_when_the_agent_is_present()
    {
        // Membership is a trust-bearing decision (AD-7): a visible-but-stale projection still carries the participant list,
        // but it may not reflect a revoked membership — so even with the Agent present as AiAgent, a non-trust-bearing read
        // must fail closed (Task 7: "stale conversation → ConversationUnavailable"), mirroring the context reader.
        StubGetConversation(Visible(StaleDetails(Participant(AgentPartyId, ParticipantType.AiAgent))));

        ConversationMembershipResult result = await EnsureMembership();

        result.Outcome.ShouldBe(ConversationMembershipOutcome.ConversationUnavailable);
    }

    [Fact]
    public async Task A_hidden_read_with_no_details_fails_closed_to_conversation_unavailable()
    {
        // A Hidden read carries no details (coarse — existence never leaks; AC1) → fail closed.
        StubGetConversation(ConversationClientResult<ConversationDetailResult>.Success(
            ConversationDetailResult.Hidden(SchemaVersion.Current), HttpStatusCode.OK));

        ConversationMembershipResult result = await EnsureMembership();

        result.Outcome.ShouldBe(ConversationMembershipOutcome.ConversationUnavailable);
    }

    [Fact]
    public async Task A_failure_client_result_fails_closed_to_conversation_unavailable()
    {
        StubGetConversation(ConversationClientResult<ConversationDetailResult>.Failure(
            new ConversationErrorResult([Error()]), HttpStatusCode.ServiceUnavailable));

        ConversationMembershipResult result = await EnsureMembership();

        result.Outcome.ShouldBe(ConversationMembershipOutcome.ConversationUnavailable);
    }

    [Fact]
    public async Task A_thrown_membership_read_fails_closed_to_conversation_unavailable_without_propagating()
    {
        // A transport fault fails closed (AD-12) and the raw error never crosses the boundary — the result is structurally a
        // safe enum only, so there is nothing to leak (AD-14).
        _client.GetConversationAsync(Arg.Any<GetConversationQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("conversations read blew up: secret-bearing detail"));

        ConversationMembershipResult result = await EnsureMembership();

        result.Outcome.ShouldBe(ConversationMembershipOutcome.ConversationUnavailable);
    }

    [Fact]
    public async Task A_genuine_cancellation_propagates_from_membership()
    {
        _client.GetConversationAsync(Arg.Any<GetConversationQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(async () => await EnsureMembership());
    }

    // ===== Append: authored by the Agent Party with the deterministic identity (AC2, AC3) =====

    [Fact]
    public async Task Acceptance_keeps_the_exact_agent_message_pending_until_the_source_confirms_it()
    {
        AppendMessageCommand? captured = null;
        _client.AppendMessageAsync(Arg.Do<AppendMessageCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(AcceptedSuccess());

        ConversationAppendResult result = await Append();

        result.Outcome.ShouldBe(ConversationAppendOutcome.AwaitingConfirmation);

        AppendMessageCommand command = captured.ShouldNotBeNull();
        command.ConversationId.ShouldBe(new ConversationId(SourceConversationId));
        command.MessageId.Value.ShouldBe(MessageIdValue);                  // deterministic message id (AD-13)
        command.AuthorPartyId.ShouldBe(new PartyId(AgentPartyId));         // authored by the Agent Party, not the caller (AC2)
        command.Text.ShouldBe(GeneratedContentText);                       // the sensitive content stays inside the adapter
        command.Metadata.ActorPartyId.ShouldBe(new PartyId(AgentPartyId)); // the actor is the Agent Party too (AC2)
        command.Metadata.TenantId.ShouldBe(new TenantId(TenantId));        // tenant-scoped (FR-19)
        command.Metadata.IdempotencyKey.ShouldBe(IdempotencyKeyValue);     // deterministic dedupe key (AC3; AD-13)
        command.Metadata.CorrelationId.ShouldBe(CorrelationId);
    }

    [Fact]
    public async Task An_append_rejected_by_conversations_maps_to_post_rejected()
    {
        _client.AppendMessageAsync(Arg.Any<AppendMessageCommand>(), Arg.Any<CancellationToken>())
            .Returns(ConversationClientResult<ConversationCommandAcceptedResult>.Failure(
                new ConversationErrorResult([new ConversationError(SchemaVersion.Current, ConversationErrorCode.CommandValidationFailed,
                    ConversationErrorCategory.Validation, IsRetryable: false, CorrelationId)]), HttpStatusCode.BadRequest));

        ConversationAppendResult result = await Append();

        result.Outcome.ShouldBe(ConversationAppendOutcome.PostRejected);
    }

    [Fact]
    public async Task A_lost_append_acknowledgement_keeps_the_result_unconfirmed()
    {
        // The thrown error carries both a secret and the generated content — neither may cross the boundary; the adapter
        // fails closed (AD-12) and the result is a safe enum only (AD-14).
        _client.AppendMessageAsync(Arg.Any<AppendMessageCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException($"transport blew up: secret + {GeneratedContentText}"));

        ConversationAppendResult result = await Append();

        result.Outcome.ShouldBe(ConversationAppendOutcome.AwaitingConfirmation);
    }

    [Fact]
    public async Task A_genuine_cancellation_propagates_from_append()
    {
        _client.AppendMessageAsync(Arg.Any<AppendMessageCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(async () => await Append());
    }

    // ===== Helpers =====

    [Fact]
    public async Task A_later_exact_source_receipt_confirms_an_accepted_message_without_resending()
    {
        var absent = Observation(ConversationAgentsOutcome.Absent);
        var confirmed = Observation(ConversationAgentsOutcome.Available, MessageIdValue, AgentPartyId);
        _client.GetAgentConversationAsync(Arg.Is<ConversationAgentReadQuery>(q =>
            q.TenantId == new TenantId(TenantId) && q.ConversationId == new ConversationId(SourceConversationId)
            && q.MessageId == new MessageId(MessageIdValue)), Arg.Any<CancellationToken>())
            .Returns(absent, absent, confirmed);
        _client.AppendMessageAsync(Arg.Any<AppendMessageCommand>(), Arg.Any<CancellationToken>()).Returns(AcceptedSuccess());

        (await Append()).Outcome.ShouldBe(ConversationAppendOutcome.AwaitingConfirmation);
        (await Append()).Outcome.ShouldBe(ConversationAppendOutcome.Posted);
        await _client.Received(1).AppendMessageAsync(Arg.Any<AppendMessageCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restart_after_lost_acknowledgement_confirms_one_persisted_message_through_the_automatic_orchestrator()
    {
        const string interaction = "call-1";
        const string version = "version-1";
        var request = new AgentInteractionPostingRequest("command-1", CorrelationId, TenantId, interaction, "hexa",
            SourceConversationId, AgentResponseMode.Automatic, ActorPrincipalId);
        var party = Substitute.For<IAgentPartyReader>();
        party.ReadAsync(TenantId, "hexa", Arg.Any<CancellationToken>())
            .Returns(new AgentPartyReadResult(AgentPartyReadOutcome.Available, AgentPartyId));
        var versions = Substitute.For<IAgentGeneratedVersionReader>();
        versions.ReadSelectedVersionAsync(TenantId, interaction, Arg.Any<CancellationToken>())
            .Returns(new AgentGeneratedVersionReadResult(AgentGeneratedVersionReadOutcome.Available, version, GeneratedContentText));
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        AgentCommandIntent? receipt = null;
        _ = dispatcher.DispatchAsync(Arg.Do<AgentCommandIntent>(intent => receipt = intent), Arg.Any<CancellationToken>());
        StubGetConversation(Visible(Details(Participant(AgentPartyId, ParticipantType.AiAgent))));

        var persisted = new Dictionary<MessageId, AppendMessageCommand>();
        _client.GetAgentConversationAsync(Arg.Any<ConversationAgentReadQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var query = call.Arg<ConversationAgentReadQuery>();
                return query.MessageId is { } key && persisted.TryGetValue(key, out var command)
                    ? Observation(ConversationAgentsOutcome.Available, key.Value, command.AuthorPartyId.Value)
                    : Observation(ConversationAgentsOutcome.Absent);
            });
        _client.AppendMessageAsync(Arg.Any<AppendMessageCommand>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var command = call.Arg<AppendMessageCommand>();
                persisted.Add(command.MessageId, command);
                return Task.FromException<ConversationClientResult<ConversationCommandAcceptedResult>>(
                    new HttpRequestException("Acknowledgement lost"));
            });

        var first = new AgentInteractionPostingOrchestrator(party, versions, Poster, dispatcher);
        (await first.ExecuteAsync(request, CancellationToken.None)).Status.ShouldBe(AgentInteractionStatus.Generated);
        receipt.ShouldBeNull();
        var restarted = new AgentInteractionPostingOrchestrator(party, versions, Poster, dispatcher);
        (await restarted.ExecuteAsync(request, CancellationToken.None)).Status.ShouldBe(AgentInteractionStatus.Posted);

        var posted = System.Text.Json.JsonSerializer.Deserialize<PostAgentResponse>(receipt.ShouldNotBeNull().Payload)!.Result;
        posted.PostedVersionId.ShouldBe(version);
        persisted.Single().Key.Value.ShouldBe(posted.MessageId);
        await _client.Received(1).AppendMessageAsync(Arg.Any<AppendMessageCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("other", SourceConversationId, MessageIdValue, AgentPartyId)]
    [InlineData(TenantId, "other", MessageIdValue, AgentPartyId)]
    [InlineData(TenantId, SourceConversationId, "other", AgentPartyId)]
    [InlineData(TenantId, SourceConversationId, MessageIdValue, OtherPartyId)]
    public async Task Inconsistent_source_proof_neither_confirms_nor_retries_the_append(string tenant, string conversation,
        string message, string author)
    {
        var proof = Observation(ConversationAgentsOutcome.Available, message, author) with
        {
            TenantId = new TenantId(tenant),
            ConversationId = new ConversationId(conversation),
        };
        _client.GetAgentConversationAsync(Arg.Any<ConversationAgentReadQuery>(), Arg.Any<CancellationToken>()).Returns(proof);

        (await Append()).Outcome.ShouldBe(ConversationAppendOutcome.AwaitingConfirmation);
        await _client.DidNotReceive().AppendMessageAsync(Arg.Any<AppendMessageCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("different", false, false)]
    [InlineData(GeneratedContentText, true, false)]
    [InlineData(GeneratedContentText, false, true)]
    public async Task A_conflicting_or_undisclosable_message_cannot_confirm_the_selected_version(string text, bool redacted, bool wrongTrace)
    {
        var proof = Observation(ConversationAgentsOutcome.Available, MessageIdValue, AgentPartyId);
        var message = proof.Messages!.Single() with
        {
            Text = text,
            Redacted = redacted,
            Provenance = wrongTrace ? new AgentMessageProvenance("wrong-call", true, false) : null,
        };
        _client.GetAgentConversationAsync(Arg.Any<ConversationAgentReadQuery>(), Arg.Any<CancellationToken>())
            .Returns(proof with { Messages = [message] });

        (await Append()).Outcome.ShouldBe(ConversationAppendOutcome.AwaitingConfirmation);
        await _client.DidNotReceive().AppendMessageAsync(Arg.Any<AppendMessageCommand>(), Arg.Any<CancellationToken>());
    }

    private static ConversationAgentReadResult Observation(ConversationAgentsOutcome outcome, string? message = null, string? author = null)
        => new(outcome, new TenantId(TenantId), new ConversationId(SourceConversationId), 1, _generatedAt, "checkpoint",
            Messages: message is null ? null : [new ConversationAgentMessage(new MessageId(message), new PartyId(author!),
                GeneratedContentText, _eventTimestamp, null, false, false, null)], AgentParticipantPresent: true);

    private Task<ConversationMembershipResult> EnsureMembership(string agentPartyId = AgentPartyId)
        => Poster.EnsureAiAgentParticipantAsync(
            new ConversationMembershipRequest(TenantId, SourceConversationId, agentPartyId, ActorPrincipalId, CorrelationId),
            CancellationToken.None);

    private Task<ConversationAppendResult> Append()
        => Poster.AppendAgentMessageAsync(
            new ConversationAppendRequest(
                TenantId,
                SourceConversationId,
                AgentPartyId,
                GeneratedContentText,
                MessageIdValue,
                IdempotencyKeyValue,
                CorrelationId),
            CancellationToken.None);

    private void StubGetConversation(ConversationClientResult<ConversationDetailResult> result)
        => _client.GetConversationAsync(Arg.Any<GetConversationQuery>(), Arg.Any<CancellationToken>()).Returns(result);

    private static ConversationClientResult<ConversationDetailResult> Visible(ConversationDetailsV1 details)
        => ConversationClientResult<ConversationDetailResult>.Success(
            ConversationDetailResult.Visible(SchemaVersion.Current, details, "ok"), HttpStatusCode.OK);

    private static ConversationDetailsV1 Details(params ConversationParticipantProjectionV1[] participants)
        => new(
            SchemaVersion.Current,
            new TenantId(TenantId),
            new ConversationId(SourceConversationId),
            FreshFreshness(),
            LifecycleState: "Active",
            Participants: participants);

    private static ConversationDetailsV1 StaleDetails(params ConversationParticipantProjectionV1[] participants)
        => new(
            SchemaVersion.Current,
            new TenantId(TenantId),
            new ConversationId(SourceConversationId),
            StaleFreshness(),
            LifecycleState: "Active",
            Participants: participants);

    private static ConversationParticipantProjectionV1 Participant(string partyId, ParticipantType type)
        => new(new PartyId(partyId), type, ParticipantRole.Member);

    private static ConversationClientResult<ConversationCommandAcceptedResult> AcceptedSuccess()
        => ConversationClientResult<ConversationCommandAcceptedResult>.Success(
            new ConversationCommandAcceptedResult(
                SchemaVersion.Current,
                new TenantId(TenantId),
                new ConversationId(SourceConversationId),
                ConversationCommandType.AppendMessageCommand,
                CorrelationId,
                IdempotencyKeyValue,
                new ReadModelVisibility(ProjectionTrustState.Current)),
            HttpStatusCode.Accepted);

    private static ConversationError Error()
        => new(
            SchemaVersion.Current,
            ConversationErrorCode.TenantProjectionStale,
            ConversationErrorCategory.Freshness,
            IsRetryable: true,
            CorrelationId: "trace-1");

    private static ProjectionFreshnessV1 FreshFreshness()
        => new(
            SchemaVersion.Current,
            "cursor-1",
            1L,
            _eventTimestamp,
            _generatedAt,
            TimeSpan.Zero,
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);

    private static ProjectionFreshnessV1 StaleFreshness()
        => new(
            SchemaVersion.Current,
            "cursor-1",
            1L,
            _eventTimestamp,
            _generatedAt,
            TimeSpan.FromMinutes(30),
            IsStale: true,
            ProjectionTrustState.Stale,
            ProjectionFreshnessReasonCode.StaleThresholdExceeded);
}
