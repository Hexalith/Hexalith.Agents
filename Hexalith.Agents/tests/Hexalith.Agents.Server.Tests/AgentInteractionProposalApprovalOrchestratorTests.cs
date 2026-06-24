using System;
using System.Text;
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

/// <summary>Server orchestration tests for Story 3.5 proposal approval/posting.</summary>
public sealed class AgentInteractionProposalApprovalOrchestratorTests
{
    private const string TenantId = "acme";
    private const string InteractionId = "interaction-001";
    private const string ProposalId = "proposal-001";
    private const string AgentId = "hexa";
    private const string ConversationId = "conversation-001";
    private const string VersionId = "version-001";
    private const string Content = "approved content must not enter the command payload";
    private const string AgentPartyId = "agent-party-001";

    private readonly IAgentPartyReader _partyReader = Substitute.For<IAgentPartyReader>();
    private readonly IAgentGeneratedVersionReader _versionReader = Substitute.For<IAgentGeneratedVersionReader>();
    private readonly IConversationResponsePoster _poster = Substitute.For<IConversationResponsePoster>();
    private readonly IApproverPolicyResolver _approverPolicyResolver = Substitute.For<IApproverPolicyResolver>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private CommandEnvelope? _dispatched;
    private ConversationAppendRequest? _append;

    private AgentInteractionProposalApprovalOrchestrator Orchestrator => new(
        _partyReader,
        _versionReader,
        _poster,
        _approverPolicyResolver,
        _dispatcher);

