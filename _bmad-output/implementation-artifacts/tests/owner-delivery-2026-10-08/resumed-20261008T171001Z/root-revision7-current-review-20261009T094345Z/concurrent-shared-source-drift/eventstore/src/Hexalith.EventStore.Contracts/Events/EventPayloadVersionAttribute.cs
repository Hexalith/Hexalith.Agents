namespace Hexalith.EventStore.Contracts.Events;

/// <summary>Declares the current JSON payload version of a domain event.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class EventPayloadVersionAttribute(int version) : Attribute {
    /// <summary>Gets the declared version, which must be between 1 and 1024.</summary>
    public int Version { get; } = version;
}
