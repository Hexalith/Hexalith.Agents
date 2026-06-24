using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> a Proposed-Agent-Reply edit failed closed AFTER the proposal was pending
/// (AC2, AC4; AD-5, AD-12, AD-14). Recorded on <see cref="Events.ProposedAgentReplyEditFailed"/> as fail-closed Audit
/// Evidence so an administrator can distinguish the failure class without any edited content, raw provider/Conversations
/// payload, or secret. Prior versions are always preserved on a failed edit.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete reason; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel (mapped to <see cref="AdapterFailure"/> by the pure policy). The reasons are deliberately coarse
/// and content-free. Mirrors <see cref="AgentProposalCreationFailureReason"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposalEditFailureReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>Edit-time authorization did not resolve to an authorized Approver (fail closed) — the trusted verdict was not <c>Valid</c> (AD-12).</summary>
    NotAuthorized,

    /// <summary>The edit adapter failed (it threw or returned a fail-closed adapter outcome) — no provider/Conversations error text is exposed (AD-14).</summary>
    AdapterFailure,
}
