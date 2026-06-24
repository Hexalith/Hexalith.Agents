namespace Hexalith.Agents.Server.Tests;

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Server.Ports;

using Hexalith.Conversations.Client;
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
    public async Task Append_success_authors_the_message_as_the_agent_party_with_the_deterministic_identity()
    {
        AppendMessageCommand? captured = null;
        _client.AppendMessageAsync(Arg.Do<AppendMessageCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(AcceptedSuccess());

        ConversationAppendResult result = await Append();

        result.Outcome.ShouldBe(ConversationAppendOutcome.Posted);

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
                new ConversationErrorResult([Error()]), HttpStatusCode.BadRequest));

        ConversationAppendResult result = await Append();

        result.Outcome.ShouldBe(ConversationAppendOutcome.PostRejected);
    }

    [Fact]
    public async Task A_thrown_append_fails_closed_to_adapter_failure_without_propagating_the_content_or_error()
    {
        // The thrown error carries both a secret and the generated content — neither may cross the boundary; the adapter
        // fails closed (AD-12) and the result is a safe enum only (AD-14).
        _client.AppendMessageAsync(Arg.Any<AppendMessageCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException($"transport blew up: secret + {GeneratedContentText}"));

        ConversationAppendResult result = await Append();

        result.Outcome.ShouldBe(ConversationAppendOutcome.AdapterFailure);
    }

    [Fact]
    public async Task A_genuine_cancellation_propagates_from_append()
    {
        _client.AppendMessageAsync(Arg.Any<AppendMessageCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(async () => await Append());
    }

    // ===== Helpers =====

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
