using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Deferred placeholder for <see cref="IProviderCatalogGateway"/> that keeps the DI graph complete and the UI
/// project buildable before the live provider-catalog read path is wired. Returns the fail-closed
/// <see cref="ProviderCatalogInspectionResult.NotAuthorized"/> result, so a host that has not yet bound the real
/// read path renders the permission-denied surface rather than an empty "no providers configured" success
/// (AD-12).
/// </summary>
/// <remarks>
/// The real implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the provider-catalog read-model and
/// lands with the dedicated Agents read-model / BFF story (Epic 4). The bUnit tests substitute the gateway with
/// NSubstitute, so this placeholder is never exercised in tests.
/// </remarks>
public sealed class DeferredProviderCatalogGateway : IProviderCatalogGateway
{
    /// <inheritdoc />
    public Task<ProviderCatalogInspectionResult> ListEntriesAsync(bool includeDisabled, CancellationToken cancellationToken)
        => Task.FromResult(ProviderCatalogInspectionResult.NotAuthorized());
}
