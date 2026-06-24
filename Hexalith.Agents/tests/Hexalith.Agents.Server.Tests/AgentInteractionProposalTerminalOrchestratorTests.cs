using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Server orchestration tests for Story 3.6 proposal reject/abandon/expire terminal actions.</summary>
public sealed class AgentInteractionProposalTerminalOrchestratorTests
{
    private const string TenantId = "acme";
    private const string InteractionId = "interaction-001";
    private const string ProposalId = "proposal-001";
    private const string AgentId = "hexa";
    private const string ConversationId = "conversation-001";
    private const string ApproverPartyId = "approver-party-001";
    private const string ExpiresAt = "2026-06-25T12:00:00Z";

    private readonly IApproverPolicyResolver _approverPolicyResolver = Substitute.For<IApproverPolicyResolver>();
    private readonly IProposalExpiryPolicyReader _expiryReader = Substitute.For<IProposalExpiryPolicyReader>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private CommandEnvelope? _dispatched;

    private AgentInteractionProposalRejectionOrchestrator RejectionOrchestrator => new(_approverPolicyResolver, _dispatcher);

    private AgentInteractionProposalAbandonmentOrchestrator AbandonmentOrchestrator => new(_approverPolicyResolver, _dispatcher);

    private AgentInteractionProposalExpiryOrchestrator ExpiryOrchestrator => new(_expiryReader, _dispatcher);

