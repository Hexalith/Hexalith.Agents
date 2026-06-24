using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The current sub-state of a Proposed Agent Reply created in Confirmation Response Mode (AC1; FR-13, FR-14; AD-5 proposal
/// lifecycle). Story 3.1 records the initial <see cref="Pending"/> state set when a proposal is created and awaits
/// authorized Approver action; Story 3.3 appends <see cref="Edited"/> for a proposal whose latest version was edited (still
/// awaiting approval); Story 3.4 appends <see cref="Regenerated"/> for a proposal whose latest accepted version was produced
/// by a regeneration attempt (still awaiting approval). Story 3.5 appends <see cref="Approved"/>, <see cref="PostingPending"/>,
/// <see cref="Posted"/>, and <see cref="PostingFailed"/> for the approve-and-post lifecycle. The remaining AD-5 terminal
/// states are reserved here additively so the proposal surface does not need a contract change when those stories arrive.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete state; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel. Ordinals are append-only — never reorder/renumber. The remaining deferred lifecycle values —
/// <c>Rejected</c>/<c>Abandoned</c>/<c>Expired</c> (Story 3.6) — are owned by Story 3.6 and must NOT be added here until
/// that story (mirroring how <see cref="AgentGenerationKind"/> appends its kinds additively).
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
}
