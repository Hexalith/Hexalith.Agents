using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side read seam for the Provider catalog grid (AC3; AD-9, AD-15). The Provider catalog page depends only on
/// this abstraction and the public <see cref="ProviderCatalogEntryView"/> contract; it never touches provider SDKs,
/// secret values, or server internals.
/// </summary>
/// <remarks>
/// The live implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the provider-catalog read-model and is
/// deferred to the Agents read-model / BFF story (Epic 4), mirroring the 1.2–1.7 deferred-binding convention. The
/// bUnit tests substitute this seam with NSubstitute; the <see cref="DeferredProviderCatalogGateway"/> placeholder
/// is never exercised in tests but keeps the DI graph complete. The returned views expose only safe normalized
/// capability metadata + configured/not-configured state — never a secret value or the configuration reference
/// (AD-9, AD-14).
/// </remarks>
public interface IProviderCatalogGateway
{
    /// <summary>
    /// Lists the safe provider/model catalog entry views backing the Provider catalog grid. Returns a structured
    /// fail-closed result; <see cref="ProviderCatalogInspectionResult.Entries"/> is empty unless the status is
    /// <see cref="ProviderCatalogInspectionStatus.Success"/>.
    /// </summary>
    /// <param name="includeDisabled">When <see langword="true"/>, disabled entries are included for inspection.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed provider-catalog inspection result.</returns>
    Task<ProviderCatalogInspectionResult> ListEntriesAsync(bool includeDisabled, CancellationToken cancellationToken);
}
