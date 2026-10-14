using System.Globalization;
using Hexalith.Agents.Contracts.SecurityEventLog;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.SecurityEventLog;

/// <summary>Pure daily security recording, invoked only by the Platform private exact-target worker.</summary>
[EventStoreDomain(SecurityEventLogIdentity.Domain)]
public sealed class SecurityEventLogAggregate : EventStoreAggregate<SecurityEventLogState>
{
    /// <summary>Appends an original once; exact lost-ack recovery is a no-op and changed facts conflict.</summary>
    public static DomainResult Handle(RecordSecurityEvent command, SecurityEventLogState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        if (string.IsNullOrWhiteSpace(command.SecurityObservationId) || string.IsNullOrWhiteSpace(command.RoutingTenantId)
            || string.IsNullOrWhiteSpace(command.DigestKeyVersion) || command.ObservedAt == default
            || command.ObservedAt.Offset != TimeSpan.Zero
            || command.UtcDay != command.ObservedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            || command.ReasonCode is not ("invalid-tag" or "scope-mismatch" or "unknown-principal" or "stale-authority" or "replay-conflict" or "unavailable-owner")
            || command.UntrustedFieldsHmac is not { Length: 64 } || !command.UntrustedFieldsHmac.All(char.IsAsciiHexDigit)
            || envelope.Domain != SecurityEventLogIdentity.Domain || envelope.TenantId != command.RoutingTenantId
            || envelope.AggregateId != SecurityEventLogIdentity.Create(command.RoutingTenantId, command.UtcDay))
        {
            return DomainResult.Rejection([new SecurityEventRejected("InvalidObservation")]);
        }

        if (state?.Observations.TryGetValue(command.SecurityObservationId, out RecordSecurityEvent? original) == true)
        {
            return original == command ? DomainResult.NoOp() : DomainResult.Rejection([new SecurityEventRejected("ObservationConflict")]);
        }

        return DomainResult.Success([new SecurityEventRecorded(command)]);
    }
}
