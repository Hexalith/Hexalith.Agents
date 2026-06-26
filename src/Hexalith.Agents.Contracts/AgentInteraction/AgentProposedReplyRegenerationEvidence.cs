using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, audit-ready evidence of one Proposed-Agent-Reply regeneration attempt, recorded on
/// <see cref="Events.ProposedAgentReplyRegenerated"/> and <see cref="Events.ProposedAgentReplyRegenerationFailed"/> so an
/// authorized administrator can see WHICH proposal was regenerated (or attempted), against which Source Conversation,
/// producing which new regenerated version, by which Approver, with which Provider/model, and under which policy basis —
/// without any regenerated content (AC1, AC3, AC4; AD-5, AD-9, AD-14). It mirrors <see cref="AgentProposedReplyEditEvidence"/>
/// and <see cref="AgentGenerationAttemptEvidence"/>: safe ids/numerics only, symmetric across success + failure.
/// </summary>
/// <remarks>
/// Carries ONLY safe ids and safe classifications — deliberately NEVER the regenerated content, a prompt, a raw provider/
/// Conversations payload, a stack trace, a raw policy internal/claim, or a secret (AD-9, AD-14). The regeneration
/// <em>timestamp</em> is the EventStore event metadata (AD-3 — no wall-clock field). <see cref="RequesterPartyId"/> is the
/// requesting Approver's stable Party reference (a reference, not PII — AD-7). The AC4 <b>policy basis</b> is the resolved
/// approver-policy <see cref="PolicyBasisVerdict"/> (a safe classification) plus the FR-7 <see cref="DisclosureCategory"/>
/// that governs how the basis is reported — user-visible/operator-only/redacted/omitted as configured.
/// </remarks>
/// <param name="ProposalId">The deterministic proposal identifier the regeneration targeted (the proposal created in Story 3.1; AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="RegeneratedVersionId">The id of the new immutable regenerated version (no content — AD-14).</param>
/// <param name="RegenerationAttemptId">The deterministic regeneration attempt identifier (reused across retries — AD-13).</param>
/// <param name="RequesterPartyId">The requesting Approver's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="ProviderId">The safe provider identifier the regeneration targeted (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The safe model identifier the regeneration targeted (a reference, not a secret — AD-9).</param>
/// <param name="ProviderCapabilityVersion">The provider capability version backing the regeneration.</param>
/// <param name="ContentSafetyPolicyVersion">The Content Safety Policy version evaluated against the regenerated content.</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (the policy basis version).</param>
/// <param name="PolicyBasisVerdict">The resolved approver-policy verdict — a safe classification of the basis on which the regeneration was authorized (AC4).</param>
/// <param name="DisclosureCategory">The FR-7 disclosure category governing how the policy basis is reported (AC4).</param>
public record AgentProposedReplyRegenerationEvidence(
    string ProposalId,
    string SourceConversationId,
    string RegeneratedVersionId,
    string RegenerationAttemptId,
    string RequesterPartyId,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int ContentSafetyPolicyVersion,
    int ApproverPolicyVersion,
    ApproverPolicyValidationStatus PolicyBasisVerdict,
    ApproverPolicyBasisDisclosure DisclosureCategory);
