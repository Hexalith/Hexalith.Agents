using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The current sub-state of a Proposed Agent Reply created in Confirmation Response Mode (AC1; FR-13, FR-14; AD-5 proposal
/// lifecycle). Story 3.1 records the initial <see cref="Pending"/> state set when a proposal is created and awaits
/// authorized Approver action; Story 3.3 appends <see cref="Edited"/> for a proposal whose latest version was edited (still
/// awaiting approval); Story 3.4 appends <see cref="Regenerated"/> for a proposal whose latest accepted version was produced
/// by a regeneration attempt (still awaiting approval). Story 3.5 appends <see cref="Approved"/>, <see cref="PostingPending"/>,
/// <see cref="Posted"/>, and <see cref="PostingFailed"/> for the approve-and-post lifecycle. Story 3.6 appends the three AD-5
/// terminal states <see cref="Rejected"/>, <see cref="Abandoned"/>, and <see cref="Expired"/>: once a proposal reaches one
/// of these it can no longer be approved, edited, regenerated, or posted (FR-18; AD-5), and every preserved version is
/// retained for authorized audit (FR-14).
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete state; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel. Ordinals are append-only — never reorder/renumber (mirroring how <see cref="AgentGenerationKind"/>
/// appends its kinds additively). The full AD-5 proposal lifecycle — including the <c>Rejected</c>/<c>Abandoned</c>/<c>Expired</c>
/// terminal states — is now owned and present; any further states stay additive.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProposedAgentReplyState
{
    /// <summary>Not-a-state sentinel — an absent/unrecognized state never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The proposal was created and awaits authorized Approver action (the initial state; Story 3.1).</summary>
    Pending,

    /// <summary>The proposal's latest version was edited by an authorized Approver and still awaits approval (Story 3.3; AC1).</summary>
    Edited,

    /// <summary>The proposal's latest accepted version was produced by an authorized Approver regeneration and still awaits approval (Story 3.4; AC2).</summary>
    Regenerated,

    /// <summary>Exactly one preserved proposal version has been approved, but posting has not yet been attempted or completed (Story 3.5).</summary>
    Approved,

    /// <summary>The approved proposal version is frozen and posting is pending; it is still not a Conversation Message (Story 3.5).</summary>
    PostingPending,

    /// <summary>The approved proposal version has been posted as a Conversation Message authored by the Agent Party identity (Story 3.5).</summary>
    Posted,

    /// <summary>Posting the approved proposal version failed closed; the approved version remains frozen for retry/audit (Story 3.5).</summary>
    PostingFailed,

    /// <summary>An authorized Approver rejected the proposal; it is terminal and can never be approved/edited/regenerated/posted, but all versions are preserved for audit (Story 3.6; AC1).</summary>
    Rejected,

    /// <summary>An authorized Approver abandoned the proposal; it is terminal and can never act again, with all versions preserved for audit (Story 3.6; AC2).</summary>
    Abandoned,

    /// <summary>The configured proposal expiry elapsed; the proposal is terminal and can never be approved/edited/regenerated/posted, with all versions preserved for audit (Story 3.6; AC3).</summary>
    Expired,
}
