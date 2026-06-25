namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Structured result of an authorized operational-status summary read (Story 4.3 AC1, AC4; AD-12, AD-14). On every
/// non-success, non-stale outcome the <see cref="Summary"/> is <see langword="null"/>, so a denied/faulted read never
/// fingerprints other tenants' records via counts, rates, or readiness blockers (AC4). It mirrors
/// <see cref="AgentInteraction.PendingProposalsResult"/>'s fail-closed factory shape (<c>Success</c>/<c>NotAuthorized</c>/
/// <c>Unavailable</c>/<c>Stale</c>).
/// </summary>
/// <param name="Status">The read outcome.</param>
/// <param name="Summary">The safe operational-status summary (non-null only when <see cref="Status"/> is <see cref="OperationalStatusInspectionStatus.Success"/> or <see cref="OperationalStatusInspectionStatus.Stale"/>).</param>
public sealed record AgentOperationalStatusSummaryResult(
    OperationalStatusInspectionStatus Status,
    AgentOperationalStatusSummaryView? Summary)
{
    /// <summary>Creates a successful fresh read result carrying the given safe summary.</summary>
    /// <param name="summary">The safe operational-status summary.</param>
    /// <returns>A success result.</returns>
    public static AgentOperationalStatusSummaryResult Success(AgentOperationalStatusSummaryView summary)
        => new(OperationalStatusInspectionStatus.Success, summary);

    /// <summary>Creates a fail-closed not-authorized result with no summary (discloses nothing; AD-12).</summary>
    /// <returns>A not-authorized result.</returns>
    public static AgentOperationalStatusSummaryResult NotAuthorized()
        => new(OperationalStatusInspectionStatus.NotAuthorized, null);

    /// <summary>Creates a read-faulted/dependency-unreachable result with no summary (the UI <c>Unavailable</c> surface).</summary>
    /// <returns>An unavailable result.</returns>
    public static AgentOperationalStatusSummaryResult Unavailable()
        => new(OperationalStatusInspectionStatus.Unavailable, null);

    /// <summary>
    /// Creates a degraded/stale result. Degraded data may still carry a summary the UI renders behind a stale/degraded
    /// notice — pass the trustworthy summary, or <see langword="null"/> when none is trustworthy (never rendered as fresh).
    /// </summary>
    /// <param name="summary">The degraded summary to render behind a stale notice, or <see langword="null"/> when none is trustworthy.</param>
    /// <returns>A stale result.</returns>
    public static AgentOperationalStatusSummaryResult Stale(AgentOperationalStatusSummaryView? summary)
        => new(OperationalStatusInspectionStatus.Stale, summary);
}
