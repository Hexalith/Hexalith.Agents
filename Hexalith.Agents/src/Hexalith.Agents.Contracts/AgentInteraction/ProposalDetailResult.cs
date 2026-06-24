namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Structured result of an authorized single Proposed-Agent-Reply detail read (AC1, AC2; AD-12, AD-14). On every
/// non-success, non-stale outcome the <see cref="Detail"/> view is <see langword="null"/>, so a denied/faulted/not-found
/// read never reveals whether the proposal exists in another tenant or leaks unrelated records (AD-12, AD-14). It mirrors
/// <see cref="PendingProposalsResult"/>'s fail-closed factory shape, extended with a <see cref="NotFound"/> outcome for a
/// single-record read and a <see cref="Stale"/> outcome that may carry a degraded detail the UI renders behind a stale
/// notice (read models expose freshness/degraded/blocked states — architecture Consistency Conventions; AD-12).
/// </summary>
/// <param name="Status">The read outcome.</param>
/// <param name="Detail">The safe proposal-detail view (non-null only when <see cref="Status"/> is <see cref="ProposalDetailInspectionStatus.Success"/> or <see cref="ProposalDetailInspectionStatus.Stale"/>).</param>
public record ProposalDetailResult(
    ProposalDetailInspectionStatus Status,
    ProposalDetailView? Detail)
{
    /// <summary>Creates a successful fresh read result carrying the given safe detail view.</summary>
    /// <param name="detail">The safe proposal-detail view.</param>
    /// <returns>A success result.</returns>
    public static ProposalDetailResult Success(ProposalDetailView detail)
        => new(ProposalDetailInspectionStatus.Success, detail);

    /// <summary>Creates a fail-closed not-authorized result with no detail (AC1 — discloses nothing; AD-12).</summary>
    /// <returns>A not-authorized result.</returns>
    public static ProposalDetailResult NotAuthorized()
        => new(ProposalDetailInspectionStatus.NotAuthorized, null);

    /// <summary>Creates a read-faulted/dependency-unreachable result with no detail (the UI <c>Unavailable</c> surface).</summary>
    /// <returns>An unavailable result.</returns>
    public static ProposalDetailResult Unavailable()
        => new(ProposalDetailInspectionStatus.Unavailable, null);

    /// <summary>
    /// Creates a degraded/stale result. Degraded data may still carry the detail the UI renders behind a stale notice —
    /// pass the trustworthy detail, or <see langword="null"/> when none is trustworthy (the UI <c>Stale</c> surface).
    /// </summary>
    /// <param name="detail">The degraded detail to render behind a stale notice, or <see langword="null"/> when none is trustworthy.</param>
    /// <returns>A stale result.</returns>
    public static ProposalDetailResult Stale(ProposalDetailView? detail)
        => new(ProposalDetailInspectionStatus.Stale, detail);

    /// <summary>Creates a not-found result with no detail (never reveals cross-tenant existence; AD-12).</summary>
    /// <returns>A not-found result.</returns>
    public static ProposalDetailResult NotFound()
        => new(ProposalDetailInspectionStatus.NotFound, null);
}
