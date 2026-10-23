using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>Canonical public proposal workflow status terms.</summary>
[JsonConverter(typeof(UnknownFallbackEnumConverter<ProposalOperationStatus>))]
public enum ProposalOperationStatus
{
    /// <summary>Absent or unrecognized proposal status sentinel.</summary>
    Unknown = 0,

    /// <summary>A proposal version was generated.</summary>
    Generated = 1,

    /// <summary>The proposal was edited.</summary>
    Edited = 2,

    /// <summary>The proposal was regenerated.</summary>
    Regenerated = 3,

    /// <summary>The proposal is pending approval and is not success.</summary>
    PendingApproval = 4,

    /// <summary>The proposal was approved but not necessarily posted.</summary>
    Approved = 5,

    /// <summary>The proposal was rejected.</summary>
    Rejected = 6,

    /// <summary>The proposal was abandoned.</summary>
    Abandoned = 7,

    /// <summary>The proposal expired.</summary>
    Expired = 8,

    /// <summary>Posting is pending and is not success.</summary>
    PostingPending = 9,

    /// <summary>The approved version was posted.</summary>
    Posted = 10,

    /// <summary>Posting failed.</summary>
    PostingFailed = 11,
}
