using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The current sub-state of a Proposed Agent Reply created in Confirmation Response Mode (AC1; FR-13, FR-14; AD-5 proposal
/// lifecycle). Story 3.1 records the initial <see cref="Pending"/> state set when a proposal is created and awaits
/// authorized Approver action; Story 3.3 appends <see cref="Edited"/> for a proposal whose latest version was edited (still
/// awaiting approval); Story 3.4 appends <see cref="Regenerated"/> for a proposal whose latest accepted version was produced
/// by a regeneration attempt (still awaiting approval). The rest of the AD-5 lifecycle is reserved here additively so the
/// proposal surface does not need a contract change when those stories arrive.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete state; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel. Ordinals are append-only — never reorder/renumber. Do NOT add the remaining deferred lifecycle
/// values in this story — <c>Approved</c>/<c>Rejected</c>/<c>Abandoned</c>/<c>Expired</c> (Stories 3.5/3.6), and
/// <c>PostingPending</c>/<c>Posted</c>/<c>PostingFailed</c> (Story 3.5) are owned by Stories 3.5/3.6 and must NOT be added
/// here (mirroring how <see cref="AgentGenerationKind"/> appends its kinds additively).
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
}
