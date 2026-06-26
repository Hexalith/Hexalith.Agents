namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Structured result of an authorized Agent launch-readiness inspection (Story 4.4 AC4). On
/// <see cref="AgentInspectionStatus.NotAuthorized"/> or <see cref="AgentInspectionStatus.AgentNotFound"/> the
/// <see cref="Readiness"/> view is <see langword="null"/>, so a failed inspection never reveals Agent existence or
/// readiness data (fail-closed and indistinguishable; AD-12). Mirrors <see cref="AgentInspectionResult"/>.
/// </summary>
/// <param name="Status">The inspection outcome.</param>
/// <param name="Readiness">The safe launch-readiness view (non-null only when <see cref="Status"/> is <see cref="AgentInspectionStatus.Success"/>).</param>
public record AgentLaunchReadinessInspectionResult(
    AgentInspectionStatus Status,
    AgentLaunchReadinessView? Readiness)
{
    /// <summary>Creates a successful inspection result carrying the given safe launch-readiness view.</summary>
    /// <param name="readiness">The safe launch-readiness view.</param>
    /// <returns>A success result.</returns>
    public static AgentLaunchReadinessInspectionResult Success(AgentLaunchReadinessView readiness)
        => new(AgentInspectionStatus.Success, readiness);

    /// <summary>Creates a not-authorized result with no readiness data (AC4 fail-closed).</summary>
    /// <returns>A not-authorized result.</returns>
    public static AgentLaunchReadinessInspectionResult NotAuthorized()
        => new(AgentInspectionStatus.NotAuthorized, null);

    /// <summary>Creates an agent-not-found result with no readiness data (indistinguishable from not-authorized; AC4).</summary>
    /// <returns>An agent-not-found result.</returns>
    public static AgentLaunchReadinessInspectionResult NotFound()
        => new(AgentInspectionStatus.AgentNotFound, null);
}
