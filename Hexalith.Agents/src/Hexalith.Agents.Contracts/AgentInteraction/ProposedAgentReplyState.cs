using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The current sub-state of a Proposed Agent Reply created in Confirmation Response Mode (AC1; FR-13; AD-5 proposal
/// lifecycle). Story 3.1 records only the initial <see cref="Pending"/> state set when a proposal is created and awaits
/// authorized Approver action; the rest of the AD-5 lifecycle is reserved here additively so the proposal surface does not
/// need a contract change when those stories arrive.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete state; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel. Do NOT add the deferred lifecycle values in this story — <c>Edited</c>/<c>Regenerated</c>
/// (Stories 3.3/3.4), <c>Approved</c>/<c>Rejected</c>/<c>Abandoned</c>/<c>Expired</c> (Stories 3.5/3.6), and
/// <c>PostingPending</c>/<c>Posted</c>/<c>PostingFailed</c> (Story 3.5) are owned by Stories 3.3–3.6 and must NOT be added
/// here (mirroring how <see cref="AgentGenerationKind"/> reserves its deferred edit/regenerate kinds additively).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProposedAgentReplyState
{
    /// <summary>Not-a-state sentinel — an absent/unrecognized state never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The proposal was created and awaits authorized Approver action (the V1 initial state; Story 3.1).</summary>
    Pending,
}
