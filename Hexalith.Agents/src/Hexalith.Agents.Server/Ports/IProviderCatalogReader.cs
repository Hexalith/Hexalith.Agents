using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the Agent selection / activation re-validation orchestration uses to read a single
/// governed provider/model catalog entry (Story 1.5; AD-3, AD-9, AD-12). The implementation reads the in-module
/// <c>ProviderCatalog</c> through the pure <c>ProviderCatalogInspection.GetEntry</c> over rehydrated catalog state
/// and maps the outcome to a safe <see cref="ProviderCatalogEntryReadResult"/>; it MUST never surface a secret
/// value, raw configuration secret, or provider SDK type back across this boundary (AD-9, AD-14).
/// </summary>
/// <remarks>
/// Keeping this a port (rather than reading the catalog inline) preserves the AD-3 round-trip: the pure Agent
/// aggregate receives the resulting provider-readiness verdict through a trusted command extension, never by
/// reading the catalog itself. The live binding to the rehydrated <c>ProviderCatalogState</c> read-model is
/// deferred (mirroring Story 1.2/1.4) so the orchestration's decision logic stays fully unit-testable here.
/// </remarks>
public interface IProviderCatalogReader
{
    /// <summary>Reads a single provider/model catalog entry and maps its state to a fail-closed read result (AC2, AC4).</summary>
    /// <param name="tenantId">The Agent's tenant scope (the catalog is tenant-scoped — cross-tenant reads fail closed as <c>NotAuthorized</c>).</param>
    /// <param name="providerId">The stable provider identifier to read.</param>
    /// <param name="modelId">The stable model identifier to read.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe catalog read result (status + optional safe entry view).</returns>
    Task<ProviderCatalogEntryReadResult> GetEntryAsync(string tenantId, string providerId, string modelId, CancellationToken ct);
}
