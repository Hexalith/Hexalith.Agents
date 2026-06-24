using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, audit-ready evidence of one Proposed-Agent-Reply abandonment attempt, recorded on
/// <see cref="Events.ProposedAgentReplyAbandoned"/> and <see cref="Events.ProposedAgentReplyAbandonmentFailed"/> so an
/// authorized administrator can see WHICH proposal was abandoned (or attempted), against which Source Conversation, by which
/// Approver, and under which policy basis — without any version content (AC2, AC4; FR-24; AD-5, AD-14). It mirrors
/// <see cref="AgentProposedReplyRejectionEvidence"/> without a rationale code: safe ids only, symmetric across success + failure.
/// </summary>
/// <remarks>
/// Carries ONLY safe ids and safe classifications — deliberately NEVER any generated/edited content, a raw
/// provider/Conversations payload, a stack trace, a raw policy internal, or a secret (AD-14). The abandonment
/// <em>timestamp</em> is the EventStore event metadata (AD-3 — no wall-clock field).
/// </remarks>
/// <param name="ProposalId">The deterministic proposal identifier the abandonment targeted (AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="ActorPartyId">The acting Approver's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (the policy basis version).</param>
/// <param name="PolicyBasisVerdict">The resolved approver-policy verdict — a safe classification of the basis on which the abandonment was authorized (AC4).</param>
/// <param name="DisclosureCategory">The FR-7 disclosure category governing how the policy basis is reported (AC4).</param>
public record AgentProposedReplyAbandonmentEvidence(
    string ProposalId,
    string SourceConversationId,
    string ActorPartyId,
    int ApproverPolicyVersion,
    ApproverPolicyValidationStatus PolicyBasisVerdict,
    ApproverPolicyBasisDisclosure DisclosureCategory);
