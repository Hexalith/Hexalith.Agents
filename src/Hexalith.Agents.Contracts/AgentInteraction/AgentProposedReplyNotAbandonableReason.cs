using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> an abandon-proposed-reply command could not be evaluated, or why an abandonment
/// failed closed (AC2, AC4; FR-18, AD-12). Distinct from a recorded <em>terminal abandonment</em>: a not-abandonable command
/// produces no state change (mirroring the edit's not-editable rejection), whereas a successful abandonment moves the
/// proposal to <see cref="ProposedAgentReplyState.Abandoned"/>. Carried on
/// <see cref="Events.Rejections.ProposedAgentReplyNotAbandonableRejection"/> (structural) and on
/// <see cref="Events.ProposedAgentReplyAbandonmentFailed"/> (fail-closed decision).
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel. Serialized by name so an absent value never resolves to a
/// concrete classification. <see cref="InteractionNotProposed"/> and <see cref="ProposalNotPending"/> are the aggregate's
/// structural rejections (a terminal/non-pending proposal can no longer be abandoned); <see cref="NotAuthorized"/> is the
/// orchestrator's fail-closed denial when abandonment-time authorization does not resolve to an authorized Approver (FR-7;
/// AD-12). Mirrors <see cref="AgentProposedReplyNotRejectableReason"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposedReplyNotAbandonableReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The interaction has no proposal to abandon (no proposal was created, or the stream has no recorded request).</summary>
    InteractionNotProposed,

    /// <summary>The proposal has reached a terminal/non-pending state (approved, rejected, abandoned, expired, posted) and can no longer be abandoned.</summary>
    ProposalNotPending,

    /// <summary>The caller is not an authorized Approver for this proposal under the snapshotted Approver Policy + current dependencies (fail closed — FR-7, AD-12).</summary>
    NotAuthorized,
}
