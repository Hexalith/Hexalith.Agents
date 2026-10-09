using System.Text.Json.Nodes;

namespace Hexalith.EventStore.Client.Events;

/// <summary>Converts one JSON event payload version to the next version.</summary>
public interface IEventPayloadUpcaster {
    /// <summary>Gets the stored event type name accepted by this step.</summary>
    string EventTypeName { get; }

    /// <summary>Gets the payload version accepted by this step.</summary>
    int FromVersion { get; }

    /// <summary>Gets the output event type name for a rename, or null to retain the input name.</summary>
    string? TargetEventTypeName => null;

    /// <summary>Returns a new JSON object for version <see cref="FromVersion"/> plus one.</summary>
    /// <param name="payload">A private JSON object detached from stored event bytes.</param>
    /// <returns>The converted payload.</returns>
    JsonObject Upcast(JsonObject payload);
}