    [Fact]
    public async Task Authorized_rejection_dispatches_one_safe_reject_command()
    {
        StubAuthorized();
        CaptureDispatch();

        AgentInteractionProposalRejectionOutcomeResult outcome = await RejectionOrchestrator.ExecuteAsync(
            RejectionRequest(rationaleCode: "OffTopic"),
            CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRejected);
        RejectProposedAgentReply command = JsonSerializer.Deserialize<RejectProposedAgentReply>(_dispatched!.Payload)!;
        _dispatched.CommandType.ShouldBe(nameof(RejectProposedAgentReply));
        _dispatched.AggregateId.ShouldBe(InteractionId);
        command.Result.Outcome.ShouldBe(AgentProposalRejectionOutcome.Rejected);
        command.Result.RationaleCode.ShouldBe("OffTopic");
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthorized_rejection_dispatches_failed_command_and_never_success()
    {
        StubUnauthorized();
        CaptureDispatch();

        AgentInteractionProposalRejectionOutcomeResult outcome = await RejectionOrchestrator.ExecuteAsync(
            RejectionRequest(),
            CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalRejectionFailed);
        RejectProposedAgentReply command = JsonSerializer.Deserialize<RejectProposedAgentReply>(_dispatched!.Payload)!;
        command.Result.Outcome.ShouldBe(AgentProposalRejectionOutcome.NotAuthorized);
        command.Result.AuthorizationVerdict.ShouldBe(ApproverPolicyValidationStatus.Unauthorized);
    }

    [Fact]
    public async Task Terminal_rejection_request_returns_not_pending_without_dispatch()
    {
        AgentInteractionProposalRejectionOutcomeResult outcome = await RejectionOrchestrator.ExecuteAsync(
            RejectionRequest(state: ProposedAgentReplyState.Expired),
            CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Unknown);
        outcome.NotRejectableReason.ShouldBe(AgentProposedReplyNotRejectableReason.ProposalNotPending);
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Terminal_abandonment_request_returns_not_pending_without_dispatch()
    {
        AgentInteractionProposalAbandonmentOutcomeResult outcome = await AbandonmentOrchestrator.ExecuteAsync(
            AbandonmentRequest(state: ProposedAgentReplyState.Rejected),
            CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Unknown);
        outcome.NotAbandonableReason.ShouldBe(AgentProposedReplyNotAbandonableReason.ProposalNotPending);
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Authorized_abandonment_dispatches_one_safe_abandon_command()
    {
        StubAuthorized();
        CaptureDispatch();

        AgentInteractionProposalAbandonmentOutcomeResult outcome = await AbandonmentOrchestrator.ExecuteAsync(
            AbandonmentRequest(),
            CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalAbandoned);
        AbandonProposedAgentReply command = JsonSerializer.Deserialize<AbandonProposedAgentReply>(_dispatched!.Payload)!;
        _dispatched.CommandType.ShouldBe(nameof(AbandonProposedAgentReply));
        command.Result.Outcome.ShouldBe(AgentProposalAbandonmentOutcome.Abandoned);
        command.Result.ActorPartyId.ShouldBe(ApproverPartyId);
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unauthorized_abandonment_dispatches_failed_command_and_never_success()
    {
        StubUnauthorized();
        CaptureDispatch();

        AgentInteractionProposalAbandonmentOutcomeResult outcome = await AbandonmentOrchestrator.ExecuteAsync(
            AbandonmentRequest(),
            CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalAbandonmentFailed);
        AbandonProposedAgentReply command = JsonSerializer.Deserialize<AbandonProposedAgentReply>(_dispatched!.Payload)!;
        command.Result.Outcome.ShouldBe(AgentProposalAbandonmentOutcome.NotAuthorized);
        command.Result.AuthorizationVerdict.ShouldBe(ApproverPolicyValidationStatus.Unauthorized);
    }

    [Fact]
    public async Task Expiry_dispatches_only_when_trusted_now_reaches_recorded_expires_at()
    {
        CaptureDispatch();

        AgentInteractionProposalExpiryOutcomeResult outcome = await ExpiryOrchestrator.ExecuteAsync(
            ExpiryRequest(expiresAt: ExpiresAt, now: DateTimeOffset.Parse("2026-06-25T12:00:00Z")),
            CancellationToken.None);

        outcome.Outcome.ShouldBe(AgentProposalExpiryOutcome.Expired);
        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalExpired);
        ExpireProposedAgentReply command = JsonSerializer.Deserialize<ExpireProposedAgentReply>(_dispatched!.Payload)!;
        _dispatched.CommandType.ShouldBe(nameof(ExpireProposedAgentReply));
        command.Result.ExpiresAt.ShouldBe(ExpiresAt);
        command.Result.Outcome.ShouldBe(AgentProposalExpiryOutcome.Expired);
    }

    [Fact]
    public async Task Expiry_not_reached_and_no_policy_do_not_dispatch()
    {
        AgentInteractionProposalExpiryOutcomeResult notReached = await ExpiryOrchestrator.ExecuteAsync(
            ExpiryRequest(expiresAt: ExpiresAt, now: DateTimeOffset.Parse("2026-06-25T11:59:59Z")),
            CancellationToken.None);
        _expiryReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(ProposalExpiryPolicyResult.None);
        AgentInteractionProposalExpiryOutcomeResult noPolicy = await ExpiryOrchestrator.ExecuteAsync(
            ExpiryRequest(expiresAt: null, now: DateTimeOffset.Parse("2026-06-25T12:00:00Z")),
            CancellationToken.None);

        notReached.Outcome.ShouldBe(AgentProposalExpiryOutcome.ExpiryNotReached);
        noPolicy.Outcome.ShouldBe(AgentProposalExpiryOutcome.NoExpiryPolicy);
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolver_cancellation_propagates_without_dispatching_terminal_command()
    {
        _approverPolicyResolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await RejectionOrchestrator.ExecuteAsync(RejectionRequest(), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Expiry_reader_cancellation_propagates_without_dispatching_terminal_command()
    {
        _expiryReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await ExpiryOrchestrator.ExecuteAsync(ExpiryRequest(expiresAt: null), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    private static AgentInteractionProposalRejectionRequest RejectionRequest(
        ProposedAgentReplyState state = ProposedAgentReplyState.Pending,
        string? rationaleCode = null)
        => new(
            "msg-1",
            "corr-1",
            TenantId,
            InteractionId,
            ProposalId,
            state,
            ConversationId,
            ApproverPartyId,
            Policy(),
            1,
            rationaleCode,
            "actor-user");

    private static AgentInteractionProposalAbandonmentRequest AbandonmentRequest(ProposedAgentReplyState state = ProposedAgentReplyState.Pending)
        => new(
            "msg-1",
            "corr-1",
            TenantId,
            InteractionId,
            ProposalId,
            state,
            ConversationId,
            ApproverPartyId,
            Policy(),
            1,
            "actor-user");

    private static AgentInteractionProposalExpiryRequest ExpiryRequest(
        string? expiresAt = ExpiresAt,
        DateTimeOffset? now = null,
        ProposedAgentReplyState state = ProposedAgentReplyState.Pending)
        => new(
            "msg-1",
            "corr-1",
            TenantId,
            InteractionId,
            ProposalId,
            state,
            AgentId,
            ConversationId,
            expiresAt,
            now ?? DateTimeOffset.Parse("2026-06-25T12:00:00Z"),
            "system-user");

    private static AgentApproverPolicy Policy()
        => new(
            [new ApproverPolicySource(ApproverPolicySourceKind.Caller, PartyId: null, TenantRole: null)],
            ApproverPolicyBasisDisclosure.UserVisible);

    private void StubAuthorized()
        => _approverPolicyResolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .Returns(new ApproverPolicyResolutionResult([
                new ApproverSourceResolution(ApproverPolicySourceKind.Caller, ApproverSourceOutcome.Resolved),
            ]));

    private void StubUnauthorized()
        => _approverPolicyResolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .Returns(new ApproverPolicyResolutionResult([
                new ApproverSourceResolution(ApproverPolicySourceKind.Caller, ApproverSourceOutcome.Unauthorized),
            ]));

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _dispatched = e), Arg.Any<CancellationToken>());
}
