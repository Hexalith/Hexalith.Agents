namespace Hexalith.EventStore.Contracts.Events;

/// <summary>Resolves and validates an event's declared payload version.</summary>
public static class EventPayloadVersion {
    /// <summary>Gets the declared version, or one when no attribute is present.</summary>
    /// <param name="eventType">The event payload CLR type.</param>
    /// <returns>The declared payload version.</returns>
    public static int GetDeclaredVersion(Type eventType) {
        ArgumentNullException.ThrowIfNull(eventType);
        int version = eventType.GetCustomAttributes(typeof(EventPayloadVersionAttribute), false)
            .Cast<EventPayloadVersionAttribute>().SingleOrDefault()?.Version ?? 1;
        if (version is < 1 or > 1024) {
            throw new InvalidOperationException($"Event type '{eventType.FullName}' declares invalid payload version {version}; expected 1 through 1024.");
        }

        return version;
    }
}
