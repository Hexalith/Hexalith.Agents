using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>Canonical public proposal workflow status terms.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProposalOperationStatus
{
    /// <summary>Absent or unrecognized proposal status sentinel.</summary>
    Unknown = 0,

    /// <summary>A proposal version was generated.</summary>
    Generated,

    /// <summary>The proposal was edited.</summary>
    Edited,

    /// <summary>The proposal was regenerated.</summary>
    Regenerated,

    /// <summary>The proposal is pending approval and is not success.</summary>
    PendingApproval,

    /// <summary>The proposal was approved but not necessarily posted.</summary>
    Approved,

    /// <summary>The proposal was rejected.</summary>
    Rejected,

    /// <summary>The proposal was abandoned.</summary>
    Abandoned,

    /// <summary>The proposal expired.</summary>
    Expired,

    /// <summary>Posting is pending and is not success.</summary>
    PostingPending,

    /// <summary>The approved version was posted.</summary>
    Posted,

    /// <summary>Posting failed.</summary>
    PostingFailed,
}
