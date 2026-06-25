using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>Safe UI status for a launch-readiness read (Story 4.4 AC4; AD-12). Mirrors <see cref="AuditEvidenceInspectionStatus"/>.</summary>
public enum LaunchReadinessInspectionStatus
{
    /// <summary>Unknown sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>The inspection succeeded; the safe launch-readiness view is present.</summary>
    Success,

    /// <summary>The caller is not authorized; no readiness view is returned (AD-12).</summary>
    NotAuthorized,

    /// <summary>A dependency/projection is unreachable; no readiness view is returned (the UI <c>Unavailable</c> surface).</summary>
    Unavailable,

    /// <summary>No Agent exists for the requested aggregate; no readiness view is returned (never reveals cross-tenant existence; AD-12).</summary>
    NotFound,
}

/// <summary>
/// Structured result of an authorized launch-readiness read (Story 4.4 AC4; AD-12, AD-14), modeled on
/// <see cref="Contracts.Operations.AgentOperationalStatusSummaryResult"/>. On every non-success outcome the
/// <see cref="Readiness"/> view is <see langword="null"/>, so a denied/faulted read never reveals whether the Agent
/// exists in another tenant or leaks any readiness data (fail-closed and indistinguishable). The bundled view carries
/// ONLY safe governance descriptors/enums/ints + presence flags + blockers — never a secret, raw payload, or content
/// (AD-14).
/// </summary>
/// <param name="Status">The read outcome.</param>
/// <param name="Readiness">The safe launch-readiness view (non-null only on <see cref="LaunchReadinessInspectionStatus.Success"/>).</param>
public sealed record LaunchReadinessResult(
    LaunchReadinessInspectionStatus Status,
    AgentLaunchReadinessView? Readiness)
{
    /// <summary>Creates a successful read result carrying the given safe launch-readiness view.</summary>
    /// <param name="readiness">The safe launch-readiness view.</param>
    /// <returns>A success result.</returns>
    public static LaunchReadinessResult Success(AgentLaunchReadinessView readiness)
        => new(LaunchReadinessInspectionStatus.Success, readiness);

    /// <summary>Creates a fail-closed not-authorized result with no readiness view (discloses nothing; AD-12).</summary>
    /// <returns>A not-authorized result.</returns>
    public static LaunchReadinessResult NotAuthorized()
        => new(LaunchReadinessInspectionStatus.NotAuthorized, null);

    /// <summary>Creates a dependency-unreachable result with no readiness view (the UI <c>Unavailable</c> surface).</summary>
    /// <returns>An unavailable result.</returns>
    public static LaunchReadinessResult Unavailable()
        => new(LaunchReadinessInspectionStatus.Unavailable, null);

    /// <summary>Creates a not-found result with no readiness view (never reveals cross-tenant existence; AD-12).</summary>
    /// <returns>A not-found result.</returns>
    public static LaunchReadinessResult NotFound()
        => new(LaunchReadinessInspectionStatus.NotFound, null);
}
