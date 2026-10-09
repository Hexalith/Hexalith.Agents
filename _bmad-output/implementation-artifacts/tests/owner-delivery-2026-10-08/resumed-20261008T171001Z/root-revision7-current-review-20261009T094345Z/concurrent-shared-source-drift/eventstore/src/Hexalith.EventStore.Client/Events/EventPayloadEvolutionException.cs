namespace Hexalith.EventStore.Client.Events;

/// <summary>Reports an unreadable known event without retaining payload or upcaster exception text.</summary>
public sealed class EventPayloadEvolutionException : InvalidOperationException {
    /// <summary>Creates a support-safe event evolution failure.</summary>
    public EventPayloadEvolutionException(string eventTypeName, int storedVersion, long sequenceNumber,
        string? upcasterTypeName = null, string? innerExceptionTypeName = null)
        : base($"Event payload evolution failed for type '{eventTypeName}', stored version {storedVersion}, sequence {sequenceNumber}, upcaster '{upcasterTypeName ?? "none"}', exception type '{innerExceptionTypeName ?? "none"}'.") {
        EventTypeName = eventTypeName;
        StoredVersion = storedVersion;
        SequenceNumber = sequenceNumber;
        UpcasterTypeName = upcasterTypeName;
        InnerExceptionTypeName = innerExceptionTypeName;
    }

    /// <summary>Gets the stored event type name.</summary>
    public string EventTypeName { get; }

    /// <summary>Gets the stored version, with null represented as version one.</summary>
    public int StoredVersion { get; }

    /// <summary>Gets the stored sequence number.</summary>
    public long SequenceNumber { get; }

    /// <summary>Gets the failing upcaster CLR type name when a step failed.</summary>
    public string? UpcasterTypeName { get; }

    /// <summary>Gets only the inner exception CLR type name.</summary>
    public string? InnerExceptionTypeName { get; }
}
