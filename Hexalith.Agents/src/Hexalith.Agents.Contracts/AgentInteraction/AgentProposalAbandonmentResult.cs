using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Server-assembled abandonment result consumed by the pure aggregate (AC2, AC4; AD-3, AD-5, AD-12, AD-14). The abandonment
/// orchestration resolves abandonment-time approver authorization and assembles this, then puts it on
/// <see cref="Commands.AbandonProposedAgentReply"/>; the pure aggregate decides on it and never resolves authorization or
/// reads any dependency itself. It carries safe ids + the policy basis only — no version content, no payload, no free text.
/// </summary>
/// <remarks>
/// A successful abandonment requires the <see cref="AgentProposalAbandonmentOutcome.Abandoned"/> outcome AND a
/// <see cref="ApproverPolicyValidationStatus.Valid"/> verdict; any other combination is a fail-closed decision. Mirrors
/// <see cref="AgentProposalRejectionResult"/> without a rationale code (abandonment carries no rationale).
/// </remarks>
/// <param name="Outcome">The abandonment outcome classification the aggregate decides on.</param>
/// <param name="ProposalId">The deterministic proposal identifier the abandonment targets (AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="ActorPartyId">The acting Approver's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (the policy basis version).</param>
/// <param name="AuthorizationVerdict">The resolved abandonment-time approver-policy verdict (the trusted authorization input).</param>
/// <param name="DisclosureCategory">The FR-7 disclosure category governing how the policy basis is reported (AC4).</param>
public sealed record AgentProposalAbandonmentResult(
    AgentProposalAbandonmentOutcome Outcome,
    string ProposalId,
    string SourceConversationId,
    string ActorPartyId,
    int ApproverPolicyVersion,
    ApproverPolicyValidationStatus AuthorizationVerdict,
    ApproverPolicyBasisDisclosure DisclosureCategory);
