using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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

/// <summary>
/// End-to-end Confirmation-mode lifecycle tests for Story 3.5 approval/posting (AC1–AC4; FR-17, FR-14). Unlike the focused
/// <see cref="AgentInteractionProposalApprovalAggregateTests"/> — which drives the approve command over a pre-built
/// <c>ProposalCreated</c> state — these walk the WHOLE command chain
/// (request → gate → context → generation → proposal create → approve) through the real reflection-dispatch pipeline
/// (<c>AgentInteractionAggregate.ProcessAsync</c> + JSON round-trip + the production <c>Apply</c> handlers), so every
/// precondition is exercised by the actual handler. This proves the ONLY way to reach an approval is a full happy
/// Confirmation path through a created proposal, that exactly the selected version is posted while prior versions stay
/// preserved, and that re-approving the same posted version is an idempotent no-op (no duplicate Conversation Message).
/// </summary>
public sealed class AgentInteractionProposalApprovalLifecycleE2ETests
{
    private const string ApproverPartyId = "approver-party-001";
    private const string ApprovalMessageId = "approval-message-001";

    private static RequestAgentInteraction ConfirmationRequest { get; } = new(
        AgentId,
        SourceConversationId,
        CallerPartyId,
        Prompt,
        IdempotencyKey,
        ConfirmationSnapshot,
        ClientCorrelationId);

    [Fact]
    public async Task A_confirmation_interaction_reaches_a_posted_proposal_through_the_full_command_chain()
    {
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        await ProcessAndApplyAsync(aggregate, state, ConfirmationRequest);
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));
        await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));
        await ProcessAndApplyAsync(aggregate, state, ProposalCommand(CreatedProposalResult()));
        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending);

        // Approve the exact generated version → Approved → PostingPending → Posted (AC1, AC2).
        DomainResult posted = await ProcessAndApplyAsync(aggregate, state, ApproveCommand(AgentProposalApprovalOutcome.Posted));

        posted.IsSuccess.ShouldBeTrue();
        posted.Events.Select(e => e.GetType()).ShouldBe([
            typeof(ProposedAgentReplyApproved),
            typeof(ProposedAgentReplyPostingPending),
            typeof(ProposedAgentReplyPosted),
        ]);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalPosted);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Posted);
        state.ApprovedVersionId.ShouldBe(PostedVersionId); // exactly the selected version is the unit of approval (AC1)
        state.ApprovalPostingMessageId.ShouldBe(ApprovalMessageId);
        state.GeneratedVersions.ShouldNotBeNull().Count.ShouldBe(1); // the preserved generated version is untouched (FR-14)

        // AD-14: the safe approval evidence that survived the full reflection-dispatch + JSON round-trip carries no content.
        JsonSerializer.Serialize(state.ProposalApprovalEvidence).ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public async Task A_proposal_cannot_be_approved_before_it_exists_end_to_end()
    {
        // AC4 end-to-end: before a proposal is created, an approve command can only be structurally rejected — there is no
        // pending proposal to approve, so it fails closed before any Conversation side effect and never posts.
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        await ProcessAndApplyAsync(aggregate, state, ConfirmationRequest);
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));
        await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));
        state.Status.ShouldBe(AgentInteractionStatus.Generated); // generated, but no proposal created yet

        DomainResult approved = await ProcessAndApplyAsync(aggregate, state, ApproveCommand(AgentProposalApprovalOutcome.Posted));

        approved.IsRejection.ShouldBeTrue();
        approved.Events[0].ShouldBeOfType<ProposedAgentReplyNotApprovableRejection>()
            .Reason.ShouldBe(AgentProposedReplyNotApprovableReason.ProposalNotPending);
        approved.Events.OfType<ProposedAgentReplyApproved>().ShouldBeEmpty();
        approved.Events.OfType<ProposedAgentReplyPosted>().ShouldBeEmpty();
        state.Status.ShouldBe(AgentInteractionStatus.Generated); // unchanged — failed closed before Conversation side effects (AC4)
        state.ApprovedVersionId.ShouldBeEmpty();
    }

    [Fact]
    public async Task Re_approving_the_same_posted_version_is_an_idempotent_no_op_end_to_end()
    {
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        await ProcessAndApplyAsync(aggregate, state, ConfirmationRequest);
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));
        await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));
        await ProcessAndApplyAsync(aggregate, state, ProposalCommand(CreatedProposalResult()));
        await ProcessAndApplyAsync(aggregate, state, ApproveCommand(AgentProposalApprovalOutcome.Posted));
        state.Status.ShouldBe(AgentInteractionStatus.ProposalPosted);

        // AC3: the same deterministic message id + idempotency key re-issued is a clean no-op — no duplicate Conversation Message.
        DomainResult retry = await ProcessAndApplyAsync(aggregate, state, ApproveCommand(AgentProposalApprovalOutcome.Posted));

        retry.IsNoOp.ShouldBeTrue();
        retry.Events.ShouldBeEmpty();
        state.Status.ShouldBe(AgentInteractionStatus.ProposalPosted);
        state.ApprovalPostingMessageId.ShouldBe(ApprovalMessageId);
    }

    private static ApproveProposedAgentReply ApproveCommand(AgentProposalApprovalOutcome outcome)
        => new(
            InteractionId,
            new AgentProposalApprovalResult(
                outcome,
                SampleProposalId,
                SourceConversationId,
                PostedVersionId,
                ApproverPartyId,
                ConfirmationSnapshot.ApproverPolicyVersion,
                ApproverPolicyValidationStatus.Valid,
                ApproverPolicyBasisDisclosure.UserVisible,
                AgentPartyId,
                ApprovalMessageId,
                IdempotencyKey,
                outcome == AgentProposalApprovalOutcome.Posted ? ApprovalMessageId : string.Empty));
}
