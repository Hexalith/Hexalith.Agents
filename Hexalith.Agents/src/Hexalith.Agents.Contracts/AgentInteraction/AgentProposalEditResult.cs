using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The server-assembled input to the pure proposal-edit decision (AC1, AC2, AC4; AD-3, AD-5, AD-14). The edit
/// orchestration resolves edit-time authorization, derives the deterministic edited-version id, and assembles this from
/// the user-supplied edited content, then puts it on <see cref="Commands.EditProposedAgentReply"/>; the pure aggregate
/// decides on it and never reads any dependency or resolves authorization itself (AD-3). It mirrors
/// <see cref="AgentProposalCreationResult"/> as the server→aggregate carrier, with one key difference:
/// <b>it carries the edited content</b> on <see cref="EditedVersion"/>.
/// </summary>
/// <remarks>
/// <b>Content confinement (AD-14):</b> unlike the create slice (where content was already durable and the orchestrator
/// discarded it), the edited content <em>originates from the user</em>, so it travels the write path here on
/// <see cref="EditedVersion"/> (<see cref="AgentGenerationKind.Edited"/>) — the legitimate, payload-protected durable home,
/// exactly analogous to how <see cref="Events.AgentOutputGenerated"/> carries generated content. The edited content must
/// NOT appear on the safe evidence, the orchestrator outcome, any rejection, or any read view. Every other member is a
/// safe id/classification: the deterministic <see cref="EditedVersion"/>.<c>VersionId</c> (AD-13), the existing
/// <see cref="ProposalId"/>, the opaque <see cref="SourceConversationId"/>, the snapshotted
/// <see cref="ApproverPolicyVersion"/>, the resolved <see cref="AuthorizationVerdict"/>, and the
/// <see cref="DisclosureCategory"/> that govern the AC4 policy basis.
/// </remarks>
/// <param name="Outcome">The server-assembled outcome classification the aggregate decides on.</param>
/// <param name="EditedVersion">The new immutable edited version (carries the edited content + the deterministic edited <c>VersionId</c> + <see cref="AgentGenerationKind.Edited"/> + <c>SourceVersionId</c> + <c>EditorPartyId</c>).</param>
/// <param name="AuthorizationVerdict">The resolved edit-time approver-policy verdict (the trusted authorization input; recorded as the AC4 policy basis).</param>
/// <param name="ProposalId">The deterministic proposal identifier the edit targets (the proposal created in Story 3.1; AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (the policy basis version).</param>
/// <param name="DisclosureCategory">The FR-7 disclosure category governing how the policy basis is reported (AC4).</param>
public record AgentProposalEditResult(
    AgentProposalEditOutcome Outcome,
    AgentGeneratedVersion EditedVersion,
    ApproverPolicyValidationStatus AuthorizationVerdict,
    string ProposalId,
    string SourceConversationId,
    int ApproverPolicyVersion,
    ApproverPolicyBasisDisclosure DisclosureCategory);
