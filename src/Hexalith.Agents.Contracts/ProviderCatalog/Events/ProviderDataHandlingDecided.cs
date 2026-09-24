namespace Hexalith.Agents.Contracts.ProviderCatalog.Events;

/// <summary>Durable tenant decision and exact confirmed terms.</summary>
public sealed record ProviderDataHandlingDecided(
    string TenantId,
    string ProviderId,
    string ModelId,
    bool Accepted,
    string Justification,
    int Revision,
    string ActorUserId,
    string RoleBasis,
    ProviderDataHandlingRecord ConfirmedTerms,
    DateTimeOffset DecidedAt) : IEventPayload;
