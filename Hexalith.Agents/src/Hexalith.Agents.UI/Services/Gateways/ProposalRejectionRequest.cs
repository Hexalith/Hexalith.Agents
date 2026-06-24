namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Safe UI request for rejecting a pending proposal (Story 3.6; AC1, AC4). It carries ids only plus an optional
/// policy-defined <see cref="RationaleCode"/> — a safe code/category, NEVER free text and NEVER generated content (AD-14).
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction id.</param>
/// <param name="ProposalId">The proposal id.</param>
/// <param name="RationaleCode">The optional policy-defined safe rejection rationale code/category (<see langword="null"/> when none selected).</param>
/// <param name="ClientCorrelationId">Optional client correlation id.</param>
public sealed record ProposalRejectionRequest(
    string AgentInteractionId,
    string ProposalId,
    string? RationaleCode,
    string? ClientCorrelationId);
