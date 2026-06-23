namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Structured result of an authorized Agent status/configuration inspection (AC3, AC4). On
/// <see cref="AgentInspectionStatus.NotAuthorized"/> or <see cref="AgentInspectionStatus.AgentNotFound"/> the
/// <see cref="Agent"/> view is <see langword="null"/>, so a failed inspection never reveals Agent existence,
/// instructions, or unrelated tenant data.
/// </summary>
/// <param name="Status">The inspection outcome.</param>
/// <param name="Agent">The safe status view (non-null only when <see cref="Status"/> is <see cref="AgentInspectionStatus.Success"/>).</param>
public record AgentInspectionResult(
    AgentInspectionStatus Status,
    AgentStatusView? Agent)
{
    /// <summary>Creates a successful inspection result carrying the given safe status view.</summary>
    /// <param name="agent">The safe status view.</param>
    /// <returns>A success result.</returns>
    public static AgentInspectionResult Success(AgentStatusView agent)
        => new(AgentInspectionStatus.Success, agent);

    /// <summary>Creates a not-authorized result with no Agent data (AC4 fail-closed).</summary>
    /// <returns>A not-authorized result.</returns>
    public static AgentInspectionResult NotAuthorized()
        => new(AgentInspectionStatus.NotAuthorized, null);

    /// <summary>Creates an agent-not-found result with no Agent data.</summary>
    /// <returns>An agent-not-found result.</returns>
    public static AgentInspectionResult NotFound()
        => new(AgentInspectionStatus.AgentNotFound, null);
}
