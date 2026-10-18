using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Live <see cref="IAgentCommandDispatcher"/> binding. It admits an immutable <see cref="AgentCommandIntent"/> before
/// constructing any EventStore target command, then submits it through the gateway so an authorized command is durably
/// persisted as Agent events instead of hitting the fail-closed placeholder.
/// </summary>
/// <remarks>
/// Orchestrations strip client-reserved keys. Host admission verifies the operation, principal, scope and exact bytes,
/// durably registers replay and captures classified denials before this dispatcher constructs a target command.
/// An admitted intent that changes anything except added host <c>trust:</c> metadata is refused as
/// <c>DependencyNotAvailable(EXT-HOST-1)</c>, so the logical identity stays the idempotency key. Gateway failures surface as <see cref="EventStoreGatewayException"/> and are translated into safe public codes
/// by <see cref="AgentCommandDispatchFailure"/> at the operations edge — this seam never invents a success and
/// never lets infrastructure text reach a caller.
/// </remarks>
public sealed class EventStoreAgentCommandDispatcher : IAgentCommandDispatcher
{
    private const string HostTrustMetadataPrefix = "trust:";

    private readonly IEventStoreGatewayClient _gateway;
    private readonly IAgentTrustedCommandAdmission _admission;

    /// <summary>Initializes a new instance of the <see cref="EventStoreAgentCommandDispatcher"/> class.</summary>
    /// <param name="gateway">The EventStore command/query gateway client.</param>
    /// <param name="admission">The private host admission closure, or the fail-closed default when absent.</param>
    public EventStoreAgentCommandDispatcher(IEventStoreGatewayClient gateway, IAgentTrustedCommandAdmission? admission = null)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        _gateway = gateway;
        _admission = admission ?? new UnavailableAgentTrustedCommandAdmission(new CommittedAgentDependencyAvailability());
    }

    /// <inheritdoc />
    public async Task<SubmitCommandResponse> DispatchAsync(AgentCommandIntent intent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ct.ThrowIfCancellationRequested();
        // This await precedes even CommandEnvelope's eager target-identity validation. The ordinary dispatcher
        // never obtains the registrar or denial-worker credential from DI.
        AgentCommandIntent admitted = await _admission.AdmitAsync(intent, ct).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(admitted);
        ct.ThrowIfCancellationRequested();
        if (!PreservesAdmittedIntent(intent, admitted))
        {
            // Admission may only add authenticated trust metadata. A changed logical identity, target, operation,
            // actor, byte or assertion would let a delivery nonce or substituted scope reach the target command.
            throw new AgentDependencyNotAvailableException("EXT-HOST-1");
        }

        var envelope = new CommandEnvelope(
            admitted.MessageId, admitted.TenantId, admitted.Domain, admitted.AggregateId, admitted.CommandType,
            admitted.Payload, admitted.CorrelationId, admitted.CausationId, admitted.UserId, admitted.Extensions);

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

        return await _gateway.SubmitCommandAsync(request, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Binds the admitted result to the exact intent submitted for admission. Every logical, target, actor and byte
    /// field and every original metadata value must be unchanged; the only permitted additions are host
    /// <c>trust:</c> entries such as the separate delivery nonce.
    /// </summary>
    private static bool PreservesAdmittedIntent(AgentCommandIntent original, AgentCommandIntent admitted)
    {
        if (!string.Equals(original.MessageId, admitted.MessageId, StringComparison.Ordinal)
            || !string.Equals(original.TenantId, admitted.TenantId, StringComparison.Ordinal)
            || !string.Equals(original.Domain, admitted.Domain, StringComparison.Ordinal)
            || !string.Equals(original.AggregateId, admitted.AggregateId, StringComparison.Ordinal)
            || !string.Equals(original.CommandType, admitted.CommandType, StringComparison.Ordinal)
            || !string.Equals(original.CorrelationId, admitted.CorrelationId, StringComparison.Ordinal)
            || !string.Equals(original.CausationId, admitted.CausationId, StringComparison.Ordinal)
            || !string.Equals(original.UserId, admitted.UserId, StringComparison.Ordinal)
            || !original.Payload.AsSpan().SequenceEqual(admitted.Payload))
        {
            return false;
        }

        Dictionary<string, string> originalExtensions = original.Extensions ?? new(StringComparer.Ordinal);
        Dictionary<string, string> admittedExtensions = admitted.Extensions ?? new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> entry in originalExtensions)
        {
            if (!admittedExtensions.TryGetValue(entry.Key, out string? value)
                || !string.Equals(value, entry.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, string> entry in admittedExtensions)
        {
            if (!originalExtensions.ContainsKey(entry.Key)
                && (entry.Key.Length <= HostTrustMetadataPrefix.Length
                    || !entry.Key.StartsWith(HostTrustMetadataPrefix, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(entry.Value)))
            {
                return false;
            }
        }

        return true;
    }
}
