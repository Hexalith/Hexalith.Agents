using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>Contract guards for Story 3.5 approval/posting surfaces.</summary>
public sealed class AgentInteractionProposalApprovalContractsTests
{
    private static AgentProposedReplyApprovalEvidence Evidence { get; } = new(
        "proposal-001",
        "conversation-001",
        "version-001",
        "approver-party-001",
        7,
        ApproverPolicyValidationStatus.Valid,
        ApproverPolicyBasisDisclosure.UserVisible,
        "agent-party-001",
        "message-001",
        "idem-001",
        "message-001");

    private static AgentProposalApprovalResult Result { get; } = new(
        AgentProposalApprovalOutcome.Posted,
        Evidence.ProposalId,
        Evidence.SourceConversationId,
        Evidence.ApprovedVersionId,
        Evidence.ApproverPartyId,
        Evidence.ApproverPolicyVersion,
        Evidence.PolicyBasisVerdict,
        Evidence.DisclosureCategory,
        Evidence.AgentPartyId,
        Evidence.MessageId,
        Evidence.IdempotencyKey,
        Evidence.PostedConversationMessageId);

    [Fact]
    public void Approval_events_are_payloads_and_structural_rejection_is_a_rejection()
    {
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyApproved)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyPostingPending)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyPosted)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyApprovalFailed)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyPostingFailed)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyNotApprovableRejection)).ShouldBeTrue();
    }

    [Fact]
    public void Approval_contracts_round_trip_and_preserve_selected_version_id()
    {
        RoundTrip(Evidence).ShouldBe(Evidence);
        RoundTrip(Result).ShouldBe(Result);
        RoundTrip(new ApproveProposedAgentReply("interaction-001", Result)).Result.SelectedVersionId.ShouldBe("version-001");
        RoundTrip(new ProposedAgentReplyApproved("interaction-001", Evidence)).Evidence.ApprovedVersionId.ShouldBe("version-001");
        RoundTrip(new ProposedAgentReplyPostingFailed("interaction-001", AgentProposalApprovalFailureReason.AdapterFailure, Evidence)).Evidence.ApprovedVersionId.ShouldBe("version-001");
    }

    [Fact]
    public void Approval_enums_serialize_by_name_and_append_ordinals_are_stable()
    {
        JsonSerializer.Serialize(AgentProposalApprovalOutcome.Posted).ShouldBe("\"Posted\"");
        JsonSerializer.Serialize(AgentProposalApprovalFailureReason.SelectedVersionInvalid).ShouldBe("\"SelectedVersionInvalid\"");
        JsonSerializer.Serialize(AgentProposedReplyNotApprovableReason.DifferentVersionAlreadyApproved).ShouldBe("\"DifferentVersionAlreadyApproved\"");

        ((int)AgentInteractionStatus.ProposalRegenerationFailed).ShouldBe(17);
        ((int)AgentInteractionStatus.ProposalApproved).ShouldBe(18);
        ((int)AgentInteractionStatus.ProposalPostingPending).ShouldBe(19);
        ((int)AgentInteractionStatus.ProposalPosted).ShouldBe(20);
        ((int)AgentInteractionStatus.ProposalPostingFailed).ShouldBe(21);
        ((int)AgentInteractionStatus.ProposalApprovalFailed).ShouldBe(22);
        ((int)ProposedAgentReplyState.Approved).ShouldBe(4);
        ((int)ProposedAgentReplyState.PostingPending).ShouldBe(5);
        ((int)ProposedAgentReplyState.Posted).ShouldBe(6);
        ((int)ProposedAgentReplyState.PostingFailed).ShouldBe(7);
    }

    [Fact]
    public void Approval_safe_surfaces_have_no_content_bearing_member()
    {
        Type[] safeTypes =
        [
            typeof(AgentProposedReplyApprovalEvidence),
            typeof(AgentProposalApprovalEvidenceView),
            typeof(AgentProposalApprovalEvidenceResult),
            typeof(GetAgentProposalApprovalEvidenceQuery),
            typeof(AgentProposalApprovalResult),
            typeof(ProposedAgentReplyApproved),
            typeof(ProposedAgentReplyPostingPending),
            typeof(ProposedAgentReplyPosted),
            typeof(ProposedAgentReplyApprovalFailed),
            typeof(ProposedAgentReplyPostingFailed),
            typeof(ProposedAgentReplyNotApprovableRejection),
        ];

        foreach (Type type in safeTypes)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                new[] { "GeneratedContent", "Content", "Text", "Prompt", "Payload", "Body" }
                    .ShouldNotContain(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase));
            }

            JsonSerializer.Serialize<object>(type == typeof(AgentProposalApprovalResult) ? Result : Evidence)
                .ShouldNotContain("secret-generated-content");
        }
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
