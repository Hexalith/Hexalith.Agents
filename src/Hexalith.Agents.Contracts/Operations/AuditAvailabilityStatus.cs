using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>Canonical public audit availability terms.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuditAvailabilityStatus
{
    /// <summary>Absent or unrecognized audit status sentinel.</summary>
    Unknown = 0,

    /// <summary>Audit evidence is pending and is not success.</summary>
    AuditPending,

    /// <summary>Audit evidence is available.</summary>
    AuditAvailable,

    /// <summary>Audit evidence is delayed and must not be rendered as available.</summary>
    AuditDelayed,

    /// <summary>Audit evidence is unavailable.</summary>
    AuditUnavailable,
}
