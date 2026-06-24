using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe structural reason an approve-proposed-reply command could not be evaluated.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposedReplyNotApprovableReason
{
    /// <summary>Not-a-reason sentinel.</summary>
    Unknown = 0,

    /// <summary>The interaction has no proposal to approve.</summary>
    InteractionNotProposed,

    /// <summary>The proposal is not in an approvable/retryable state.</summary>
    ProposalNotPending,

    /// <summary>A different version has already been approved and cannot be replaced.</summary>
    DifferentVersionAlreadyApproved,
}
