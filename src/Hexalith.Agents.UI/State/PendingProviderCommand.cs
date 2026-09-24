using Hexalith.Agents.Contracts.ProviderCatalog;

namespace Hexalith.Agents.UI.State;

/// <summary>Safe command identity retained for one browser session while exact truth is pending.</summary>
/// <param name="Family">The governance command family.</param>
/// <param name="ResourceKey">The resource whose next write is locked.</param>
/// <param name="TenantId">The command's owning tenant.</param>
/// <param name="Acceptance">The submitted command identity.</param>
/// <param name="ExpectedCapabilityVersion">The expected platform capability version, if relevant.</param>
/// <param name="ExpectedStatus">The expected platform lifecycle status, if relevant.</param>
public sealed record PendingProviderCommand(
    string Family,
    string ResourceKey,
    string TenantId,
    ProviderCatalogCommandAcceptance Acceptance,
    int? ExpectedCapabilityVersion = null,
    ProviderModelStatus? ExpectedStatus = null);
