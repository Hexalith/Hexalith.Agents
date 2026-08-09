using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Live <see cref="IAgentCommandDispatcher"/> binding (Story 5.2 AC1). It submits the fully-built, server-trusted
/// <see cref="CommandEnvelope"/> through the EventStore gateway so an authorized administration command is durably
/// persisted as Agent events instead of hitting the fail-closed placeholder.
/// </summary>
/// <remarks>
/// The dispatcher performs no authorization and populates no trusted extension: the orchestration that built the
/// envelope already stripped every client-supplied reserved key and repopulated only the keys it can vouch for.
/// Gateway failures surface as <see cref="EventStoreGatewayException"/> and are translated into safe public codes
/// by <see cref="AgentCommandDispatchFailure"/> at the operations edge — this seam never invents a success and
/// never lets infrastructure text reach a caller.
/// </remarks>
public sealed class EventStoreAgentCommandDispatcher : IAgentCommandDispatcher
{
    private readonly IEventStoreGatewayClient _gateway;

    /// <summary>Initializes a new instance of the <see cref="EventStoreAgentCommandDispatcher"/> class.</summary>
    /// <param name="gateway">The EventStore command/query gateway client.</param>
    public EventStoreAgentCommandDispatcher(IEventStoreGatewayClient gateway)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        _gateway = gateway;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(CommandEnvelope envelope, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        using JsonDocument payload = JsonDocument.Parse(
            envelope.Payload.Length == 0 ? "{}"u8.ToArray() : envelope.Payload);

        var request = new SubmitCommandRequest(
            envelope.MessageId,
            envelope.TenantId,
            envelope.Domain,
            envelope.AggregateId,
            envelope.CommandType,
            payload.RootElement.Clone(),
            envelope.CorrelationId,
            envelope.Extensions is null ? null : new Dictionary<string, string>(envelope.Extensions, StringComparer.Ordinal),
            // The message id is the deterministic command identity the orchestrations already derive, so an exact
            // duplicate submission is idempotent at the gateway rather than appending a second event.
            envelope.MessageId);

        _ = await _gateway.SubmitCommandAsync(request, ct).ConfigureAwait(false);
    }
}
