namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The server-assembled input to the pure proposal-creation decision (AC1, AC3, AC4; AD-3, AD-14). The orchestration
/// assembles this from the selected generated version read + the optional expiry read, then puts it on
/// <see cref="Commands.CreateProposedAgentReply"/>; the pure aggregate decides on it and never reads any dependency itself
/// (AD-3). It mirrors <see cref="AgentResponsePostingResult"/> as the server→aggregate carrier.
/// </summary>
/// <remarks>
/// <b>Crucially, it carries NO generated content</b> — the content was already durably recorded on
/// <see cref="Events.AgentOutputGenerated"/>/state by Story 2.4; proposal creation transports only safe ids (AD-14). Every
/// member is a safe id: the deterministic <see cref="ProposalId"/> (AD-13), the opaque <see cref="SourceConversationId"/>,
/// the selected <see cref="ProposedVersionId"/>, the policy-snapshot versions, and the optional <see cref="ExpiresAt"/>. On
/// a fail-closed outcome the ids carried are the ones that were attempted (<see cref="ProposedVersionId"/>/
/// <see cref="ProposalId"/> empty when the version read failed before a version id was known).
/// </remarks>
/// <param name="Outcome">The server-assembled outcome classification the aggregate decides on.</param>
/// <param name="ProposalId">The deterministic proposal identifier (reused across retries — AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="ProposedVersionId">The selected generated version identifier held in the proposal (no content — AD-14).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time.</param>
/// <param name="ContentSafetyPolicyVersion">The content-safety policy version snapshotted at request time.</param>
/// <param name="ExpiresAt">The optional ISO-8601 expiry timestamp "where configured" (<see langword="null"/> when unconfigured).</param>
public record AgentProposalCreationResult(
    AgentProposalCreationOutcome Outcome,
    string ProposalId,
    string SourceConversationId,
    string ProposedVersionId,
    int ApproverPolicyVersion,
    int ContentSafetyPolicyVersion,
    string? ExpiresAt);
