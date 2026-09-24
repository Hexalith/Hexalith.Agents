namespace Hexalith.Agents.Contracts.ProviderCatalog.Events;

/// <summary>Records a platform-owned tenant visibility decision.</summary>
/// <param name="TenantId">The target tenant.</param>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="Enabled">Whether the tenant can see the entry.</param>
/// <param name="Revision">The resulting tenant stream revision.</param>
/// <param name="ActorUserId">The Platform Operator actor.</param>
/// <param name="MigratedFrom">Optional migration provenance.</param>
public sealed record TenantProviderModelEnablementSet(
    string TenantId,
    string ProviderId,
    string ModelId,
    bool Enabled,
    int Revision,
    string ActorUserId,
    string? MigratedFrom) : IEventPayload;
