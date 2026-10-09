namespace Hexalith.EventStore.Contracts.Events;

/// <summary>Reports one malformed event identity component without exposing event payload data.</summary>
public sealed class EventIdentityException : InvalidOperationException {
    /// <summary>Initializes an identity failure for the named component.</summary>
    /// <param name="component">The malformed metadata component.</param>
    public EventIdentityException(string component)
        : base($"Invalid event identity component: {component}.") {
        Component = component;
    }

    /// <summary>Gets the malformed component name.</summary>
    public string Component { get; }
}
