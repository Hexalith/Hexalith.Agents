using System.Collections.Generic;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal request driving a provider-catalog mutation (Story 5.3). Identity, tenant scope, and the
/// catalog-admin decision are all server-derived — never taken from the caller's payload.
/// </summary>
/// <param name="MessageId">The command idempotency key, supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The catalog's tenant scope (also the catalog aggregate id).</param>
/// <param name="ActorUserId">The authenticated actor.</param>
/// <param name="IsProviderAdmin">The trusted catalog-admin decision from claims (fails closed when false).</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved keys are stripped).</param>
public sealed record ProviderCatalogAdministrationRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string ActorUserId,
    bool IsProviderAdmin,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);
