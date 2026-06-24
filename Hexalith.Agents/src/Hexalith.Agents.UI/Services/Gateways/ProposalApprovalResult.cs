namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>Safe UI status for approving and posting a proposal version.</summary>
public enum ProposalApprovalStatus
{
    /// <summary>Unknown status.</summary>
    Unknown = 0,

    /// <summary>The selected version was approved but not posted.</summary>
    Approved,

    /// <summary>The selected version is approved and posting is pending.</summary>
    PostingPending,

    /// <summary>The selected version was posted to the Conversation.</summary>
    Posted,

    /// <summary>Approval was denied.</summary>
    NotAuthorized,

    /// <summary>The proposal is no longer pending/approvable.</summary>
    NotPending,

    /// <summary>Posting failed closed.</summary>
    PostingFailed,

    /// <summary>The service is unavailable or failed closed.</summary>
    Unavailable,
}

/// <summary>Structured result from the proposal approval gateway.</summary>
/// <param name="Status">The safe status.</param>
/// <param name="SelectedVersionId">The selected version id, present only when safe to echo to host code.</param>
/// <param name="MessageId">The posted message id, present only when posted.</param>
public sealed record ProposalApprovalResult(
    ProposalApprovalStatus Status,
    string? SelectedVersionId,
    string? MessageId)
{
    /// <summary>Creates a posting-pending result.</summary>
    public static ProposalApprovalResult PostingPending(string selectedVersionId)
        => new(ProposalApprovalStatus.PostingPending, selectedVersionId, null);

    /// <summary>Creates a posted result.</summary>
    public static ProposalApprovalResult Posted(string selectedVersionId, string messageId)
        => new(ProposalApprovalStatus.Posted, selectedVersionId, messageId);

    /// <summary>Creates a not-authorized result.</summary>
    public static ProposalApprovalResult NotAuthorized()
        => new(ProposalApprovalStatus.NotAuthorized, null, null);

    /// <summary>Creates a not-pending result.</summary>
    public static ProposalApprovalResult NotPending()
        => new(ProposalApprovalStatus.NotPending, null, null);

    /// <summary>Creates a posting-failed result.</summary>
    public static ProposalApprovalResult PostingFailed(string selectedVersionId)
        => new(ProposalApprovalStatus.PostingFailed, selectedVersionId, null);

    /// <summary>Creates an unavailable result.</summary>
    public static ProposalApprovalResult Unavailable()
        => new(ProposalApprovalStatus.Unavailable, null, null);
}
