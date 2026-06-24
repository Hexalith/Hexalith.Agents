using System.Linq;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentInteractionTestData;

namespace Hexalith.Agents.Tests;

/// <summary>Aggregate tests for Story 3.6 rejected/abandoned/expired terminal proposal states.</summary>
public sealed class AgentInteractionProposalTerminalAggregateTests
{
    [Fact]
    public void Authorized_rejection_records_terminal_state_rationale_and_preserves_versions()
    {
        AgentInteractionState state = StateProposalCreated();
        int priorCount = state.GeneratedVersions!.Count;

        DomainResult result = Reject(RejectionResult(), state);

        result.IsSuccess.ShouldBeTrue();
        ProposedAgentReplyRejected rejected = result.Events.Single().ShouldBeOfType<ProposedAgentReplyRejected>();
        rejected.Evidence.RationaleCode.ShouldBe(SampleRationaleCode);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalRejected);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Rejected);
        state.ProposalRejectionEvidence.ShouldNotBeNull().RationaleCode.ShouldBe(SampleRationaleCode);
        state.GeneratedVersions!.Count.ShouldBe(priorCount);
        state.GeneratedVersions[0].VersionId.ShouldBe(PostedVersionId);
    }

    [Fact]
    public void Authorized_abandonment_records_terminal_state_and_preserves_versions()
    {
        AgentInteractionState state = StateProposalCreated();
        int priorCount = state.GeneratedVersions!.Count;

        DomainResult result = Abandon(AbandonmentResult(), state);

        result.Events.Single().ShouldBeOfType<ProposedAgentReplyAbandoned>();
        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalAbandoned);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Abandoned);
        state.ProposalAbandonmentEvidence.ShouldNotBeNull().ActorPartyId.ShouldBe(ActorPartyId);
        state.GeneratedVersions!.Count.ShouldBe(priorCount);
    }

    [Fact]
    public void Reached_expiry_records_terminal_state_deterministically_and_preserves_versions()
    {
        AgentInteractionState state = StateProposalCreated();
        int priorCount = state.GeneratedVersions!.Count;

        DomainResult result = Expire(ExpiryResult(), state);

        result.Events.Single().ShouldBeOfType<ProposedAgentReplyExpired>()
            .Evidence.ExpiresAt.ShouldBe(SampleExpiresAt);
        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalExpired);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Expired);
        state.ProposalExpiryEvidence.ShouldNotBeNull().ExpiresAt.ShouldBe(SampleExpiresAt);
        state.GeneratedVersions!.Count.ShouldBe(priorCount);
    }

    [Fact]
    public void Expiry_not_reached_is_a_noop_and_does_not_transition()
    {
        AgentInteractionState state = StateProposalCreated();

        DomainResult result = Expire(ExpiryResult(AgentProposalExpiryOutcome.ExpiryNotReached), state);

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending);
    }

    [Fact]
    public void Unauthorized_reject_and_abandon_record_fail_closed_events_without_terminal_success()
    {
        DomainResult rejected = Reject(
            RejectionResult(AgentProposalRejectionOutcome.NotAuthorized, ApproverPolicyValidationStatus.Unauthorized),
            StateProposalCreated());
        DomainResult abandoned = Abandon(
            AbandonmentResult(AgentProposalAbandonmentOutcome.NotAuthorized, ApproverPolicyValidationStatus.Unauthorized),
            StateProposalCreated());

        rejected.Events.Single().ShouldBeOfType<ProposedAgentReplyRejectionFailed>()
            .Reason.ShouldBe(AgentProposedReplyNotRejectableReason.NotAuthorized);
        abandoned.Events.Single().ShouldBeOfType<ProposedAgentReplyAbandonmentFailed>()
            .Reason.ShouldBe(AgentProposedReplyNotAbandonableReason.NotAuthorized);
        rejected.Events.OfType<ProposedAgentReplyRejected>().ShouldBeEmpty();
        abandoned.Events.OfType<ProposedAgentReplyAbandoned>().ShouldBeEmpty();
    }

    [Fact]
    public void Reissuing_same_terminal_command_is_a_clean_noop()
    {
        AgentInteractionState rejected = StateProposalCreated();
        ApplyAll(rejected, Reject(RejectionResult(), rejected));
        AgentInteractionState abandoned = StateProposalCreated();
        ApplyAll(abandoned, Abandon(AbandonmentResult(), abandoned));
        AgentInteractionState expired = StateProposalCreated();
        ApplyAll(expired, Expire(ExpiryResult(), expired));

        Reject(RejectionResult(), rejected).IsNoOp.ShouldBeTrue();
        Abandon(AbandonmentResult(), abandoned).IsNoOp.ShouldBeTrue();
        Expire(ExpiryResult(), expired).IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public void Non_requested_interaction_rejects_terminal_actions_as_not_proposed()
    {
        Reject(RejectionResult(), state: null).Events.Single()
            .ShouldBeOfType<ProposedAgentReplyNotRejectableRejection>()
            .Reason.ShouldBe(AgentProposedReplyNotRejectableReason.InteractionNotProposed);
        Abandon(AbandonmentResult(), state: null).Events.Single()
            .ShouldBeOfType<ProposedAgentReplyNotAbandonableRejection>()
            .Reason.ShouldBe(AgentProposedReplyNotAbandonableReason.InteractionNotProposed);
        Expire(ExpiryResult(), state: null).Events.Single()
            .ShouldBeOfType<ProposedAgentReplyNotExpirableRejection>()
            .Reason.ShouldBe(AgentProposedReplyNotExpirableReason.InteractionNotProposed);
    }

    [Theory]
    [InlineData(ProposedAgentReplyState.Rejected)]
    [InlineData(ProposedAgentReplyState.Abandoned)]
    [InlineData(ProposedAgentReplyState.Expired)]
    public void Terminal_proposals_reject_approve_edit_and_regenerate_before_side_effects(ProposedAgentReplyState terminalState)
    {
        AgentInteractionState state = StateInTerminalState(terminalState);
        int priorCount = state.GeneratedVersions!.Count;

        Approve(ApprovalResult(AgentProposalApprovalOutcome.Posted), state).Events.Single()
            .ShouldBeOfType<ProposedAgentReplyNotApprovableRejection>()
            .Reason.ShouldBe(AgentProposedReplyNotApprovableReason.ProposalNotPending);
        Edit(EditResult(), state).Events.Single()
            .ShouldBeOfType<ProposedAgentReplyNotEditableRejection>()
            .Reason.ShouldBe(AgentProposedReplyNotEditableReason.ProposalNotPending);
        Regenerate(RegenerationResult(), state).Events.Single()
            .ShouldBeOfType<ProposedAgentReplyNotRegeneratableRejection>()
            .Reason.ShouldBe(AgentProposedReplyNotRegeneratableReason.ProposalNotPending);
        state.GeneratedVersions!.Count.ShouldBe(priorCount);
    }

    [Theory]
    [InlineData(ProposedAgentReplyState.Rejected)]
    [InlineData(ProposedAgentReplyState.Abandoned)]
    [InlineData(ProposedAgentReplyState.Expired)]
    public void Terminal_proposals_reject_other_terminal_actions_as_not_pending_without_a_noop(ProposedAgentReplyState terminalState)
    {
        // AC2/AC4 cross-terminal guard: a proposal that already reached one terminal state "can never act again". The
        // matching command is the idempotent NoOp (proven elsewhere), but a DIFFERENT terminal command must be a structural
        // ProposalNotPending rejection — crucially NOT a silent no-op (which would mask an illegal cross-terminal transition).
        AgentInteractionState state = StateInTerminalState(terminalState);

        if (terminalState is not ProposedAgentReplyState.Rejected)
        {
            DomainResult reject = Reject(RejectionResult(), state);
            reject.IsNoOp.ShouldBeFalse();
            reject.Events.Single().ShouldBeOfType<ProposedAgentReplyNotRejectableRejection>()
                .Reason.ShouldBe(AgentProposedReplyNotRejectableReason.ProposalNotPending);
        }

        if (terminalState is not ProposedAgentReplyState.Abandoned)
        {
            DomainResult abandon = Abandon(AbandonmentResult(), state);
            abandon.IsNoOp.ShouldBeFalse();
            abandon.Events.Single().ShouldBeOfType<ProposedAgentReplyNotAbandonableRejection>()
                .Reason.ShouldBe(AgentProposedReplyNotAbandonableReason.ProposalNotPending);
        }

        if (terminalState is not ProposedAgentReplyState.Expired)
        {
            DomainResult expire = Expire(ExpiryResult(), state);
            expire.IsNoOp.ShouldBeFalse();
            expire.Events.Single().ShouldBeOfType<ProposedAgentReplyNotExpirableRejection>()
                .Reason.ShouldBe(AgentProposedReplyNotExpirableReason.ProposalNotPending);
        }

        state.ProposalState.ShouldBe(terminalState);
        state.GeneratedVersions!.Count.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(AgentProposalRejectionOutcome.Rejected, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalRejected)]
    [InlineData(AgentProposalRejectionOutcome.Rejected, ApproverPolicyValidationStatus.Unauthorized, AgentInteractionStatus.ProposalRejectionFailed)]
    [InlineData(AgentProposalRejectionOutcome.PolicyFailure, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalRejectionFailed)]
    public void Rejection_decide_matches_recorded_decision(
        AgentProposalRejectionOutcome outcome,
        ApproverPolicyValidationStatus verdict,
        AgentInteractionStatus expected)
    {
        AgentProposalRejectionResult result = RejectionResult(outcome, verdict);

        AgentProposalRejectionPolicy.Decide(result).ShouldBe(expected);
        TerminalStatus(Reject(result, StateProposalCreated())).ShouldBe(expected);
    }

    [Theory]
    [InlineData(AgentProposalAbandonmentOutcome.Abandoned, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalAbandoned)]
    [InlineData(AgentProposalAbandonmentOutcome.Abandoned, ApproverPolicyValidationStatus.Unauthorized, AgentInteractionStatus.ProposalAbandonmentFailed)]
    [InlineData(AgentProposalAbandonmentOutcome.PolicyFailure, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalAbandonmentFailed)]
    public void Abandonment_decide_matches_recorded_decision(
        AgentProposalAbandonmentOutcome outcome,
        ApproverPolicyValidationStatus verdict,
        AgentInteractionStatus expected)
    {
        AgentProposalAbandonmentResult result = AbandonmentResult(outcome, verdict);

        AgentProposalAbandonmentPolicy.Decide(result).ShouldBe(expected);
        TerminalStatus(Abandon(result, StateProposalCreated())).ShouldBe(expected);
    }

    [Theory]
    [InlineData(AgentProposalExpiryOutcome.Expired, AgentInteractionStatus.ProposalExpired)]
    [InlineData(AgentProposalExpiryOutcome.NoExpiryPolicy, AgentInteractionStatus.Unknown)]
    [InlineData(AgentProposalExpiryOutcome.ExpiryNotReached, AgentInteractionStatus.Unknown)]
    public void Expiry_decide_matches_recorded_decision(AgentProposalExpiryOutcome outcome, AgentInteractionStatus expected)
    {
        AgentProposalExpiryResult result = ExpiryResult(outcome);

        AgentProposalExpiryPolicy.Decide(result).ShouldBe(expected);
        TerminalStatus(Expire(result, StateProposalCreated())).ShouldBe(expected);
    }

    private static AgentInteractionState StateInTerminalState(ProposedAgentReplyState terminalState)
    {
        AgentInteractionState state = StateProposalCreated();
        DomainResult result = terminalState switch
        {
            ProposedAgentReplyState.Rejected => Reject(RejectionResult(), state),
            ProposedAgentReplyState.Abandoned => Abandon(AbandonmentResult(), state),
            ProposedAgentReplyState.Expired => Expire(ExpiryResult(), state),
            _ => DomainResult.NoOp(),
        };
        ApplyAll(state, result);
        return state;
    }

    private static DomainResult Reject(AgentProposalRejectionResult result, AgentInteractionState? state)
    {
        RejectProposedAgentReply command = RejectCommand(result);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static DomainResult Abandon(AgentProposalAbandonmentResult result, AgentInteractionState? state)
    {
        AbandonProposedAgentReply command = AbandonCommand(result);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static DomainResult Expire(AgentProposalExpiryResult result, AgentInteractionState? state)
    {
        ExpireProposedAgentReply command = ExpireCommand(result);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static DomainResult Approve(AgentProposalApprovalResult result, AgentInteractionState? state)
    {
        var command = new ApproveProposedAgentReply(InteractionId, result);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static DomainResult Edit(AgentProposalEditResult result, AgentInteractionState? state)
    {
        EditProposedAgentReply command = EditCommand(result);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static DomainResult Regenerate(AgentProposalRegenerationResult result, AgentInteractionState? state)
    {
        RegenerateProposedAgentReply command = RegenerateCommand(result);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static AgentProposalApprovalResult ApprovalResult(AgentProposalApprovalOutcome outcome)
        => new(
            outcome,
            SampleProposalId,
            SourceConversationId,
            PostedVersionId,
            ActorPartyId,
            ConfirmationSnapshot.ApproverPolicyVersion,
            ApproverPolicyValidationStatus.Valid,
            ApproverPolicyBasisDisclosure.UserVisible,
            AgentPartyId,
            PostedMessageId,
            IdempotencyKey,
            PostedMessageId);

    private static AgentInteractionStatus TerminalStatus(DomainResult result)
    {
        if (result.Events.OfType<ProposedAgentReplyRejected>().Any())
        {
            return AgentInteractionStatus.ProposalRejected;
        }

        if (result.Events.OfType<ProposedAgentReplyAbandoned>().Any())
        {
            return AgentInteractionStatus.ProposalAbandoned;
        }

        if (result.Events.OfType<ProposedAgentReplyExpired>().Any())
        {
            return AgentInteractionStatus.ProposalExpired;
        }

        if (result.Events.OfType<ProposedAgentReplyRejectionFailed>().Any())
        {
            return AgentInteractionStatus.ProposalRejectionFailed;
        }

        return result.Events.OfType<ProposedAgentReplyAbandonmentFailed>().Any()
            ? AgentInteractionStatus.ProposalAbandonmentFailed
            : AgentInteractionStatus.Unknown;
    }
}
