using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Deferred placeholder for <see cref="IProviderCatalogGateway"/> that keeps the DI graph complete and the UI
/// project buildable before the live provider-catalog path is wired. Returns the fail-closed
/// <see cref="ProviderCatalogInspectionResult.NotAuthorized"/> result, so a host that has not yet bound the real
/// path renders the permission-denied surface rather than an empty success (AD-12).
/// </summary>
public sealed class DeferredProviderCatalogGateway : IProviderCatalogGateway
{
    /// <inheritdoc />
    public Task<ProviderCatalogInspectionResult> ListEntriesAsync(
        bool includeDisabled,
        string? expectedProjectionVersion,
        CancellationToken cancellationToken)
        => Task.FromResult(ProviderCatalogInspectionResult.NotAuthorized());

    /// <inheritdoc />
    public Task<ProviderCatalogInspectionResult> GetEntryAsync(
        string providerId,
        string modelId,
        int? expectedCapabilityVersion,
        CancellationToken cancellationToken)
        => Task.FromResult(ProviderCatalogInspectionResult.NotAuthorized());

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> CreateAsync(CreateProviderModelEntry command, CancellationToken cancellationToken)
        => Denied(command);

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> UpdateAsync(UpdateProviderModelEntry command, CancellationToken cancellationToken)
        => Denied(command);

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> EnableAsync(EnableProviderModelEntry command, CancellationToken cancellationToken)
        => Denied(command);

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> DisableAsync(DisableProviderModelEntry command, CancellationToken cancellationToken)
        => Denied(command);

    private static Task<ProviderCatalogWriteResult> Denied(object command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.FromResult(ProviderCatalogWriteResult.Failed(AgentSetupWriteStatus.NotAuthorized));
    }
}
