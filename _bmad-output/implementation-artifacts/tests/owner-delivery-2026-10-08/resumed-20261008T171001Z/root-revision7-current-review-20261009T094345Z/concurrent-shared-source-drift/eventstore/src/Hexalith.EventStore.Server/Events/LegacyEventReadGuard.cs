using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.EventStore.Server.Events;

/// <summary>Fences versioned stored events from readers that still use typed source bytes.</summary>
internal static class LegacyEventReadGuard
{
    /// <summary>Refuses a source requiring the authenticated evolution route.</summary>
    internal static void RequireUnversioned(EventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        EventIdentityValidator.ValidateRead(envelope.TenantId, envelope.Domain, envelope.AggregateId,
            envelope.AggregateType, envelope.EventTypeName, envelope.MessageId, envelope.CorrelationId,
            envelope.CausationId, envelope.SequenceNumber);
        if (envelope.MetadataVersion != 1 || envelope.EventContractType is not null
            || envelope.PayloadVersion is < 1 or > 1024)
        {
            throw new InvalidOperationException("RollbackReaderCapabilityHold: a typed legacy reader cannot consume a versioned stored event.");
        }
    }
}
