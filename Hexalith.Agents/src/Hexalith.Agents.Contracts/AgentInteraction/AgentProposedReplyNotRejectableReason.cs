using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> a reject-proposed-reply command could not be evaluated, or why a rejection
/// failed closed (AC1, AC4; FR-18, AD-12). Distinct from a recorded <em>terminal rejection</em>: a not-rejectable command
/// produces no state change (mirroring the edit's not-editable rejection), whereas a successful rejection moves the proposal
/// to <see cref="ProposedAgentReplyState.Rejected"/>. Carried on
/// <see cref="Events.Rejections.ProposedAgentReplyNotRejectableRejection"/> (structural) and on
/// <see cref="Events.ProposedAgentReplyRejectionFailed"/> (fail-closed decision).
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel. Serialized by name so an absent value never resolves to a
/// concrete classification. <see cref="InteractionNotProposed"/> and <see cref="ProposalNotPending"/> are the aggregate's
/// structural rejections (a terminal/non-pending proposal can no longer be rejected); <see cref="NotAuthorized"/> is the
/// orchestrator's fail-closed denial when rejection-time authorization does not resolve to an authorized Approver (FR-7;
/// AD-12). Mirrors <see cref="AgentProposedReplyNotEditableReason"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposedReplyNotRejectableReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The interaction has no proposal to reject (no proposal was created, or the stream has no recorded request).</summary>
    InteractionNotProposed,

    /// <summary>The proposal has reached a terminal/non-pending state (approved, rejected, abandoned, expired, posted) and can no longer be rejected.</summary>
    ProposalNotPending,

    /// <summary>The caller is not an authorized Approver for this proposal under the snapshotted Approver Policy + current dependencies (fail closed — FR-7, AD-12).</summary>
    NotAuthorized,
}
