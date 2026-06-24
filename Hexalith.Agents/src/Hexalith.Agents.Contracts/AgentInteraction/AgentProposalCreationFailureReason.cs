using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> Proposed-Agent-Reply creation failed closed AFTER a successful, safety-passing
/// generation (AC1, AC3, AC4; AD-5, AD-12, AD-14). Recorded on <see cref="Events.ProposedAgentReplyCreationFailed"/> as
/// fail-closed Audit Evidence so an administrator can distinguish the failure class without any generated content, raw
/// provider/Conversations payload, or secret.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete reason; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel. The reasons are deliberately coarse and content-free — they carry no provider/Conversations detail.
/// Mirrors <see cref="AgentResponsePostingFailureReason"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposalCreationFailureReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The selected generated version could not be read (missing, not-found, or read failure) — proposal creation must not proceed (AD-12).</summary>
    GeneratedVersionUnavailable,

    /// <summary>The creation adapter failed (it threw or returned a fail-closed adapter outcome) — no provider/Conversations error text is exposed (AD-14).</summary>
    AdapterFailure,
}
