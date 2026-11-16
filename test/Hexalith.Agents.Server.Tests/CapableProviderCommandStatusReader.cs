using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Explicitly capable fixture host; production SDK system refusal remains independently tested.</summary>
internal sealed class CapableProviderCommandStatusReader(IEventStoreGatewayClient gateway) : IAgentCommandStatusReader
{
    /// <inheritdoc />
    public bool SupportsScope(string tenantId, string domain) => tenantId == "system" && domain == "provider-catalog";

    /// <inheritdoc />
    public Task<bool?> WasRejectedAsync(string messageId, CancellationToken ct) => Task.FromResult<bool?>(null);

    /// <inheritdoc />
    public Task<CommandStatusQueryResponse?> ReadAsync(string tenantId, string domain, string messageId, string? correlationId, CancellationToken ct)
        => new EventStoreAgentCommandStatusReader(_ => gateway).ReadAsync(tenantId, domain, messageId, correlationId, ct);
}
