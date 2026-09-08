using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Application.Queries;

/// <summary>Serves the live <see cref="ListProviderCatalogEntriesQuery"/> from the persisted catalog read model.</summary>
public sealed class ListProviderCatalogEntriesQueryHandler(
    IReadModelStore readModelStore,
    IOptions<ProviderCatalogReadModelOptions> options,
    ITenantAccessReader tenantAccessReader)
    : ProviderCatalogQueryHandlerBase(readModelStore, options, tenantAccessReader)
{
    /// <inheritdoc />
    public override string QueryType => ListProviderCatalogEntriesQuery.QueryType;

    /// <inheritdoc />
    protected override ProviderCatalogInspectionResult CreateResult(ProviderCatalogReadModel? model, QueryEnvelope query)
    {
        ListProviderCatalogEntriesQuery? payload = ReadPayload<ListProviderCatalogEntriesQuery>(query.Payload);
        return ProviderCatalogViewFactory.CreateList(
            model,
            query.TenantId,
            payload?.IncludeDisabled ?? false,
            payload?.ExpectedProjectionVersion,
            isProviderAdmin: true);
    }
}
