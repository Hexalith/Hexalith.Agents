using System;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>Contract guards for Story 3.6 reject/abandon/expire terminal surfaces.</summary>
public sealed class AgentInteractionProposalTerminalContractsTests
{
    private static AgentProposedReplyRejectionEvidence RejectionEvidence { get; } = new(
        "proposal-001",
        "conversation-001",
        "actor-party-001",
        7,
        ApproverPolicyValidationStatus.Valid,
        ApproverPolicyBasisDisclosure.OperatorOnly,
        "OffTopic");

    private static AgentProposedReplyAbandonmentEvidence AbandonmentEvidence { get; } = new(
        "proposal-001",
        "conversation-001",
        "actor-party-001",
        7,
        ApproverPolicyValidationStatus.Valid,
        ApproverPolicyBasisDisclosure.OperatorOnly);

    private static AgentProposedReplyExpiryEvidence ExpiryEvidence { get; } = new(
        "proposal-001",
        "conversation-001",
        "2026-12-31T23:59:59Z");

    private static AgentProposalRejectionResult RejectionResult { get; } = new(
        AgentProposalRejectionOutcome.Rejected,
        "proposal-001",
        "conversation-001",
        "actor-party-001",
        7,
        ApproverPolicyValidationStatus.Valid,
        ApproverPolicyBasisDisclosure.OperatorOnly,
        "OffTopic");

    private static AgentProposalAbandonmentResult AbandonmentResult { get; } = new(
        AgentProposalAbandonmentOutcome.Abandoned,
        "proposal-001",
        "conversation-001",
        "actor-party-001",
        7,
        ApproverPolicyValidationStatus.Valid,
        ApproverPolicyBasisDisclosure.OperatorOnly);

    private static AgentProposalExpiryResult ExpiryResult { get; } = new(
        AgentProposalExpiryOutcome.Expired,
        "proposal-001",
        "conversation-001",
        "2026-12-31T23:59:59Z");

    [Fact]
    public void Terminal_events_are_payloads_and_structural_rejections_are_rejections()
    {
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyRejected)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyAbandoned)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyExpired)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyRejectionFailed)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyAbandonmentFailed)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyNotRejectableRejection)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyNotAbandonableRejection)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyNotExpirableRejection)).ShouldBeTrue();
    }

    [Fact]
    public void Terminal_contracts_round_trip_and_preserve_safe_ids_and_rationale_code()
    {
        RoundTrip(RejectionEvidence).ShouldBe(RejectionEvidence);
        RoundTrip(AbandonmentEvidence).ShouldBe(AbandonmentEvidence);
        RoundTrip(ExpiryEvidence).ShouldBe(ExpiryEvidence);
        RoundTrip(RejectionResult).ShouldBe(RejectionResult);
        RoundTrip(AbandonmentResult).ShouldBe(AbandonmentResult);
        RoundTrip(ExpiryResult).ShouldBe(ExpiryResult);
        RoundTrip(new RejectProposedAgentReply("interaction-001", RejectionResult)).Result.RationaleCode.ShouldBe("OffTopic");
        RoundTrip(new AbandonProposedAgentReply("interaction-001", AbandonmentResult)).Result.ProposalId.ShouldBe("proposal-001");
        RoundTrip(new ExpireProposedAgentReply("interaction-001", ExpiryResult)).Result.ExpiresAt.ShouldBe("2026-12-31T23:59:59Z");
        RoundTrip(new ProposedAgentReplyRejected("interaction-001", RejectionEvidence)).Evidence.RationaleCode.ShouldBe("OffTopic");
        RoundTrip(new ProposedAgentReplyExpired("interaction-001", ExpiryEvidence)).Evidence.ExpiresAt.ShouldBe("2026-12-31T23:59:59Z");
    }

    [Fact]
    public void Terminal_enums_serialize_by_name_and_append_ordinals_are_stable()
    {
        JsonSerializer.Serialize(AgentProposalRejectionOutcome.Rejected).ShouldBe("\"Rejected\"");
        JsonSerializer.Serialize(AgentProposalAbandonmentOutcome.Abandoned).ShouldBe("\"Abandoned\"");
        JsonSerializer.Serialize(AgentProposalExpiryOutcome.Expired).ShouldBe("\"Expired\"");
        JsonSerializer.Serialize(AgentProposedReplyNotRejectableReason.NotAuthorized).ShouldBe("\"NotAuthorized\"");
        JsonSerializer.Serialize(AgentProposedReplyNotExpirableReason.ExpiryNotReached).ShouldBe("\"ExpiryNotReached\"");

        // ProposedAgentReplyState terminal ordinals are append-only after PostingFailed (=7).
        ((int)ProposedAgentReplyState.Rejected).ShouldBe(8);
        ((int)ProposedAgentReplyState.Abandoned).ShouldBe(9);
        ((int)ProposedAgentReplyState.Expired).ShouldBe(10);

        // AgentInteractionStatus terminal ordinals are append-only after ProposalApprovalFailed (=22).
        ((int)AgentInteractionStatus.ProposalRejected).ShouldBe(23);
        ((int)AgentInteractionStatus.ProposalAbandoned).ShouldBe(24);
        ((int)AgentInteractionStatus.ProposalExpired).ShouldBe(25);
        ((int)AgentInteractionStatus.ProposalRejectionFailed).ShouldBe(26);
        ((int)AgentInteractionStatus.ProposalAbandonmentFailed).ShouldBe(27);
    }

    [Fact]
    public void Terminal_safe_surfaces_have_no_content_bearing_member_and_rationale_is_a_code()
    {
        Type[] safeTypes =
        [
            typeof(AgentProposedReplyRejectionEvidence),
            typeof(AgentProposedReplyAbandonmentEvidence),
            typeof(AgentProposedReplyExpiryEvidence),
            typeof(AgentProposalRejectionResult),
            typeof(AgentProposalAbandonmentResult),
            typeof(AgentProposalExpiryResult),
            typeof(ProposedAgentReplyRejected),
            typeof(ProposedAgentReplyAbandoned),
            typeof(ProposedAgentReplyExpired),
            typeof(ProposedAgentReplyRejectionFailed),
            typeof(ProposedAgentReplyAbandonmentFailed),
            typeof(ProposedAgentReplyNotRejectableRejection),
            typeof(ProposedAgentReplyNotAbandonableRejection),
            typeof(ProposedAgentReplyNotExpirableRejection),
        ];

        foreach (Type type in safeTypes)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                new[] { "GeneratedContent", "Content", "Text", "Prompt", "Payload", "Body" }
                    .ShouldNotContain(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase));
            }
        }

        // The rationale rides as a safe code/category (a short policy-defined token), never generated content.
        JsonSerializer.Serialize(RejectionResult).ShouldNotContain("secret-generated-content");
        RejectionResult.RationaleCode.ShouldBe("OffTopic");
        RejectionEvidence.RationaleCode.ShouldBe("OffTopic");
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
