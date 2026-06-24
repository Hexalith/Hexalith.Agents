using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The server-assembled input to the pure proposal-regeneration decision (AC1, AC2, AC3, AC4; AD-3, AD-5, AD-9, AD-13,
/// AD-14). The regeneration orchestration resolves regeneration-time authorization, re-reads the same Source Conversation,
/// re-invokes the provider behind its adapter, runs the content-safety gate, derives the deterministic regenerated-version
/// id, and assembles this, then puts it on <see cref="Commands.RegenerateProposedAgentReply"/>; the pure aggregate decides on
/// it and never reads any dependency or resolves authorization itself (AD-3). It mirrors <see cref="AgentProposalEditResult"/>
/// as the server→aggregate carrier (with an approver verdict) and <see cref="AgentOutputGenerationResult"/> in carrying the
/// provider/model/policy metadata and the optional content-bearing version.
/// </summary>
/// <remarks>
/// <b>Content confinement (AD-14):</b> the freshly generated content rides the write path here on
/// <see cref="RegeneratedVersion"/> (<see cref="AgentGenerationKind.Regenerated"/>) — its legitimate, payload-protected
/// durable home, exactly analogous to how <see cref="Events.AgentOutputGenerated"/> carries generated content. The
/// orchestrator populates <see cref="RegeneratedVersion"/> ONLY for the <see cref="AgentProposalRegenerationOutcome.Regenerated"/>
/// success outcome (after the content passed safety); for every failure outcome (including a safety block) it is
/// <see langword="null"/>, so unsafe/failed content never reaches the aggregate and no approvable version is ever built
/// (AD-5, AD-14). Every other member is a safe id/classification/numeric: the deterministic <see cref="RegeneratedVersionId"/>
/// and <see cref="RegenerationAttemptId"/> (AD-13, present on both success and failure for symmetric evidence), the existing
/// <see cref="ProposalId"/>, the opaque <see cref="SourceConversationId"/>, the <see cref="RequesterPartyId"/> reference, the
/// snapshotted provider/model/policy versions, the snapshotted <see cref="ApproverPolicyVersion"/>, the resolved
/// <see cref="AuthorizationVerdict"/>, and the <see cref="DisclosureCategory"/> that govern the AC4 policy basis.
/// </remarks>
/// <param name="Outcome">The server-assembled outcome classification the aggregate decides on.</param>
/// <param name="RegenerationAttemptId">The deterministic regeneration attempt identifier (reused across retries so the aggregate dedupes; AD-13).</param>
/// <param name="RegeneratedVersionId">The deterministic regenerated-version id (derived from the attempt; present on success AND failure for symmetric safe evidence; AD-13).</param>
/// <param name="RegeneratedVersion">The new immutable regenerated version (carries the fresh content + <see cref="RegeneratedVersionId"/> + <see cref="AgentGenerationKind.Regenerated"/>); <see langword="null"/> on every non-success outcome (AD-5, AD-14).</param>
/// <param name="AuthorizationVerdict">The resolved regeneration-time approver-policy verdict (the trusted authorization input; recorded as the AC4 policy basis).</param>
/// <param name="ProposalId">The deterministic proposal identifier the regeneration targets (the proposal created in Story 3.1; AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="RequesterPartyId">The requesting Approver's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="ProviderId">The safe provider identifier the regeneration targeted (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The safe model identifier the regeneration targeted (a reference, not a secret — AD-9).</param>
/// <param name="ProviderCapabilityVersion">The provider capability version backing the regeneration.</param>
/// <param name="ContentSafetyPolicyVersion">The Content Safety Policy version evaluated against the regenerated content.</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (the policy basis version).</param>
/// <param name="DisclosureCategory">The FR-7 disclosure category governing how the policy basis is reported (AC4).</param>
/// <param name="PromptTokenCount">The prompt/input token usage (<c>0</c> when the provider produced none).</param>
/// <param name="OutputTokenCount">The regenerated-output token usage (<c>0</c> when the provider produced none).</param>
public record AgentProposalRegenerationResult(
    AgentProposalRegenerationOutcome Outcome,
    string RegenerationAttemptId,
    string RegeneratedVersionId,
    AgentGeneratedVersion? RegeneratedVersion,
    ApproverPolicyValidationStatus AuthorizationVerdict,
    string ProposalId,
    string SourceConversationId,
    string RequesterPartyId,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int ContentSafetyPolicyVersion,
    int ApproverPolicyVersion,
    ApproverPolicyBasisDisclosure DisclosureCategory,
    int PromptTokenCount,
    int OutputTokenCount);
