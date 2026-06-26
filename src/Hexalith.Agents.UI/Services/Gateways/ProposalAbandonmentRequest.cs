namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Safe UI request for abandoning a pending proposal (Story 3.6; AC2, AC4). It carries ids only — an abandonment is a
/// terminal transition that carries no rationale and no content (AD-14).
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction id.</param>
/// <param name="ProposalId">The proposal id.</param>
/// <param name="ClientCorrelationId">Optional client correlation id.</param>
public sealed record ProposalAbandonmentRequest(
    string AgentInteractionId,
    string ProposalId,
    string? ClientCorrelationId);
