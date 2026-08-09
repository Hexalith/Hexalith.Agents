namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Structured result of an authorized Agent setup read (Story 5.2 AC2, AC4). Mirrors
/// <see cref="AgentInspectionResult"/>: on any non-success outcome <see cref="Setup"/> is
/// <see langword="null"/>, so a denied or cross-tenant read reveals no Agent existence, instructions, or versions.
/// </summary>
/// <param name="Status">The read outcome.</param>
/// <param name="Setup">The authoritative setup view (non-null only on <see cref="AgentInspectionStatus.Success"/>).</param>
public record AgentSetupResult(
    AgentInspectionStatus Status,
    AgentSetupView? Setup)
{
    /// <summary>Creates a successful setup read carrying the authoritative view.</summary>
    /// <param name="setup">The authoritative setup view.</param>
    /// <returns>A success result.</returns>
    public static AgentSetupResult Success(AgentSetupView setup)
        => new(AgentInspectionStatus.Success, setup);

    /// <summary>Creates a not-authorized result with no setup data (fail-closed; AC4).</summary>
    /// <returns>A not-authorized result.</returns>
    public static AgentSetupResult NotAuthorized()
        => new(AgentInspectionStatus.NotAuthorized, null);

    /// <summary>Creates an agent-not-found result with no setup data.</summary>
    /// <returns>An agent-not-found result.</returns>
    public static AgentSetupResult NotFound()
        => new(AgentInspectionStatus.AgentNotFound, null);

    /// <summary>
    /// Creates an unavailable result with no setup data, for a read that could not reach its backing surface.
    /// Distinct from <see cref="NotAuthorized"/> and <see cref="NotFound"/> so a transport failure is never
    /// rendered as a denial or as a missing Agent.
    /// </summary>
    /// <returns>An unavailable result.</returns>
    public static AgentSetupResult Unavailable()
        => new(AgentInspectionStatus.Unavailable, null);
}
