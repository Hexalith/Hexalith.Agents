using System;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure approval/posting decision shared by the aggregate and server orchestration.
/// </summary>
internal static class AgentProposalApprovalPolicy
{
    internal static DomainResult Evaluate(string interactionId, AgentProposalApprovalResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ApprovalDecision decision = Compute(result);
        return decision.Status switch
        {
            AgentInteractionStatus.ProposalPosted => DomainResult.Success([
                new ProposedAgentReplyApproved(interactionId, decision.Evidence),
                new ProposedAgentReplyPostingPending(interactionId, decision.Evidence),
                new ProposedAgentReplyPosted(interactionId, decision.Evidence),
            ]),
            AgentInteractionStatus.ProposalPostingPending => DomainResult.Success([
                new ProposedAgentReplyApproved(interactionId, decision.Evidence),
                new ProposedAgentReplyPostingPending(interactionId, decision.Evidence),
            ]),
            AgentInteractionStatus.ProposalPostingFailed => DomainResult.Success([
                new ProposedAgentReplyApproved(interactionId, decision.Evidence),
                new ProposedAgentReplyPostingPending(interactionId, decision.Evidence),
                new ProposedAgentReplyPostingFailed(interactionId, decision.Reason, decision.Evidence),
            ]),
            _ => DomainResult.Success([
                new ProposedAgentReplyApprovalFailed(interactionId, decision.Reason, decision.Evidence),
            ]),
        };
    }

    internal static DomainResult Reconcile(string interactionId, AgentProposedReplyApprovalEvidence pending, AgentProposalApprovalResult observation)
    {
        // A replay may learn the outcome of the existing approval. It cannot replace its actor or its intent.
        if (observation.AuthorizationVerdict != ApproverPolicyValidationStatus.Valid
            || pending.ProposalId != observation.ProposalId || pending.SourceConversationId != observation.SourceConversationId
            || pending.ApprovedVersionId != observation.SelectedVersionId || pending.MessageId != observation.MessageId
            || pending.IdempotencyKey != observation.IdempotencyKey || pending.AgentPartyId != observation.AgentPartyId)
        {
            return DomainResult.NoOp();
        }

        if (observation.Outcome is AgentProposalApprovalOutcome.Posted or AgentProposalApprovalOutcome.IdempotentPosted
            && pending.MessageId == observation.PostedConversationMessageId && !string.IsNullOrEmpty(pending.MessageId))
        {
            return DomainResult.Success([new ProposedAgentReplyPosted(interactionId,
                pending with { PostedConversationMessageId = observation.PostedConversationMessageId })]);
        }

        return observation.Outcome == AgentProposalApprovalOutcome.PostRejected
            ? DomainResult.Success([new ProposedAgentReplyPostingFailed(interactionId, AgentProposalApprovalFailureReason.PostRejected, pending)])
            : DomainResult.NoOp();
    }

    internal static AgentInteractionStatus Decide(AgentProposalApprovalResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return Compute(result).Status;
    }

    private static ApprovalDecision Compute(AgentProposalApprovalResult result)
    {
        AgentProposedReplyApprovalEvidence evidence = Evidence(result);
        bool authorized = result.AuthorizationVerdict == ApproverPolicyValidationStatus.Valid;
        if (!authorized)
        {
            return new ApprovalDecision(AgentInteractionStatus.ProposalApprovalFailed, MapFailureReason(result), evidence);
        }

        return result.Outcome switch
        {
            AgentProposalApprovalOutcome.Posted or AgentProposalApprovalOutcome.IdempotentPosted
                => new ApprovalDecision(AgentInteractionStatus.ProposalPosted, AgentProposalApprovalFailureReason.Unknown, evidence),
            AgentProposalApprovalOutcome.Approved or AgentProposalApprovalOutcome.IdempotentPostingPending
                => new ApprovalDecision(AgentInteractionStatus.ProposalPostingPending, AgentProposalApprovalFailureReason.Unknown, evidence),
            AgentProposalApprovalOutcome.PartyIdentityUnavailable
                or AgentProposalApprovalOutcome.MembershipUnavailable
                or AgentProposalApprovalOutcome.MembershipRejected
                or AgentProposalApprovalOutcome.ConversationUnavailable
                or AgentProposalApprovalOutcome.PostRejected
                or AgentProposalApprovalOutcome.AdapterFailure
                => new ApprovalDecision(AgentInteractionStatus.ProposalPostingFailed, MapFailureReason(result), evidence),
            _ => new ApprovalDecision(AgentInteractionStatus.ProposalApprovalFailed, MapFailureReason(result), evidence),
        };
    }

    private static AgentProposedReplyApprovalEvidence Evidence(AgentProposalApprovalResult r)
        => new(
            r.ProposalId,
            r.SourceConversationId,
            r.SelectedVersionId,
            r.ApproverPartyId,
            r.ApproverPolicyVersion,
            r.AuthorizationVerdict,
            r.DisclosureCategory,
            r.AgentPartyId,
            r.MessageId,
            r.IdempotencyKey,
            r.PostedConversationMessageId);

    private static AgentProposalApprovalFailureReason MapFailureReason(AgentProposalApprovalResult result)
    {
        if (result.AuthorizationVerdict != ApproverPolicyValidationStatus.Valid)
        {
            return result.AuthorizationVerdict == ApproverPolicyValidationStatus.Unknown
                ? AgentProposalApprovalFailureReason.PolicyFailure
                : AgentProposalApprovalFailureReason.NotAuthorized;
        }

        return result.Outcome switch
        {
            AgentProposalApprovalOutcome.PolicyFailure => AgentProposalApprovalFailureReason.PolicyFailure,
            AgentProposalApprovalOutcome.SelectedVersionMissing => AgentProposalApprovalFailureReason.SelectedVersionMissing,
            AgentProposalApprovalOutcome.SelectedVersionInvalid => AgentProposalApprovalFailureReason.SelectedVersionInvalid,
            AgentProposalApprovalOutcome.PartyIdentityUnavailable => AgentProposalApprovalFailureReason.PartyIdentityUnavailable,
            AgentProposalApprovalOutcome.MembershipUnavailable => AgentProposalApprovalFailureReason.MembershipUnavailable,
            AgentProposalApprovalOutcome.MembershipRejected => AgentProposalApprovalFailureReason.MembershipRejected,
            AgentProposalApprovalOutcome.ConversationUnavailable => AgentProposalApprovalFailureReason.ConversationUnavailable,
            AgentProposalApprovalOutcome.PostRejected => AgentProposalApprovalFailureReason.PostRejected,
            _ => AgentProposalApprovalFailureReason.AdapterFailure,
        };
    }

    private readonly record struct ApprovalDecision(
        AgentInteractionStatus Status,
        AgentProposalApprovalFailureReason Reason,
        AgentProposedReplyApprovalEvidence Evidence);
}
