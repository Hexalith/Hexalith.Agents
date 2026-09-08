using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Serves the live <see cref="GetProviderCatalogEntryQuery"/> from the persisted catalog read model.</summary>
public sealed class GetProviderCatalogEntryQueryHandler(
    IReadModelStore readModelStore,
    IOptions<ProviderCatalogReadModelOptions> options,
    ITenantAccessReader tenantAccessReader)
    : ProviderCatalogQueryHandlerBase(readModelStore, options, tenantAccessReader)
{
    /// <inheritdoc />
    public override string QueryType => GetProviderCatalogEntryQuery.QueryType;

    /// <inheritdoc />
    protected override ProviderCatalogInspectionResult CreateResult(ProviderCatalogReadModel? model, QueryEnvelope query)
    {
        GetProviderCatalogEntryQuery? payload = ReadPayload<GetProviderCatalogEntryQuery>(query.Payload);
        return ProviderCatalogViewFactory.CreateEntry(
            model,
            query.TenantId,
            payload?.ProviderId ?? string.Empty,
            payload?.ModelId ?? string.Empty,
            payload?.ExpectedCapabilityVersion,
            isProviderAdmin: true);
    }
}
