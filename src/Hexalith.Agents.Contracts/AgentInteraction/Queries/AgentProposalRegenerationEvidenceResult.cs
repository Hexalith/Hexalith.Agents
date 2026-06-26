namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Structured result of an authorized Proposed-Agent-Reply regeneration-evidence inspection (AC3, AC4; FR-14, FR-16). On
/// <see cref="AgentInteractionInspectionStatus.NotAuthorized"/> or <see cref="AgentInteractionInspectionStatus.NotFound"/>
/// the <see cref="Evidence"/> view is <see langword="null"/>, so a failed inspection never reveals whether the proposal
/// exists in another tenant or leaks unrelated records (AD-12, AD-14). Mirrors <see cref="AgentProposalEditEvidenceResult"/>.
/// </summary>
/// <param name="Status">The inspection outcome.</param>
/// <param name="Evidence">The safe regeneration-evidence view (non-null only when <see cref="Status"/> is <see cref="AgentInteractionInspectionStatus.Success"/>).</param>
public record AgentProposalRegenerationEvidenceResult(
    AgentInteractionInspectionStatus Status,
    AgentProposalRegenerationEvidenceView? Evidence)
{
    /// <summary>Creates a successful inspection result carrying the given safe evidence view.</summary>
    /// <param name="evidence">The safe regeneration-evidence view.</param>
    /// <returns>A success result.</returns>
    public static AgentProposalRegenerationEvidenceResult Success(AgentProposalRegenerationEvidenceView evidence)
        => new(AgentInteractionInspectionStatus.Success, evidence);

    /// <summary>Creates a not-authorized result with no evidence (AC4 fail-closed).</summary>
    /// <returns>A not-authorized result.</returns>
    public static AgentProposalRegenerationEvidenceResult NotAuthorized()
        => new(AgentInteractionInspectionStatus.NotAuthorized, null);

    /// <summary>Creates a not-found result with no evidence (never reveals cross-tenant existence).</summary>
    /// <returns>A not-found result.</returns>
    public static AgentProposalRegenerationEvidenceResult NotFound()
        => new(AgentInteractionInspectionStatus.NotFound, null);
}
