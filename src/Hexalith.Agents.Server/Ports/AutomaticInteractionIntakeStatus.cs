using System.Text.Json.Serialization;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Safe intake outcomes.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AutomaticInteractionIntakeStatus>))]
public enum AutomaticInteractionIntakeStatus
{
    /// <summary>Unknown outcome.</summary>
    Unknown = 0,
    /// <summary>NotFound outcome.</summary>
    NotFound = 1,
    /// <summary>DependencyNotAvailable outcome.</summary>
    DependencyNotAvailable = 2,
    /// <summary>PayloadProtectionUnavailable outcome.</summary>
    PayloadProtectionUnavailable = 3,
    /// <summary>DirectoryNotReady outcome.</summary>
    DirectoryNotReady = 4,
    /// <summary>ConversationClosing outcome.</summary>
    ConversationClosing = 5,
    /// <summary>Scheduled outcome.</summary>
    Scheduled = 6,
    /// <summary>StartSuppressedByDeletion outcome.</summary>
    StartSuppressedByDeletion = 7,
}
