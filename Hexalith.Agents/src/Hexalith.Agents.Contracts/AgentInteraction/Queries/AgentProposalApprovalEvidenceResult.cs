namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Structured result for safe proposal approval/posting evidence inspection.
/// </summary>
/// <param name="Status">The inspection status.</param>
/// <param name="Evidence">The safe evidence view, present only on success.</param>
public sealed record AgentProposalApprovalEvidenceResult(
    AgentInteractionInspectionStatus Status,
    AgentProposalApprovalEvidenceView? Evidence)
{
    /// <summary>Creates a successful result.</summary>
    public static AgentProposalApprovalEvidenceResult Success(AgentProposalApprovalEvidenceView evidence)
        => new(AgentInteractionInspectionStatus.Success, evidence);

    /// <summary>Creates a not-authorized result.</summary>
    public static AgentProposalApprovalEvidenceResult NotAuthorized()
        => new(AgentInteractionInspectionStatus.NotAuthorized, null);

    /// <summary>Creates a not-found result.</summary>
    public static AgentProposalApprovalEvidenceResult NotFound()
        => new(AgentInteractionInspectionStatus.NotFound, null);
}
