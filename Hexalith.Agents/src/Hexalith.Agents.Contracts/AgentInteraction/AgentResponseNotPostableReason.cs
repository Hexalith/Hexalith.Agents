using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> a post-response command could not be evaluated at all (AD-12). Distinct from a
/// recorded posting-failed <em>decision</em>: a not-postable command produces no state change (mirroring the generation's
/// not-generatable rejection), whereas a posting-failed decision is a successfully-recorded fail-closed outcome. Carried
/// only on <see cref="Events.Rejections.AgentResponseNotPostableRejection"/>.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel: an absent/unrecognized reason is treated as "not a
/// concrete reason". Serialized by name so an absent value never resolves to a concrete classification. Mirrors
/// <see cref="AgentOutputNotGeneratableReason"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentResponseNotPostableReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The interaction has no recorded request yet, so there is no call to post a response for.</summary>
    InteractionNotRequested,

    /// <summary>The interaction has not generated output (status is not <c>Generated</c>); posting must never run before generation succeeds.</summary>
    OutputNotGenerated,

    /// <summary>The snapshot response mode is not <c>Automatic</c> (it is <c>Confirmation</c>) — that path posts via Epic 3 approval, not automatic posting.</summary>
    NotAutomaticResponseMode,
}
