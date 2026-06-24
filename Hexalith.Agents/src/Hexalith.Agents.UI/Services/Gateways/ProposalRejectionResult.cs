namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>Safe UI status for rejecting a pending proposal (Story 3.6; AC1, AC4).</summary>
public enum ProposalRejectionStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>The proposal was moved to the rejected terminal state; all versions are preserved for audit.</summary>
    Rejected,

    /// <summary>The caller is not an authorized Approver for this proposal; no rejection was made (AD-12 fail-closed).</summary>
    NotAuthorized,

    /// <summary>The proposal is no longer pending (terminal), so it cannot be rejected (AC4 fail-closed).</summary>
    NotPending,

    /// <summary>The rejection seam faulted / a dependency is unreachable; no rejection was recorded (the UI error surface).</summary>
    Unavailable,
}

/// <summary>
/// Structured result of submitting an authorized proposal rejection (Story 3.6; AC1, AC4). It carries ONLY the safe
/// <see cref="Status"/> — never any version id or content (a rejection is a terminal transition, not a version; AD-14).
/// Mirrors <see cref="ProposalApprovalResult"/> as a fail-closed structured wrapper.
/// </summary>
/// <param name="Status">The rejection outcome.</param>
public sealed record ProposalRejectionResult(ProposalRejectionStatus Status)
{
    /// <summary>Creates a rejected result.</summary>
    public static ProposalRejectionResult Rejected()
        => new(ProposalRejectionStatus.Rejected);

    /// <summary>Creates a not-authorized result (AD-12 fail-closed).</summary>
    public static ProposalRejectionResult NotAuthorized()
        => new(ProposalRejectionStatus.NotAuthorized);

    /// <summary>Creates a not-pending result (the terminal-proposal fail-closed surface; AC4).</summary>
    public static ProposalRejectionResult NotPending()
        => new(ProposalRejectionStatus.NotPending);

    /// <summary>Creates an unavailable result (the UI error surface).</summary>
    public static ProposalRejectionResult Unavailable()
        => new(ProposalRejectionStatus.Unavailable);
}
