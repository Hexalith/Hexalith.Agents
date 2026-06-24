using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Structured result of an authorized pending-proposal queue read (AC1, AC3, AC4). It mirrors
/// <see cref="ProviderCatalog.ProviderCatalogInspectionResult"/>: on every non-success outcome the
/// <see cref="Proposals"/> list is empty and <see cref="PendingCount"/> is zero, so a failed/denied read never
/// fingerprints other tenants' records via counts, empty states, filters, accessible names, or error details (AC4).
/// </summary>
/// <param name="Status">The read outcome.</param>
/// <param name="Proposals">Safe proposal views (empty unless <see cref="Status"/> is <see cref="PendingProposalsInspectionStatus.Success"/> or <see cref="PendingProposalsInspectionStatus.Stale"/>).</param>
/// <param name="PendingCount">
/// The AC3 count indication — the authorized count of proposals needing the current Approver's action. It is
/// <c>0</c> on every non-<see cref="PendingProposalsInspectionStatus.Success"/>/<see cref="PendingProposalsInspectionStatus.Stale"/> outcome.
/// </param>
public record PendingProposalsResult(
    PendingProposalsInspectionStatus Status,
    IReadOnlyList<PendingProposalView> Proposals,
    int PendingCount)
{
    /// <summary>Creates a successful read result carrying the given safe views and the authorized "needs my action" count.</summary>
    public static PendingProposalsResult Success(IReadOnlyList<PendingProposalView> proposals, int pendingCount)
        => new(PendingProposalsInspectionStatus.Success, proposals, pendingCount);

    /// <summary>Creates a fail-closed not-authorized result with no records and a zero count (AC4 — discloses nothing).</summary>
    public static PendingProposalsResult NotAuthorized()
        => new(PendingProposalsInspectionStatus.NotAuthorized, [], 0);

    /// <summary>Creates a read-faulted/dependency-unreachable result with no records and a zero count (the UI <c>Error</c> surface).</summary>
    public static PendingProposalsResult Unavailable()
        => new(PendingProposalsInspectionStatus.Unavailable, [], 0);

    /// <summary>
    /// Creates a degraded/stale result. Degraded data may still carry rows the UI renders behind a stale notice — pass
    /// the trustworthy rows + their count, or <c>[]</c>/<c>0</c> when none are trustworthy (the UI <c>Stale</c> surface).
    /// </summary>
    public static PendingProposalsResult Stale(IReadOnlyList<PendingProposalView> proposals, int pendingCount)
        => new(PendingProposalsInspectionStatus.Stale, proposals, pendingCount);
}
