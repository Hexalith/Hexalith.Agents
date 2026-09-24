namespace Hexalith.Agents.Contracts.ProviderCatalog.Events;

/// <summary>Records a platform-owned tenant visibility decision.</summary>
public sealed record TenantProviderModelEnablementSet(
    string TenantId,
    string ProviderId,
    string ModelId,
    bool Enabled,
    int Revision,
    string ActorUserId,
    string? MigratedFrom) : IEventPayload;
