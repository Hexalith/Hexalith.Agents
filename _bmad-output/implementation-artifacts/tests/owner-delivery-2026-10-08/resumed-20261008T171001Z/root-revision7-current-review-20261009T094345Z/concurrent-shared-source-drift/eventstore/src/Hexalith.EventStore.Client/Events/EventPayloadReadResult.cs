namespace Hexalith.EventStore.Client.Events;

/// <summary>Describes an in-memory readable payload without changing stored event data.</summary>
/// <param name="EventTypeName">The effective event type name.</param>
/// <param name="Payload">The readable JSON payload bytes.</param>
/// <param name="EventType">The resolved current CLR event type, if known.</param>
public sealed record EventPayloadReadResult(string EventTypeName, byte[] Payload, Type? EventType);
