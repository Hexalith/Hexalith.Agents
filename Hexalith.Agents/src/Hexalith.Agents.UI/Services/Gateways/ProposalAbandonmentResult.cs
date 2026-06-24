namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>Safe UI status for abandoning a pending proposal (Story 3.6; AC2, AC4).</summary>
public enum ProposalAbandonmentStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>The proposal was moved to the abandoned terminal state and can never act again; all versions are preserved for audit.</summary>
    Abandoned,

    /// <summary>The caller is not an authorized Approver for this proposal; no abandonment was made (AD-12 fail-closed).</summary>
    NotAuthorized,

    /// <summary>The proposal is no longer pending (terminal), so it cannot be abandoned (AC4 fail-closed).</summary>
    NotPending,

    /// <summary>The abandonment seam faulted / a dependency is unreachable; no abandonment was recorded (the UI error surface).</summary>
    Unavailable,
}

/// <summary>
/// Structured result of submitting an authorized proposal abandonment (Story 3.6; AC2, AC4). It carries ONLY the safe
/// <see cref="Status"/> — never any version id or content (an abandonment is a terminal transition; AD-14). Mirrors
/// <see cref="ProposalRejectionResult"/>.
/// </summary>
/// <param name="Status">The abandonment outcome.</param>
public sealed record ProposalAbandonmentResult(ProposalAbandonmentStatus Status)
{
    /// <summary>Creates an abandoned result.</summary>
    public static ProposalAbandonmentResult Abandoned()
        => new(ProposalAbandonmentStatus.Abandoned);

    /// <summary>Creates a not-authorized result (AD-12 fail-closed).</summary>
    public static ProposalAbandonmentResult NotAuthorized()
        => new(ProposalAbandonmentStatus.NotAuthorized);

    /// <summary>Creates a not-pending result (the terminal-proposal fail-closed surface; AC4).</summary>
    public static ProposalAbandonmentResult NotPending()
        => new(ProposalAbandonmentStatus.NotPending);

    /// <summary>Creates an unavailable result (the UI error surface).</summary>
    public static ProposalAbandonmentResult Unavailable()
        => new(ProposalAbandonmentStatus.Unavailable);
}
