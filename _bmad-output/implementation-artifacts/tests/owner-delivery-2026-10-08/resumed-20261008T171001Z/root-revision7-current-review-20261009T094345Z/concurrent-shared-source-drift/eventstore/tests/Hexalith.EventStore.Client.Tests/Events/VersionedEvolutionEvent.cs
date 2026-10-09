using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.EventStore.Client.Tests.Events;

/// <summary>Current event contract used to verify multi-step evolution.</summary>
[EventPayloadVersion(3)]
public sealed record VersionedEvolutionEvent(string Name) : IEventPayload;
