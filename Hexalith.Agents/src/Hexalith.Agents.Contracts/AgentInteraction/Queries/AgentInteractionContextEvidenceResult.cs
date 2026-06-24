namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Structured result of an authorized Conversation context evidence inspection (AC3, AC4; FR-25). On
/// <see cref="AgentInteractionContextInspectionStatus.NotAuthorized"/> or
/// <see cref="AgentInteractionContextInspectionStatus.NotFound"/> the <see cref="Evidence"/> view is
/// <see langword="null"/>, so a failed inspection never reveals whether the interaction exists in another tenant or
/// leaks unrelated records (AC1; AD-12). Mirrors <see cref="AgentInteractionGateEvidenceResult"/>.
/// </summary>
/// <param name="Status">The inspection outcome.</param>
/// <param name="Evidence">The safe evidence view (non-null only when <see cref="Status"/> is <see cref="AgentInteractionContextInspectionStatus.Success"/>).</param>
public record AgentInteractionContextEvidenceResult(
    AgentInteractionContextInspectionStatus Status,
    AgentInteractionContextEvidenceView? Evidence)
{
    /// <summary>Creates a successful inspection result carrying the given safe evidence view.</summary>
    /// <param name="evidence">The safe evidence view.</param>
    /// <returns>A success result.</returns>
    public static AgentInteractionContextEvidenceResult Success(AgentInteractionContextEvidenceView evidence)
        => new(AgentInteractionContextInspectionStatus.Success, evidence);

    /// <summary>Creates a not-authorized result with no evidence (AC1, AC4 fail-closed).</summary>
    /// <returns>A not-authorized result.</returns>
    public static AgentInteractionContextEvidenceResult NotAuthorized()
        => new(AgentInteractionContextInspectionStatus.NotAuthorized, null);

    /// <summary>Creates a not-found result with no evidence (never reveals cross-tenant existence; AC1).</summary>
    /// <returns>A not-found result.</returns>
    public static AgentInteractionContextEvidenceResult NotFound()
        => new(AgentInteractionContextInspectionStatus.NotFound, null);
}
