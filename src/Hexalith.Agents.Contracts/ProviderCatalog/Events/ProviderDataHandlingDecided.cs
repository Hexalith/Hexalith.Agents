namespace Hexalith.Agents.Contracts.ProviderCatalog.Events;

/// <summary>Durable tenant decision and exact confirmed terms.</summary>
/// <param name="TenantId">The deciding tenant.</param>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="Accepted">Whether terms were accepted.</param>
/// <param name="Justification">The administrator's explanation.</param>
/// <param name="Revision">The resulting tenant stream revision.</param>
/// <param name="ActorUserId">The administrator actor.</param>
/// <param name="RoleBasis">The recorded authority basis.</param>
/// <param name="ConfirmedTerms">The exact terms decided.</param>
/// <param name="DecidedAt">The trusted decision time.</param>
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
