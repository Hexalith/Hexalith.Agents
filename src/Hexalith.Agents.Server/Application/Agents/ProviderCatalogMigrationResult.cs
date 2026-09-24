namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>Safe outcome of a legacy catalog migration batch.</summary>
public sealed record ProviderCatalogMigrationResult(string Status, int PlatformEntries, int TenantEntries);
