using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> an edit-proposed-reply command could not be evaluated at all (AC2; AD-12).
/// Distinct from a recorded edit-failed <em>decision</em>: a not-editable command produces no state change and no new
/// version (mirroring the creation's not-creatable rejection), whereas an edit-failed decision is a successfully-recorded
/// fail-closed outcome. Carried on <see cref="Events.Rejections.ProposedAgentReplyNotEditableRejection"/> and on the
/// server edit outcome's no-dispatch denial.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel. Serialized by name so an absent value never resolves to
/// a concrete classification. <see cref="InteractionNotProposed"/> and <see cref="ProposalNotPending"/> are the
/// aggregate's structural rejections (AC2 terminal/non-pending states); <see cref="NotAuthorized"/> is the orchestrator's
/// fail-closed no-dispatch denial when edit-time authorization does not resolve to an authorized Approver (FR-15; AD-12).
/// Mirrors <see cref="AgentProposedReplyNotCreatableReason"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposedReplyNotEditableReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The interaction has no pending proposal to edit (no proposal was created, or the stream has no recorded request).</summary>
    InteractionNotProposed,

    /// <summary>The proposal has reached a terminal/non-pending state (approved, rejected, abandoned, expired, posted) and can no longer be edited (AC2).</summary>
    ProposalNotPending,

    /// <summary>The caller is not an authorized Approver for this proposal under the snapshotted Approver Policy + current dependencies (fail closed — FR-15, AD-12).</summary>
    NotAuthorized,
}
