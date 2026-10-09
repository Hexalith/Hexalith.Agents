using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.EventStore.Client.Tests.Events;

/// <summary>Current contract reached from a historical event name.</summary>
[EventPayloadVersion(2)]
public sealed record RenamedEvolutionEvent(string Name) : IEventPayload;
