namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>Safe UI request for approving one selected proposal version.</summary>
/// <param name="AgentInteractionId">The AgentInteraction id.</param>
/// <param name="ProposalId">The proposal id.</param>
/// <param name="SelectedVersionId">The exact selected version id.</param>
/// <param name="ClientCorrelationId">Optional client correlation id.</param>
public sealed record ProposalApprovalRequest(
    string AgentInteractionId,
    string ProposalId,
    string SelectedVersionId,
    string? ClientCorrelationId);
