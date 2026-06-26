using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IProviderCatalogReader"/> registered so the Server DI graph is complete and compiles
/// cleanly, while the live binding to the rehydrated <c>ProviderCatalogState</c> read-model (via
/// <c>ProviderCatalogInspection.GetEntry</c> over the EventStore read path) is deferred to the dedicated read-model
/// story (mirroring Story 1.2 deferring its DAPR read-path binding and Story 1.4 deferring the command-dispatch
/// binding). It throws a clear, actionable error if it is ever invoked at runtime before the real reader is wired —
/// it is never exercised by this story's unit tests, which substitute the seam.
/// </summary>
public sealed class DeferredProviderCatalogReader : IProviderCatalogReader
{
    /// <inheritdoc />
    public Task<ProviderCatalogEntryReadResult> GetEntryAsync(string tenantId, string providerId, string modelId, CancellationToken ct)
        => throw new NotSupportedException(
            "The live Agents provider-catalog reader is not wired yet (Story 1.5 defers the ProviderCatalog read-model "
            + "binding, mirroring Story 1.2/1.4). Register a concrete IProviderCatalogReader in the read-model story.");
}
