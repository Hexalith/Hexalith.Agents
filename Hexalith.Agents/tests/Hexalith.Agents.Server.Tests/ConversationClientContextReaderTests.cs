namespace Hexalith.Agents.Server.Tests;

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Ports;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>
/// Tests for <see cref="ConversationClientContextReader"/> (Story 2.3; AC1, AC2, AC3; AD-6, AD-12, AD-14). Verify the
/// <c>GetConversationAsync</c> mapping: a visible + trust-bearing read → <c>Loaded</c> with the ordered visible
/// messages; <c>Hidden</c>/<c>Redacted</c> → <c>Unauthorized</c>; <c>Unavailable</c> → <c>Unavailable</c>; a
/// not-trust-bearing freshness → <c>Stale</c>; and a client <c>!IsSuccess</c> result or a thrown exception → a
/// fail-closed <c>Unavailable</c> result carrying no messages and no raw error (AD-14).
/// </summary>
public sealed class ConversationClientContextReaderTests
{
    private const string TenantId = "acme";
    private const string SourceConversationId = "conversation-001";
    private const string CallerPartyId = "party-001";
    private const string CallerPrincipalId = "principal-1";

    private static readonly DateTimeOffset _eventTimestamp = new(2026, 6, 24, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _generatedAt = new(2026, 6, 24, 9, 0, 5, TimeSpan.Zero);

    private readonly IConversationClient _client = Substitute.For<IConversationClient>();

    private ConversationClientContextReader Reader => new(_client);

    [Fact]
    public async Task Visible_and_fresh_maps_to_loaded_with_ordered_messages()
    {
        // Two messages out of chronological order — the reader must order them by CreatedAt.
        ConversationDetailsV1 details = Details(
            FreshFreshness(),
            Message("second", _eventTimestamp.AddMinutes(2)),
            Message("first", _eventTimestamp.AddMinutes(1)));
        StubGetConversation(ConversationClientResult<ConversationDetailResult>.Success(
            ConversationDetailResult.Visible(SchemaVersion.Current, details, "ok"), HttpStatusCode.OK));

        ConversationContextReadResult result = await Read();

        result.Outcome.ShouldBe(AgentInteractionContextLoadOutcome.Loaded);
        result.MessageCount.ShouldBe(2);
        result.Messages.ShouldNotBeNull();
        result.Messages![0].Text.ShouldBe("first");
        result.Messages![1].Text.ShouldBe("second");
        result.IsFresh.ShouldBeTrue();
    }

    [Fact]
    public async Task Hidden_maps_to_unauthorized()
    {
        StubGetConversation(ConversationClientResult<ConversationDetailResult>.Success(
            ConversationDetailResult.Hidden(SchemaVersion.Current), HttpStatusCode.OK));

        ConversationContextReadResult result = await Read();

        result.Outcome.ShouldBe(AgentInteractionContextLoadOutcome.Unauthorized);
        result.Messages.ShouldBeNull();
    }

    [Fact]
    public async Task Redacted_with_no_details_maps_to_unauthorized()
    {
        // A Redacted no-details result is an access denial (coarse — no cross-tenant disclosure; AC1).
        var redacted = new ConversationDetailResult(
            SchemaVersion.Current, ProjectionTrustState.Redacted, ProjectionFreshnessReasonCode.Redacted, null, "not available");
        StubGetConversation(ConversationClientResult<ConversationDetailResult>.Success(redacted, HttpStatusCode.OK));

        ConversationContextReadResult result = await Read();

        result.Outcome.ShouldBe(AgentInteractionContextLoadOutcome.Unauthorized);
    }

    [Fact]
    public async Task Unavailable_maps_to_unavailable()
    {
        StubGetConversation(ConversationClientResult<ConversationDetailResult>.Success(
            ConversationDetailResult.Unavailable(SchemaVersion.Current), HttpStatusCode.OK));

        ConversationContextReadResult result = await Read();

        result.Outcome.ShouldBe(AgentInteractionContextLoadOutcome.Unavailable);
        result.Messages.ShouldBeNull();
    }

    [Fact]
    public async Task Rebuilding_with_no_details_maps_to_unavailable()
    {
        // A Rebuilding projection (no details, not an access denial) is a degraded read, not an authorization failure →
        // Unavailable (the doc-stated Unavailable/Rebuilding → Unavailable branch; AD-12).
        var rebuilding = new ConversationDetailResult(
            SchemaVersion.Current, ProjectionTrustState.Rebuilding, ProjectionFreshnessReasonCode.Rebuilding, null, "rebuilding");
        StubGetConversation(ConversationClientResult<ConversationDetailResult>.Success(rebuilding, HttpStatusCode.OK));

        ConversationContextReadResult result = await Read();

        result.Outcome.ShouldBe(AgentInteractionContextLoadOutcome.Unavailable);
        result.Messages.ShouldBeNull();
    }

    [Fact]
    public async Task Visible_and_fresh_with_no_messages_maps_to_loaded_with_an_empty_message_list()
    {
        // An authorized, fresh, but empty Conversation is a valid load (count 0) — the reader returns an empty, non-null
        // list so the orchestrator measures zero tokens rather than tripping the null-payload degraded-read guard.
        ConversationDetailsV1 details = Details(FreshFreshness());
        StubGetConversation(ConversationClientResult<ConversationDetailResult>.Success(
            ConversationDetailResult.Visible(SchemaVersion.Current, details, "ok"), HttpStatusCode.OK));

        ConversationContextReadResult result = await Read();

        result.Outcome.ShouldBe(AgentInteractionContextLoadOutcome.Loaded);
        result.MessageCount.ShouldBe(0);
        result.Messages.ShouldNotBeNull().ShouldBeEmpty();
        result.IsFresh.ShouldBeTrue();
    }

    [Fact]
    public async Task Visible_but_not_trust_bearing_freshness_maps_to_stale()
    {
        ConversationDetailsV1 details = Details(StaleFreshness(), Message("hi", _eventTimestamp.AddMinutes(1)));
        StubGetConversation(ConversationClientResult<ConversationDetailResult>.Success(
            ConversationDetailResult.Visible(SchemaVersion.Current, details, "stale"), HttpStatusCode.OK));

        ConversationContextReadResult result = await Read();

        result.Outcome.ShouldBe(AgentInteractionContextLoadOutcome.Stale);
        result.Messages.ShouldBeNull();
        result.IsFresh.ShouldBeFalse();
    }

    [Fact]
    public async Task A_failure_client_result_maps_to_unavailable()
    {
        var error = new ConversationError(
            SchemaVersion.Current,
            ConversationErrorCode.TenantProjectionStale,
            ConversationErrorCategory.Freshness,
            IsRetryable: true,
            CorrelationId: "trace-1");
        StubGetConversation(ConversationClientResult<ConversationDetailResult>.Failure(
            new ConversationErrorResult([error]), HttpStatusCode.ServiceUnavailable));

        ConversationContextReadResult result = await Read();

        result.Outcome.ShouldBe(AgentInteractionContextLoadOutcome.Unavailable);
        result.Messages.ShouldBeNull();
    }

    [Fact]
    public async Task A_thrown_client_exception_fails_closed_to_unavailable_without_raw_error()
    {
        _client.GetConversationAsync(Arg.Any<GetConversationQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("conversations transport blew up: secret detail"));

        ConversationContextReadResult result = await Read();

        result.Outcome.ShouldBe(AgentInteractionContextLoadOutcome.Unavailable);
        result.Messages.ShouldBeNull();
        result.MessageCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_genuine_cancellation_propagates()
    {
        _client.GetConversationAsync(Arg.Any<GetConversationQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(async () => await Read());
    }

    // ===== Helpers =====

    private Task<ConversationContextReadResult> Read()
        => Reader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, CallerPrincipalId, CancellationToken.None);

    private void StubGetConversation(ConversationClientResult<ConversationDetailResult> result)
        => _client.GetConversationAsync(Arg.Any<GetConversationQuery>(), Arg.Any<CancellationToken>()).Returns(result);

    private static ConversationDetailsV1 Details(ProjectionFreshnessV1 freshness, params ConversationTimelineMessageProjectionV1[] messages)
        => new(
            SchemaVersion.Current,
            new TenantId(TenantId),
            new ConversationId(SourceConversationId),
            freshness,
            LifecycleState: "Active",
            Messages: messages);

    private static ConversationTimelineMessageProjectionV1 Message(string text, DateTimeOffset createdAt)
        => new(new MessageId("m-" + text), new PartyId("party-author"), text, createdAt);

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
            TimeSpan.FromMinutes(10),
            IsStale: true,
            ProjectionTrustState.Stale,
            ProjectionFreshnessReasonCode.StaleThresholdExceeded);
}
