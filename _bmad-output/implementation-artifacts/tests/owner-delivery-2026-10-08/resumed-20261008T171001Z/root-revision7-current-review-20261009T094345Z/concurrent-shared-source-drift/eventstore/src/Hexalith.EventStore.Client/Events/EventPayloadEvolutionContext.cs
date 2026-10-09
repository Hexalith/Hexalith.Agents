using System.Threading;

namespace Hexalith.EventStore.Client.Events;

/// <summary>Flows the host's immutable event reader registry through static aggregate conventions.</summary>
public sealed class EventPayloadEvolutionContext : IDisposable {
    private static readonly AsyncLocal<EventPayloadEvolutionRegistry?> CurrentRegistry = new();
    private readonly EventPayloadEvolutionRegistry? _previous;

    private EventPayloadEvolutionContext(EventPayloadEvolutionRegistry registry) {
        _previous = CurrentRegistry.Value;
        CurrentRegistry.Value = registry;
    }

    /// <summary>Gets the registry active for this asynchronous request, when registered.</summary>
    public static EventPayloadEvolutionRegistry? Current => CurrentRegistry.Value;

    /// <summary>Activates a registry for the current asynchronous request.</summary>
    /// <param name="registry">The host's validated registry.</param>
    /// <returns>A scope that restores the previous registry.</returns>
    public static EventPayloadEvolutionContext Enter(EventPayloadEvolutionRegistry registry) {
        ArgumentNullException.ThrowIfNull(registry);
        return new EventPayloadEvolutionContext(registry);
    }

    /// <summary>Restores the previously active registry.</summary>
    public void Dispose() => CurrentRegistry.Value = _previous;
}
