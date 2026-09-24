namespace Hexalith.Agents.Server.Api;

/// <summary>The explicit legacy tenant inventory submitted by a Platform Operator for migration.</summary>
/// <param name="LegacyTenantIds">The legacy tenant identifiers to migrate.</param>
public sealed record ProviderCatalogMigrationRequest(IReadOnlyCollection<string>? LegacyTenantIds);
