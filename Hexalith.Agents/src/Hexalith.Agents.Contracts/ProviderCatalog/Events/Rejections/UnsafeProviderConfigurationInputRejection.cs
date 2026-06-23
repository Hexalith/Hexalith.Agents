namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>
/// A create/update command supplied a configuration reference that does not look like a safe opaque reference
/// identifier (e.g. it is over-long or contains disallowed characters), which could indicate an attempt to push
/// a raw credential/secret value through the public contract (AC1, AD-14). The command is rejected before
/// mutation; the <paramref name="Reason"/> never echoes the offending value.
/// </summary>
public record UnsafeProviderConfigurationInputRejection(
    string CatalogId,
    string ProviderId,
    string ModelId,
    string Reason) : IRejectionEvent;
