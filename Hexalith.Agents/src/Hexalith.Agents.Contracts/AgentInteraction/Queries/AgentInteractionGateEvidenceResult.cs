namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Structured result of an authorized invocation gate evidence inspection (AC3, AC4; FR-24). On
/// <see cref="AgentInteractionGateInspectionStatus.NotAuthorized"/> or
/// <see cref="AgentInteractionGateInspectionStatus.NotFound"/> the <see cref="Evidence"/> view is
/// <see langword="null"/>, so a failed inspection never reveals whether the interaction exists in another tenant or
/// leaks unrelated records (AC3; AD-12). Mirrors <see cref="Agent.AgentInspectionResult"/>.
/// </summary>
/// <param name="Status">The inspection outcome.</param>
/// <param name="Evidence">The safe evidence view (non-null only when <see cref="Status"/> is <see cref="AgentInteractionGateInspectionStatus.Success"/>).</param>
public record AgentInteractionGateEvidenceResult(
    AgentInteractionGateInspectionStatus Status,
    AgentInteractionGateEvidenceView? Evidence)
{
    /// <summary>Creates a successful inspection result carrying the given safe evidence view.</summary>
    /// <param name="evidence">The safe evidence view.</param>
    /// <returns>A success result.</returns>
    public static AgentInteractionGateEvidenceResult Success(AgentInteractionGateEvidenceView evidence)
        => new(AgentInteractionGateInspectionStatus.Success, evidence);

    /// <summary>Creates a not-authorized result with no evidence (AC3, AC4 fail-closed).</summary>
    /// <returns>A not-authorized result.</returns>
    public static AgentInteractionGateEvidenceResult NotAuthorized()
        => new(AgentInteractionGateInspectionStatus.NotAuthorized, null);

    /// <summary>Creates a not-found result with no evidence (never reveals cross-tenant existence; AC3).</summary>
    /// <returns>A not-found result.</returns>
    public static AgentInteractionGateEvidenceResult NotFound()
        => new(AgentInteractionGateInspectionStatus.NotFound, null);
}
