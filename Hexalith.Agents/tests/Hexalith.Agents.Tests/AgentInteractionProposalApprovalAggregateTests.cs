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

/// <summary>Aggregate tests for Story 3.5 approval/posting.</summary>
public sealed class AgentInteractionProposalApprovalAggregateTests
{
    [Fact]
    public void Posted_approval_records_approval_pending_and_posted_for_the_selected_version()
    {
        AgentInteractionState state = StateProposalCreated();

        DomainResult result = Approve(ApprovalResult(AgentProposalApprovalOutcome.Posted), state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Select(e => e.GetType()).ShouldBe([
            typeof(ProposedAgentReplyApproved),
            typeof(ProposedAgentReplyPostingPending),
            typeof(ProposedAgentReplyPosted),
        ]);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalPosted);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Posted);
        state.ApprovedVersionId.ShouldBe(PostedVersionId);
        state.ApproverPartyId.ShouldBe("approver-party-001");
        state.ApprovalPostingMessageId.ShouldBe("message-001");
        state.ProposalApprovalEvidence.ShouldNotBeNull().ApprovedVersionId.ShouldBe(PostedVersionId);
    }

    [Fact]
    public void Posting_failure_records_approval_then_posting_failed_and_keeps_the_version_frozen()
    {
        AgentInteractionState state = StateProposalCreated();

        DomainResult result = Approve(ApprovalResult(AgentProposalApprovalOutcome.MembershipUnavailable), state);

        result.Events.Select(e => e.GetType()).ShouldBe([
            typeof(ProposedAgentReplyApproved),
            typeof(ProposedAgentReplyPostingPending),
            typeof(ProposedAgentReplyPostingFailed),
        ]);
        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalPostingFailed);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.PostingFailed);
        state.ApprovedVersionId.ShouldBe(PostedVersionId);
        state.ProposalPostingFailureReason.ShouldBe(AgentProposalApprovalFailureReason.MembershipUnavailable);
    }

    [Fact]
    public void Missing_selected_version_records_approval_failed_before_posting_events()
    {
        AgentInteractionState state = StateProposalCreated();

        DomainResult result = Approve(ApprovalResult(AgentProposalApprovalOutcome.Posted, selectedVersionId: "missing-version"), state);

        result.Events.Single().ShouldBeOfType<ProposedAgentReplyApprovalFailed>()
            .Reason.ShouldBe(AgentProposalApprovalFailureReason.SelectedVersionInvalid);
        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalApprovalFailed);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending);
        state.ApprovedVersionId.ShouldBeEmpty();
    }

    [Fact]
    public void Different_selected_version_after_approval_is_rejected()
    {
        AgentInteractionState state = StateProposalCreated();
        ApplyAll(state, Approve(ApprovalResult(AgentProposalApprovalOutcome.MembershipUnavailable), state));

        DomainResult result = Approve(ApprovalResult(AgentProposalApprovalOutcome.Posted, selectedVersionId: "another-version"), state);

        result.IsRejection.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ProposedAgentReplyNotApprovableRejection>()
            .Reason.ShouldBe(AgentProposedReplyNotApprovableReason.DifferentVersionAlreadyApproved);
    }

    [Fact]
    public void Same_posted_approval_retry_is_noop()
    {
        AgentInteractionState state = StateProposalCreated();
        AgentProposalApprovalResult result = ApprovalResult(AgentProposalApprovalOutcome.Posted);
        ApplyAll(state, Approve(result, state));

        DomainResult retry = Approve(result, state);

        retry.IsNoOp.ShouldBeTrue();
        retry.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Approved_outcome_records_approval_and_posting_pending_without_posting()
    {
        AgentInteractionState state = StateProposalCreated();

        DomainResult result = Approve(ApprovalResult(AgentProposalApprovalOutcome.Approved), state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Select(e => e.GetType()).ShouldBe([
            typeof(ProposedAgentReplyApproved),
            typeof(ProposedAgentReplyPostingPending),
        ]);
        result.Events.OfType<ProposedAgentReplyPosted>().ShouldBeEmpty(); // "Approved is not posted" guardrail

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalPostingPending);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.PostingPending);
        state.ApprovedVersionId.ShouldBe(PostedVersionId);
        state.ApprovalPostingMessageId.ShouldBe("message-001");
    }

    [Fact]
    public void Empty_selected_version_records_approval_failed_with_selected_version_missing()
    {
        AgentInteractionState state = StateProposalCreated();

        DomainResult result = Approve(ApprovalResult(AgentProposalApprovalOutcome.Posted, selectedVersionId: string.Empty), state);

        result.Events.Single().ShouldBeOfType<ProposedAgentReplyApprovalFailed>()
            .Reason.ShouldBe(AgentProposalApprovalFailureReason.SelectedVersionMissing);
        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalApprovalFailed);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending);
        state.ApprovedVersionId.ShouldBeEmpty();
    }

    [Fact]
    public void Approving_an_interaction_with_no_recorded_stream_is_rejected_as_not_proposed()
    {
        DomainResult result = Approve(ApprovalResult(AgentProposalApprovalOutcome.Posted), state: null);

        result.IsRejection.ShouldBeTrue();
        result.Events.Single().ShouldBeOfType<ProposedAgentReplyNotApprovableRejection>()
            .Reason.ShouldBe(AgentProposedReplyNotApprovableReason.InteractionNotProposed);
    }

    [Fact]
    public void Approval_preserves_every_prior_generated_version()
    {
        AgentInteractionState state = StateProposalCreated();
        int priorCount = state.GeneratedVersions!.Count;

        ApplyAll(state, Approve(ApprovalResult(AgentProposalApprovalOutcome.Posted), state));

        state.GeneratedVersions!.Count.ShouldBe(priorCount);
        state.GeneratedVersions[0].VersionId.ShouldBe(PostedVersionId);
    }

    [Theory]
    [InlineData(AgentProposalApprovalOutcome.Posted, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalPosted)]
    [InlineData(AgentProposalApprovalOutcome.IdempotentPosted, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalPosted)]
    [InlineData(AgentProposalApprovalOutcome.Approved, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalPostingPending)]
    [InlineData(AgentProposalApprovalOutcome.IdempotentPostingPending, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalPostingPending)]
    [InlineData(AgentProposalApprovalOutcome.MembershipUnavailable, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalPostingFailed)]
    [InlineData(AgentProposalApprovalOutcome.MembershipRejected, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalPostingFailed)]
    [InlineData(AgentProposalApprovalOutcome.ConversationUnavailable, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalPostingFailed)]
    [InlineData(AgentProposalApprovalOutcome.PartyIdentityUnavailable, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalPostingFailed)]
    [InlineData(AgentProposalApprovalOutcome.PostRejected, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalPostingFailed)]
    [InlineData(AgentProposalApprovalOutcome.AdapterFailure, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalPostingFailed)]
    [InlineData(AgentProposalApprovalOutcome.Unknown, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalApprovalFailed)]
    [InlineData(AgentProposalApprovalOutcome.Posted, ApproverPolicyValidationStatus.Unauthorized, AgentInteractionStatus.ProposalApprovalFailed)]
    [InlineData(AgentProposalApprovalOutcome.Posted, ApproverPolicyValidationStatus.Unknown, AgentInteractionStatus.ProposalApprovalFailed)]
    public void Decide_matches_the_aggregate_recorded_decision_for_each_outcome(
        AgentProposalApprovalOutcome outcome,
        ApproverPolicyValidationStatus verdict,
        AgentInteractionStatus expected)
    {
        AgentProposalApprovalResult result = ApprovalResult(outcome, verdict: verdict);

        AgentProposalApprovalPolicy.Decide(result).ShouldBe(expected);

        DomainResult domainResult = Approve(result, StateProposalCreated());
        TerminalStatus(domainResult).ShouldBe(expected);
    }

    private static DomainResult Approve(AgentProposalApprovalResult result, AgentInteractionState? state)
    {
        var command = new ApproveProposedAgentReply(InteractionId, result);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static AgentProposalApprovalResult ApprovalResult(
        AgentProposalApprovalOutcome outcome,
        string selectedVersionId = PostedVersionId,
        ApproverPolicyValidationStatus verdict = ApproverPolicyValidationStatus.Valid)
        => new(
            outcome,
            SampleProposalId,
            SourceConversationId,
            selectedVersionId,
            "approver-party-001",
            ConfirmationSnapshot.ApproverPolicyVersion,
            verdict,
            ApproverPolicyBasisDisclosure.UserVisible,
            "agent-party-001",
            "message-001",
            "idem-001",
            outcome is AgentProposalApprovalOutcome.Posted or AgentProposalApprovalOutcome.IdempotentPosted ? "message-001" : string.Empty);

    private static AgentInteractionStatus TerminalStatus(DomainResult result)
    {
        if (result.Events.OfType<ProposedAgentReplyPosted>().Any())
        {
            return AgentInteractionStatus.ProposalPosted;
        }

        if (result.Events.OfType<ProposedAgentReplyPostingFailed>().Any())
        {
            return AgentInteractionStatus.ProposalPostingFailed;
        }

        if (result.Events.OfType<ProposedAgentReplyPostingPending>().Any())
        {
            return AgentInteractionStatus.ProposalPostingPending;
        }

        return result.Events.OfType<ProposedAgentReplyApprovalFailed>().Any()
            ? AgentInteractionStatus.ProposalApprovalFailed
            : AgentInteractionStatus.Unknown;
    }
}