    [Fact]
    public async Task Happy_path_reads_the_exact_selected_version_posts_once_and_dispatches_safe_approval_command()
    {
        StubAuthorized();
        _versionReader.ReadVersionAsync(TenantId, InteractionId, VersionId, Arg.Any<CancellationToken>())
            .Returns(new AgentGeneratedVersionReadResult(AgentGeneratedVersionReadOutcome.Available, VersionId, Content));
        _partyReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyReadResult(AgentPartyReadOutcome.Available, AgentPartyId));
        _poster.EnsureAiAgentParticipantAsync(Arg.Any<ConversationMembershipRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ConversationMembershipResult(ConversationMembershipOutcome.Present));
        _poster.AppendAgentMessageAsync(Arg.Do<ConversationAppendRequest>(r => _append = r), Arg.Any<CancellationToken>())
            .Returns(new ConversationAppendResult(ConversationAppendOutcome.Posted));
        _ = _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _dispatched = e), Arg.Any<CancellationToken>());

        AgentInteractionProposalApprovalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalPosted);
        outcome.SelectedVersionId.ShouldBe(VersionId);
        outcome.MessageId.ShouldNotBeEmpty();
        await _versionReader.Received(1).ReadVersionAsync(TenantId, InteractionId, VersionId, Arg.Any<CancellationToken>());

        _append.ShouldNotBeNull();
        _append!.AuthorPartyId.ShouldBe(AgentPartyId);
        _append.Text.ShouldBe(Content);
        _append.MessageId.ShouldBe(outcome.MessageId);

        _dispatched.ShouldNotBeNull();
        _dispatched!.CommandType.ShouldBe(nameof(ApproveProposedAgentReply));
        _dispatched.AggregateId.ShouldBe(InteractionId);
        string payload = Encoding.UTF8.GetString(_dispatched.Payload);
        payload.ShouldNotContain(Content);
        ApproveProposedAgentReply command = JsonSerializer.Deserialize<ApproveProposedAgentReply>(_dispatched.Payload)!;
        command.Result.SelectedVersionId.ShouldBe(VersionId);
        command.Result.Outcome.ShouldBe(AgentProposalApprovalOutcome.Posted);
        command.Result.AgentPartyId.ShouldBe(AgentPartyId);
    }

    [Fact]
    public async Task Unauthorized_approval_dispatches_approval_failed_without_reading_version_or_posting()
    {
        _approverPolicyResolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .Returns(new ApproverPolicyResolutionResult([
                new ApproverSourceResolution(ApproverPolicySourceKind.Caller, ApproverSourceOutcome.Unauthorized),
            ]));
        _ = _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _dispatched = e), Arg.Any<CancellationToken>());

        AgentInteractionProposalApprovalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalApprovalFailed);
        await _versionReader.DidNotReceive().ReadVersionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _poster.DidNotReceive().AppendAgentMessageAsync(Arg.Any<ConversationAppendRequest>(), Arg.Any<CancellationToken>());
        JsonSerializer.Deserialize<ApproveProposedAgentReply>(_dispatched!.Payload)!.Result.Outcome.ShouldBe(AgentProposalApprovalOutcome.NotAuthorized);
    }

    [Fact]
    public async Task Terminal_proposal_returns_not_pending_without_dispatch()
    {
        AgentInteractionProposalApprovalOutcomeResult outcome = await Orchestrator.ExecuteAsync(
            Request(ProposedAgentReplyState.Posted),
            CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Unknown);
        outcome.NotApprovableReason.ShouldBe(AgentProposedReplyNotApprovableReason.ProposalNotPending);
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Selected_version_unavailable_fails_closed_before_reading_party_or_posting()
    {
        StubAuthorized();
        _versionReader.ReadVersionAsync(TenantId, InteractionId, VersionId, Arg.Any<CancellationToken>())
            .Returns(AgentGeneratedVersionReadResult.NotAvailable);
        CaptureDispatch();

        AgentInteractionProposalApprovalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalApprovalFailed);
        await _partyReader.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _poster.DidNotReceive().AppendAgentMessageAsync(Arg.Any<ConversationAppendRequest>(), Arg.Any<CancellationToken>());
        JsonSerializer.Deserialize<ApproveProposedAgentReply>(_dispatched!.Payload)!.Result.Outcome
            .ShouldBe(AgentProposalApprovalOutcome.SelectedVersionMissing);
    }

    [Fact]
    public async Task Missing_agent_party_identity_fails_closed_before_posting()
    {
        StubAuthorized();
        StubVersionAvailable();
        _partyReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(AgentPartyReadResult.Unavailable);
        CaptureDispatch();

        AgentInteractionProposalApprovalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalPostingFailed);
        await _poster.DidNotReceive().AppendAgentMessageAsync(Arg.Any<ConversationAppendRequest>(), Arg.Any<CancellationToken>());
        JsonSerializer.Deserialize<ApproveProposedAgentReply>(_dispatched!.Payload)!.Result.Outcome
            .ShouldBe(AgentProposalApprovalOutcome.PartyIdentityUnavailable);
    }

    [Fact]
    public async Task Unavailable_conversation_membership_fails_closed_before_posting()
    {
        StubAuthorized();
        StubVersionAvailable();
        StubPartyAvailable();
        _poster.EnsureAiAgentParticipantAsync(Arg.Any<ConversationMembershipRequest>(), Arg.Any<CancellationToken>())
            .Returns(ConversationMembershipResult.SeamUnavailable);
        CaptureDispatch();

        AgentInteractionProposalApprovalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalPostingFailed);
        await _poster.DidNotReceive().AppendAgentMessageAsync(Arg.Any<ConversationAppendRequest>(), Arg.Any<CancellationToken>());
        JsonSerializer.Deserialize<ApproveProposedAgentReply>(_dispatched!.Payload)!.Result.Outcome
            .ShouldBe(AgentProposalApprovalOutcome.MembershipUnavailable);
    }

    [Fact]
    public async Task Conversation_rejecting_the_append_maps_to_posting_failed()
    {
        StubAuthorized();
        StubVersionAvailable();
        StubPartyAvailable();
        StubMembershipPresent();
        StubAppend(ConversationAppendOutcome.PostRejected);
        CaptureDispatch();

        AgentInteractionProposalApprovalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalPostingFailed);
        await _poster.Received(1).AppendAgentMessageAsync(Arg.Any<ConversationAppendRequest>(), Arg.Any<CancellationToken>());
        ApproveProposedAgentReply dispatched = JsonSerializer.Deserialize<ApproveProposedAgentReply>(_dispatched!.Payload)!;
        dispatched.Result.Outcome.ShouldBe(AgentProposalApprovalOutcome.PostRejected);
        dispatched.Result.PostedConversationMessageId.ShouldBeEmpty();
    }

    [Fact]
    public async Task Posting_adapter_failure_maps_to_posting_failed()
    {
        StubAuthorized();
        StubVersionAvailable();
        StubPartyAvailable();
        StubMembershipPresent();
        StubAppend(ConversationAppendOutcome.AdapterFailure);
        CaptureDispatch();

        AgentInteractionProposalApprovalOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalPostingFailed);
        JsonSerializer.Deserialize<ApproveProposedAgentReply>(_dispatched!.Payload)!.Result.Outcome
            .ShouldBe(AgentProposalApprovalOutcome.AdapterFailure);
    }

    [Fact]
    public async Task A_genuine_cancellation_propagates_and_no_command_is_dispatched()
    {
        StubAuthorized();
        _versionReader.ReadVersionAsync(TenantId, InteractionId, VersionId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Orchestrator.ExecuteAsync(Request(), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Posting_identity_is_deterministic_so_a_retry_targets_the_same_message()
    {
        StubAuthorized();
        StubVersionAvailable();
        StubPartyAvailable();
        StubMembershipPresent();
        StubAppend(ConversationAppendOutcome.Posted);
        CaptureDispatch();

        AgentInteractionProposalApprovalOutcomeResult first = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);
        AgentInteractionProposalApprovalOutcomeResult second = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        first.Status.ShouldBe(AgentInteractionStatus.ProposalPosted);
        second.Status.ShouldBe(AgentInteractionStatus.ProposalPosted);
        first.MessageId.ShouldNotBeEmpty();
        second.MessageId.ShouldBe(first.MessageId);
    }

    private static AgentInteractionProposalApprovalRequest Request(ProposedAgentReplyState state = ProposedAgentReplyState.Pending)
        => new(
            "msg-1",
            "corr-1",
            TenantId,
            InteractionId,
            ProposalId,
            state,
            AgentId,
            ConversationId,
            VersionId,
            "approver-party-001",
            Policy(),
            1,
            "actor-user");

    private static AgentApproverPolicy Policy()
        => new(
            [new ApproverPolicySource(ApproverPolicySourceKind.Caller, PartyId: null, TenantRole: null)],
            ApproverPolicyBasisDisclosure.UserVisible);

    private void StubAuthorized()
        => _approverPolicyResolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .Returns(new ApproverPolicyResolutionResult([
                new ApproverSourceResolution(ApproverPolicySourceKind.Caller, ApproverSourceOutcome.Resolved),
            ]));

    private void StubVersionAvailable()
        => _versionReader.ReadVersionAsync(TenantId, InteractionId, VersionId, Arg.Any<CancellationToken>())
            .Returns(new AgentGeneratedVersionReadResult(AgentGeneratedVersionReadOutcome.Available, VersionId, Content));

    private void StubPartyAvailable()
        => _partyReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyReadResult(AgentPartyReadOutcome.Available, AgentPartyId));

    private void StubMembershipPresent()
        => _poster.EnsureAiAgentParticipantAsync(Arg.Any<ConversationMembershipRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ConversationMembershipResult(ConversationMembershipOutcome.Present));

    private void StubAppend(ConversationAppendOutcome outcome)
        => _poster.AppendAgentMessageAsync(Arg.Do<ConversationAppendRequest>(r => _append = r), Arg.Any<CancellationToken>())
            .Returns(new ConversationAppendResult(outcome));

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _dispatched = e), Arg.Any<CancellationToken>());
}
