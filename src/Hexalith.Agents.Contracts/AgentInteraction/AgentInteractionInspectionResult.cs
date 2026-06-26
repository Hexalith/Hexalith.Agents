namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Structured result of an authorized Agent Call (<c>AgentInteraction</c>) status inspection (AC2, AC3). On
/// <see cref="AgentInteractionInspectionStatus.NotAuthorized"/> or <see cref="AgentInteractionInspectionStatus.NotFound"/>
/// the <see cref="View"/> is <see langword="null"/>, so a failed inspection never reveals interaction existence or any
/// unrelated tenant data (AD-12, AD-14). Mirrors <see cref="Agent.AgentInspectionResult"/> exactly — the safe handle the
/// feedback UI polls for live call status; it carries no prompt, generated content, stream name, provider detail, or
/// secret.
/// </summary>
/// <param name="Status">The inspection outcome.</param>
/// <param name="View">The safe status view (non-null only when <see cref="Status"/> is <see cref="AgentInteractionInspectionStatus.Success"/>).</param>
public record AgentInteractionInspectionResult(
    AgentInteractionInspectionStatus Status,
    AgentInteractionStatusView? View)
{
    /// <summary>Creates a successful inspection result carrying the given safe status view.</summary>
    /// <param name="view">The safe status view.</param>
    /// <returns>A success result.</returns>
    public static AgentInteractionInspectionResult Success(AgentInteractionStatusView view)
        => new(AgentInteractionInspectionStatus.Success, view);

    /// <summary>Creates a not-authorized result with no status view (AD-12 fail-closed).</summary>
    /// <returns>A not-authorized result.</returns>
    public static AgentInteractionInspectionResult NotAuthorized()
        => new(AgentInteractionInspectionStatus.NotAuthorized, null);

    /// <summary>Creates an interaction-not-found result with no status view.</summary>
    /// <returns>An interaction-not-found result.</returns>
    public static AgentInteractionInspectionResult NotFound()
        => new(AgentInteractionInspectionStatus.NotFound, null);
}
