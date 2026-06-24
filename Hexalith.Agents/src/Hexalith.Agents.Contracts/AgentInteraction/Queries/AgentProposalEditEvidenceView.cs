using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Safe, audit-ready projection of a Proposed Agent Reply's latest edit for authorized inspection (AC4; FR-14, FR-15). It
/// exposes ONLY safe references — the proposal/edited/source version ids, the editor Party reference, the policy basis, the
/// approver-policy version, and the optional ISO-8601 edit timestamp (from EventStore event metadata) — and deliberately
/// <b>NEVER</b> the edited/generated content, a raw provider/Conversations payload, an EventStore stream name, a stack
/// trace, or a secret (AD-14). It mirrors <see cref="AgentInteractionContextEvidenceView"/>: safe ids/enums only, no
/// content field at all, so an edited reply is never rendered as a Conversation Message by construction.
/// </summary>
/// <remarks>
/// The edited content's sole durable home stays the <c>ProposedAgentReplyEdited</c> event/state version; this view
/// references only the version <em>ids</em>. The editor must read version content for display through an authorized reader
/// port from the durable version history, never from a content-bearing projection (there is none; AD-14). The live
/// computation of this view (resolving the edit event + metadata timestamp against the read path) is part of the deferred
/// Epic-4 read-model binding; the stable contract lands here. All member names are kept clear of the forbidden secret
/// tokens (<c>Secret</c>/<c>ApiKey</c>/<c>Credential</c>/<c>Password</c>/<c>ConnectionString</c>).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate-id row handle).</param>
/// <param name="ProposalId">The deterministic proposal identifier (created in Story 3.1; AD-13).</param>
/// <param name="State">The proposal sub-state (<c>Edited</c> after an edit; reserved states render through a total default).</param>
/// <param name="EditedVersionId">The id of the latest edited version (no content; AD-14).</param>
/// <param name="SourceVersionId">The id of the version the edit was made from (its provenance).</param>
/// <param name="EditorPartyId">The authoring Approver's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (the policy basis version).</param>
/// <param name="PolicyBasisVerdict">The resolved approver-policy verdict — a safe classification of the basis on which the edit was authorized.</param>
/// <param name="DisclosureCategory">The FR-7 disclosure category governing how the policy basis is reported.</param>
/// <param name="EditedAt">The optional ISO-8601 edit timestamp sourced from EventStore event metadata (<see langword="null"/> when unavailable).</param>
public record AgentProposalEditEvidenceView(
    string AgentInteractionId,
    string ProposalId,
    ProposedAgentReplyState State,
    string EditedVersionId,
    string SourceVersionId,
    string EditorPartyId,
    int ApproverPolicyVersion,
    ApproverPolicyValidationStatus PolicyBasisVerdict,
    ApproverPolicyBasisDisclosure DisclosureCategory,
    string? EditedAt);
