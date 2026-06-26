using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> a Proposed-Agent-Reply regeneration failed closed AFTER the proposal was pending
/// (AC3, AC4; AD-5, AD-9, AD-12, AD-14). Recorded on <see cref="Events.ProposedAgentReplyRegenerationFailed"/> as
/// fail-closed Audit Evidence so an administrator can distinguish the failure class (authorization/provider/timeout/safety/
/// policy) without any regenerated content, raw provider payload, provider-specific error text, or secret. Prior versions
/// are always preserved and the proposal remains retryable on a failed regeneration.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete reason; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel (mapped to <see cref="AdapterFailure"/> by the pure policy). The provider-class reasons mirror
/// <see cref="AgentOutputGenerationFailureReason"/> (regeneration re-invokes the provider), plus <see cref="NotAuthorized"/>
/// for the fail-closed approver path (mirroring <see cref="AgentProposalEditFailureReason.NotAuthorized"/>). The reasons are
/// deliberately coarse and content-free — they carry no provider SDK detail.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposalRegenerationFailureReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>Regeneration-time authorization did not resolve to an authorized Approver (fail closed) — the trusted verdict was not <c>Valid</c> (AD-12).</summary>
    NotAuthorized,

    /// <summary>The provider invocation exceeded its bounded request timeout.</summary>
    ProviderTimeout,

    /// <summary>The selected Provider/model is disabled or not text-generation capable — regeneration must not proceed.</summary>
    ProviderDisabled,

    /// <summary>The Provider/model entry could not be read (missing, not-found, or read failure) — fail closed.</summary>
    ProviderUnavailable,

    /// <summary>The provider adapter failed (it threw or returned a fail-closed adapter outcome) — no provider error text is exposed (AD-14).</summary>
    AdapterFailure,

    /// <summary>The Source Conversation context could not be re-read fresh enough to build the model input — fail closed.</summary>
    InvalidContext,

    /// <summary>Regenerated content was blocked by Content Safety Policy (paired with the <see cref="AgentInteractionStatus.ProposalRegenerationFailed"/> decision; AD-5, AD-14).</summary>
    ContentSafetyBlocked,

    /// <summary>The effective Content Safety Policy could not be loaded/evaluated — fail closed rather than skip the safety gate.</summary>
    PolicyFailure,
}
