using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side seam for the Provider catalog grid and writes (Story 5.3). The page depends only on this
/// abstraction and the public catalog contracts; it never touches provider SDKs, secret values, or server internals.
/// </summary>
public interface IProviderCatalogGateway
{
    /// <summary>Lists the safe provider/model catalog entry views backing the Provider catalog grid.</summary>
    /// <param name="includeDisabled">When <see langword="true"/>, disabled entries are included for inspection.</param>
    /// <param name="expectedProjectionVersion">The projection version the caller is waiting to see, if any.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed provider-catalog inspection result.</returns>
    Task<ProviderCatalogInspectionResult> ListEntriesAsync(
        bool includeDisabled,
        string? expectedProjectionVersion,
        CancellationToken cancellationToken);

    /// <summary>Gets one authorized provider/model catalog entry.</summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="expectedCapabilityVersion">The capability version the caller is waiting to see, if any.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed provider-catalog inspection result.</returns>
    Task<ProviderCatalogInspectionResult> GetEntryAsync(
        string providerId,
        string modelId,
        int? expectedCapabilityVersion,
        CancellationToken cancellationToken);

    /// <summary>Creates a provider/model catalog entry.</summary>
    /// <param name="command">The create command.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight write.</param>
    /// <returns>The structured write result.</returns>
    Task<ProviderCatalogWriteResult> CreateAsync(CreateProviderModelEntry command, CancellationToken cancellationToken);

    /// <summary>Updates provider/model metadata and pricing.</summary>
    /// <param name="command">The update command.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight write.</param>
    /// <returns>The structured write result.</returns>
    Task<ProviderCatalogWriteResult> UpdateAsync(UpdateProviderModelEntry command, CancellationToken cancellationToken);

    /// <summary>Enables a provider/model catalog entry.</summary>
    /// <param name="command">The enable command.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight write.</param>
    /// <returns>The structured write result.</returns>
    Task<ProviderCatalogWriteResult> EnableAsync(EnableProviderModelEntry command, CancellationToken cancellationToken);

    /// <summary>Disables a provider/model catalog entry.</summary>
    /// <param name="command">The disable command.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight write.</param>
    /// <returns>The structured write result.</returns>
    Task<ProviderCatalogWriteResult> DisableAsync(DisableProviderModelEntry command, CancellationToken cancellationToken);
}
