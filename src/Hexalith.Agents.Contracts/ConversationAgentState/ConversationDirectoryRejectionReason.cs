using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.ConversationAgentState;

/// <summary>Closed directory vocabulary.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ConversationDirectoryRejectionReason>))]
public enum ConversationDirectoryRejectionReason
{
    /// <summary>Unknown classification.</summary>
    Unknown = 0,
    /// <summary>InvalidTarget classification.</summary>
    InvalidTarget = 1,
    /// <summary>DirectoryNotReady classification.</summary>
    DirectoryNotReady = 2,
    /// <summary>ProtectionTargetMismatch classification.</summary>
    ProtectionTargetMismatch = 3,
    /// <summary>StaleRevision classification.</summary>
    StaleRevision = 4,
    /// <summary>DivergentDuplicate classification.</summary>
    DivergentDuplicate = 5,
    /// <summary>ConversationClosing classification.</summary>
    ConversationClosing = 6,
    /// <summary>PermitNotFound classification.</summary>
    PermitNotFound = 7,
    /// <summary>LeaseNotFound classification.</summary>
    LeaseNotFound = 8,
    /// <summary>LeaseNotCommitted classification.</summary>
    LeaseNotCommitted = 9,
    /// <summary>LeaseCancelled classification.</summary>
    LeaseCancelled = 10,
    /// <summary>InvalidLease classification.</summary>
    InvalidLease = 11,
    /// <summary>NotAuthorized classification.</summary>
    NotAuthorized = 12,
}
