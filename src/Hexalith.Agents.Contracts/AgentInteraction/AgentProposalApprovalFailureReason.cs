using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe reason for an approval failure or an approved-version posting failure.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposalApprovalFailureReason
{
    /// <summary>Not-a-reason sentinel.</summary>
    Unknown = 0,

    /// <summary>The approver policy did not authorize the approval.</summary>
    NotAuthorized,

    /// <summary>The policy basis could not be resolved safely.</summary>
    PolicyFailure,

    /// <summary>The selected version id was missing or empty.</summary>
    SelectedVersionMissing,

    /// <summary>The selected version id did not identify a preserved approvable version.</summary>
    SelectedVersionInvalid,

    /// <summary>The Agent Party identity was unavailable or invalid at posting time.</summary>
    PartyIdentityUnavailable,

    /// <summary>The membership seam was unavailable.</summary>
    MembershipUnavailable,

    /// <summary>Conversations rejected Agent membership.</summary>
    MembershipRejected,

    /// <summary>The source Conversation was unavailable or inaccessible.</summary>
    ConversationUnavailable,

    /// <summary>Conversations rejected the message append.</summary>
    PostRejected,

    /// <summary>A posting or version-read adapter failed without exposing raw details.</summary>
    AdapterFailure,
}
