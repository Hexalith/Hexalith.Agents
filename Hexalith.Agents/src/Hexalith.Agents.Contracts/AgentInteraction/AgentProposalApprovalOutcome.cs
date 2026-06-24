using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Server-assembled outcome of approving and posting one selected Proposed Agent Reply version.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposalApprovalOutcome
{
    /// <summary>Not-an-outcome sentinel; fails closed.</summary>
    Unknown = 0,

    /// <summary>Approval was recorded and posting is pending.</summary>
    Approved,

    /// <summary>The approved version was posted to Conversations.</summary>
    Posted,

    /// <summary>A retry observed the same approved version still pending.</summary>
    IdempotentPostingPending,

    /// <summary>A retry observed the same approved version already posted.</summary>
    IdempotentPosted,

    /// <summary>The approver policy did not authorize the action.</summary>
    NotAuthorized,

    /// <summary>The policy basis could not be resolved safely.</summary>
    PolicyFailure,

    /// <summary>The selected version id was missing or empty.</summary>
    SelectedVersionMissing,

    /// <summary>The selected version id did not identify a preserved generated/edited/regenerated version.</summary>
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
