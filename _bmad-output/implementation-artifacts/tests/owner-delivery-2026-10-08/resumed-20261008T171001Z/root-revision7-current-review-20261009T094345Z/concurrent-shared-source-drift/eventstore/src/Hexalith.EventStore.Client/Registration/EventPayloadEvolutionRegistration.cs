using System.Reflection;

using Hexalith.EventStore.Client.Events;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.EventStore.Client.Registration;

/// <summary>Collects event and upcaster types until the host constructs its immutable registry.</summary>
internal sealed class EventPayloadEvolutionRegistration {
    private readonly HashSet<Type> _eventTypes = [];
    private readonly HashSet<Type> _upcasterTypes = [];

    internal void AddAssembly(Assembly assembly) {
        ArgumentNullException.ThrowIfNull(assembly);
        foreach (Type type in assembly.GetTypes()) {
            if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters) { continue; }
            if (typeof(IEventPayload).IsAssignableFrom(type)) { _eventTypes.Add(type); }
            if (typeof(IEventPayloadUpcaster).IsAssignableFrom(type)) { _upcasterTypes.Add(type); }
        }
    }

    internal void AddUpcaster(Type type) => _upcasterTypes.Add(type);

    internal EventPayloadEvolutionRegistry Build() {
        IEventPayloadUpcaster[] upcasters = [.. _upcasterTypes.Select(type =>
            Activator.CreateInstance(type, nonPublic: true) as IEventPayloadUpcaster
                ?? throw new InvalidOperationException($"Upcaster '{type.FullName}' requires a parameterless constructor."))];
        return new EventPayloadEvolutionRegistry(_eventTypes, upcasters);
    }
}
