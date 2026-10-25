using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Reads terminal command evidence through authenticated, tenant and domain bound SDK transport.</summary>
/// <param name="gatewayFactory">Creates an SDK client whose assertion delegate binds one exact domain.</param>
internal sealed class EventStoreAgentCommandStatusReader(
    Func<string, IEventStoreGatewayClient> gatewayFactory) : IAgentCommandStatusReader
{
    /// <inheritdoc />
    public Task<bool?> WasRejectedAsync(string messageId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // A message alone supplies no trusted tenant or domain, so it cannot authorize a workload read.
        return Task.FromResult<bool?>(null);
    }

    /// <inheritdoc />
    public async Task<bool?> WasRejectedAsync(string tenantId, string domain, string messageId, string correlationId, CancellationToken ct)
    {
        CommandStatusQueryResponse? status = await ReadAsync(tenantId, domain, messageId, correlationId, ct).ConfigureAwait(false);
        return status?.IsRejected is true ? true : null;
    }

    /// <inheritdoc />
    public async Task<CommandStatusQueryResponse?> ReadAsync(
        string tenantId, string domain, string messageId, string? correlationId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(domain)
            || string.IsNullOrWhiteSpace(messageId) || correlationId is not null && string.IsNullOrWhiteSpace(correlationId))
        {
            return null;
        }

        try
        {
            CommandStatusQueryResponse? status = await gatewayFactory(domain)
                .GetWorkloadCommandStatusAsync(tenantId, messageId, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            return status is not null
                && string.Equals(status.TenantId, tenantId, StringComparison.Ordinal)
                && string.Equals(status.Domain, domain, StringComparison.Ordinal)
                && string.Equals(status.MessageId, messageId, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(status.CorrelationId)
                && (correlationId is null || string.Equals(status.CorrelationId, correlationId, StringComparison.Ordinal))
                && Enum.IsDefined((CommandStatus)status.StatusCode)
                && string.Equals(Enum.GetName((CommandStatus)status.StatusCode), status.Status, StringComparison.Ordinal)
                ? status : null;
        }
        catch (Exception)
        {
            // Caller cancellation wins even when a non-cooperative transport returns a different failure.
            ct.ThrowIfCancellationRequested();
            // Missing credentials, transport failures and timeouts supply no domain outcome evidence.
            return null;
        }
    }
}
