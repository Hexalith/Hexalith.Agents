using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// The structured accepted identity returned when an Agent administration command is accepted (Story 5.2 AC1).
/// It names only public identities — the Agent, the command message, and the correlation used for tracing — plus
/// the truth stage the acceptance has reached. It deliberately exposes no stream name, revision, aggregate type,
/// workflow instance, projection address, or Provider SDK detail.
/// </summary>
/// <param name="AgentId">The Agent the command targeted.</param>
/// <param name="MessageId">The accepted command message identity.</param>
/// <param name="CorrelationId">The correlation identity for tracing the accepted command.</param>
/// <param name="TruthState">The truth stage reached by acceptance — always <see cref="AgentSetupTruthState.Submitted"/>, never a projection claim.</param>
public record AgentCommandAcceptance(
    string AgentId,
    string MessageId,
    string CorrelationId,
    AgentSetupTruthState TruthState);
