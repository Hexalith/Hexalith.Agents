using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Safe, audit-ready projection of a Proposed Agent Reply's latest regeneration for authorized inspection (AC3, AC4;
/// FR-14, FR-16). It exposes ONLY safe references — the proposal/regenerated-version/attempt ids, the requester Party
/// reference, the source Conversation reference, the provider/model/capability/content-safety/approver-policy versions, the
/// policy basis, the safe failure class, and the optional ISO-8601 regeneration timestamp (from EventStore event metadata) —
/// and deliberately <b>NEVER</b> the regenerated/generated content, a prompt, a raw provider/Conversations payload, an
/// EventStore stream name, a stack trace, or a secret (AD-9, AD-14). It mirrors <see cref="AgentProposalEditEvidenceView"/>:
/// safe ids/enums only, no content field at all, so a regenerated reply is never rendered as a Conversation Message by
/// construction.
/// </summary>
/// <remarks>
/// The regenerated content's sole durable home stays the <c>ProposedAgentReplyRegenerated</c> event/state version; this view
/// references only the version <em>ids</em>. The editor must read version content for display through an authorized reader
/// port from the durable version history, never from a content-bearing projection (there is none; AD-14). The live
/// computation of this view (resolving the regeneration event + metadata timestamp against the read path) is part of the
/// deferred Epic-4 read-model binding; the stable contract lands here. All member names are kept clear of the forbidden
/// secret tokens (<c>Secret</c>/<c>ApiKey</c>/<c>Credential</c>/<c>Password</c>/<c>ConnectionString</c>).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate-id row handle).</param>
/// <param name="ProposalId">The deterministic proposal identifier (created in Story 3.1; AD-13).</param>
/// <param name="State">The proposal sub-state (<c>Regenerated</c> after a successful regeneration; reserved states render through a total default).</param>
/// <param name="RegeneratedVersionId">The id of the latest regenerated version (no content; AD-14).</param>
/// <param name="RegenerationAttemptId">The deterministic regeneration attempt id (reused across retries; AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="RequesterPartyId">The requesting Approver's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="ProviderId">The safe provider identifier the regeneration targeted (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The safe model identifier the regeneration targeted (a reference, not a secret — AD-9).</param>
/// <param name="ProviderCapabilityVersion">The provider capability version backing the regeneration.</param>
/// <param name="ContentSafetyPolicyVersion">The Content Safety Policy version evaluated against the regenerated content.</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (the policy basis version).</param>
/// <param name="PolicyBasisVerdict">The resolved approver-policy verdict — a safe classification of the basis on which the regeneration was authorized.</param>
/// <param name="DisclosureCategory">The FR-7 disclosure category governing how the policy basis is reported.</param>
/// <param name="FailureReason">The safe regeneration-failure classification (<c>Unknown</c> on a successful regeneration).</param>
/// <param name="RegeneratedAt">The optional ISO-8601 regeneration timestamp sourced from EventStore event metadata (<see langword="null"/> when unavailable).</param>
public record AgentProposalRegenerationEvidenceView(
    string AgentInteractionId,
    string ProposalId,
    ProposedAgentReplyState State,
    string RegeneratedVersionId,
    string RegenerationAttemptId,
    string SourceConversationId,
    string RequesterPartyId,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int ContentSafetyPolicyVersion,
    int ApproverPolicyVersion,
    ApproverPolicyValidationStatus PolicyBasisVerdict,
    ApproverPolicyBasisDisclosure DisclosureCategory,
    AgentProposalRegenerationFailureReason FailureReason,
    string? RegeneratedAt);
