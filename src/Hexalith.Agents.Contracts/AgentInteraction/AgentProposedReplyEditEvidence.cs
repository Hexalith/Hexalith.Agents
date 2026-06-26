using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, audit-ready evidence of one Proposed-Agent-Reply edit attempt, recorded on
/// <see cref="Events.ProposedAgentReplyEdited"/> and <see cref="Events.ProposedAgentReplyEditFailed"/> so an authorized
/// administrator can see WHICH proposal was edited (or attempted), against which Source Conversation, producing which new
/// edited version, FROM which source version, by which Approver, and under which policy basis — without any edited content
/// (AC1, AC3, AC4; AD-5, AD-14). It mirrors <see cref="AgentProposedReplyEvidence"/>: safe ids only, symmetric across
/// success + failure.
/// </summary>
/// <remarks>
/// Carries ONLY safe ids and safe classifications — deliberately NEVER the edited content, a raw provider/Conversations
/// payload, a stack trace, a raw policy internal/claim, or a secret (AD-14). The edit <em>timestamp</em> is the EventStore
/// event metadata (AD-3 — no wall-clock field). <see cref="EditorPartyId"/> is the authoring Approver's stable Party
/// reference (a reference, not PII — AD-7). The AC4 <b>policy basis</b> is the resolved approver-policy
/// <see cref="PolicyBasisVerdict"/> (a safe classification) plus the FR-7 <see cref="DisclosureCategory"/> that governs
/// how the basis is reported — user-visible/operator-only/redacted/omitted as configured.
/// </remarks>
/// <param name="ProposalId">The deterministic proposal identifier the edit targeted (the proposal created in Story 3.1; AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="EditedVersionId">The id of the new immutable edited version (no content — AD-14).</param>
/// <param name="SourceVersionId">The id of the version the edit was made from (its provenance).</param>
/// <param name="EditorPartyId">The authoring Approver's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (the policy basis version).</param>
/// <param name="PolicyBasisVerdict">The resolved approver-policy verdict — a safe classification of the basis on which the edit was authorized (AC4).</param>
/// <param name="DisclosureCategory">The FR-7 disclosure category governing how the policy basis is reported (AC4).</param>
public record AgentProposedReplyEditEvidence(
    string ProposalId,
    string SourceConversationId,
    string EditedVersionId,
    string SourceVersionId,
    string EditorPartyId,
    int ApproverPolicyVersion,
    ApproverPolicyValidationStatus PolicyBasisVerdict,
    ApproverPolicyBasisDisclosure DisclosureCategory);
