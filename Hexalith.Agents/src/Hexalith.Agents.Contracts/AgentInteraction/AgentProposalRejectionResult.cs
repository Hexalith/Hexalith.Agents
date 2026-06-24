using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Server-assembled rejection result consumed by the pure aggregate (AC1, AC4; AD-3, AD-5, AD-12, AD-14). The rejection
/// orchestration resolves rejection-time approver authorization and assembles this, then puts it on
/// <see cref="Commands.RejectProposedAgentReply"/>; the pure aggregate decides on it and never resolves authorization or
/// reads any dependency itself. It carries safe ids + the policy basis only — no version content, no payload, no free text.
/// </summary>
/// <remarks>
/// <see cref="RationaleCode"/> is an optional policy-defined safe code/category (e.g. "off-topic", "duplicate") — NEVER free
/// text and NEVER generated content (AD-14). A successful rejection requires the <see cref="AgentProposalRejectionOutcome.Rejected"/>
/// outcome AND a <see cref="ApproverPolicyValidationStatus.Valid"/> verdict; any other combination is a fail-closed decision.
/// </remarks>
/// <param name="Outcome">The rejection outcome classification the aggregate decides on.</param>
/// <param name="ProposalId">The deterministic proposal identifier the rejection targets (AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="ActorPartyId">The acting Approver's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (the policy basis version).</param>
/// <param name="AuthorizationVerdict">The resolved rejection-time approver-policy verdict (the trusted authorization input).</param>
/// <param name="DisclosureCategory">The FR-7 disclosure category governing how the policy basis is reported (AC4).</param>
/// <param name="RationaleCode">The optional policy-defined safe rationale code/category (never free text or content — AD-14).</param>
public sealed record AgentProposalRejectionResult(
    AgentProposalRejectionOutcome Outcome,
    string ProposalId,
    string SourceConversationId,
    string ActorPartyId,
    int ApproverPolicyVersion,
    ApproverPolicyValidationStatus AuthorizationVerdict,
    ApproverPolicyBasisDisclosure DisclosureCategory,
    string? RationaleCode);
