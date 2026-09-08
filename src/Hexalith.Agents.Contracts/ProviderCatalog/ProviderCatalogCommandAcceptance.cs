using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>
/// The structured accepted identity returned when a provider-catalog command is accepted (Story 5.3).
/// It names only public identities — the provider, the model, the command message, and the correlation — plus
/// the truth stage the acceptance has reached. It deliberately exposes no stream name, revision, aggregate type,
/// workflow instance, projection address, secret value, or Provider SDK detail.
/// </summary>
/// <param name="ProviderId">The provider the command targeted.</param>
/// <param name="ModelId">The model the command targeted.</param>
/// <param name="MessageId">The accepted command message identity.</param>
/// <param name="CorrelationId">The correlation identity for tracing the accepted command.</param>
/// <param name="TruthState">The truth stage reached by acceptance — always <see cref="AgentSetupTruthState.Submitted"/>, never a projection claim.</param>
public record ProviderCatalogCommandAcceptance(
    string ProviderId,
    string ModelId,
    string MessageId,
    string CorrelationId,
    AgentSetupTruthState TruthState);
