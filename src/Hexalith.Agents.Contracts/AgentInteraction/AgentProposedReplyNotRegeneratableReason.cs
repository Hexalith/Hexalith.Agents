using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> a regenerate-proposed-reply command could not be evaluated at all (AC3, AC4;
/// AD-12). Distinct from a recorded regeneration-failed <em>decision</em>: a not-regeneratable command produces no state
/// change and no new version (mirroring the edit's not-editable rejection), whereas a regeneration-failed decision is a
/// successfully-recorded fail-closed outcome that keeps the proposal retryable. Carried on
/// <see cref="Events.Rejections.ProposedAgentReplyNotRegeneratableRejection"/> and on the server regeneration outcome's
/// no-dispatch denial.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel. Serialized by name so an absent value never resolves to
/// a concrete classification. <see cref="InteractionNotProposed"/> and <see cref="ProposalNotPending"/> are the
/// aggregate's structural rejections (AC4 terminal proposals never invoke the provider); <see cref="NotAuthorized"/> is the
/// orchestrator's fail-closed no-dispatch denial when regeneration-time authorization does not resolve to an authorized
/// Approver (FR-16; AD-12). Mirrors <see cref="AgentProposedReplyNotEditableReason"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposedReplyNotRegeneratableReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The interaction has no pending proposal to regenerate (no proposal was created, or the stream has no recorded request).</summary>
    InteractionNotProposed,

    /// <summary>The proposal has reached a terminal/non-pending state (approved, rejected, abandoned, expired, posted) and can no longer be regenerated — no provider invocation occurs (AC4).</summary>
    ProposalNotPending,

    /// <summary>The caller is not an authorized Approver for this proposal under the snapshotted Approver Policy + current dependencies (fail closed — FR-16, AD-12).</summary>
    NotAuthorized,
}
