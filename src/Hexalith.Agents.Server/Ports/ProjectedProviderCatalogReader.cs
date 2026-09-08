using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Live <see cref="IProviderCatalogReader"/> over the projected provider-catalog read model (Story 5.3).
/// Cross-tenant reads fail closed as <see cref="ProviderCatalogInspectionStatus.NotAuthorized"/> before the
/// store is addressed when the HTTP caller belongs to a different tenant.
/// </summary>
public sealed class ProjectedProviderCatalogReader(
    IReadModelStore readModelStore,
    IOptions<ProviderCatalogReadModelOptions> options,
    IAgentAdministrationContextProvider contextProvider) : IProviderCatalogReader
{
    private readonly IReadModelStore _readModelStore = readModelStore
        ?? throw new ArgumentNullException(nameof(readModelStore));

    private readonly IOptions<ProviderCatalogReadModelOptions> _options = options
        ?? throw new ArgumentNullException(nameof(options));

    private readonly IAgentAdministrationContextProvider _contextProvider = contextProvider
        ?? throw new ArgumentNullException(nameof(contextProvider));

    /// <inheritdoc />
    public async Task<ProviderCatalogEntryReadResult> GetEntryAsync(
        string tenantId,
        string providerId,
        string modelId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        AgentAdministrationContext context = _contextProvider.GetContext();
        if (!context.IsAuthorized)
        {
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.NotAuthorized, null);
        }

        if (!string.Equals(context.TenantId, tenantId, StringComparison.Ordinal))
        {
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.NotAuthorized, null);
        }

        ReadModelEntry<ProviderCatalogReadModel> entry;
        try
        {
            entry = await _readModelStore
                .GetAsync<ProviderCatalogReadModel>(
                    _options.Value.StateStoreName,
                    ProviderCatalogReadModelAddresses.Detail(tenantId),
                    ct)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A degraded store read is not a denial: the verdict maps a successful-shape empty entry to Unavailable.
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Unavailable, null);
        }

        ProviderCatalogInspectionResult result = ProviderCatalogViewFactory.CreateEntry(
            entry.Value,
            tenantId,
            providerId,
            modelId,
            expectedCapabilityVersion: null,
            isProviderAdmin: true);

        return result.Status switch
        {
            ProviderCatalogInspectionStatus.Success when result.Entries.Count == 1
                => new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, result.Entries[0]),
            ProviderCatalogInspectionStatus.EntryNotFound
                => new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null),
            ProviderCatalogInspectionStatus.NotAuthorized
                => new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.NotAuthorized, null),
            ProviderCatalogInspectionStatus.Unavailable
                => new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Unavailable, null),
            _ => new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Unavailable, null),
        };
    }
}
