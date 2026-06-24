using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> a create-proposed-reply command could not be evaluated at all (AD-12). Distinct
/// from a recorded creation-failed <em>decision</em>: a not-creatable command produces no state change (mirroring the
/// posting's not-postable rejection), whereas a creation-failed decision is a successfully-recorded fail-closed outcome.
/// Carried only on <see cref="Events.Rejections.ProposedAgentReplyNotCreatableRejection"/>.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel: an absent/unrecognized reason is treated as "not a
/// concrete reason". Serialized by name so an absent value never resolves to a concrete classification. Mirrors
/// <see cref="AgentResponseNotPostableReason"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposedReplyNotCreatableReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The interaction has no recorded request yet, so there is no call to create a proposal for.</summary>
    InteractionNotRequested,

    /// <summary>The snapshot response mode is not <c>Confirmation</c> (it is <c>Automatic</c>) — that path posts via Story 2.5, not proposal creation.</summary>
    NotConfirmationResponseMode,

    /// <summary>The interaction has not generated output (status is not <c>Generated</c>); proposal creation must never run before generation succeeds (AC3 — nothing approvable to propose).</summary>
    OutputNotGenerated,
}
