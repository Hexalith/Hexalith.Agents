using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Canonical outer status for public Agents automation operations.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentOperationStatus
{
    /// <summary>Absent or unrecognized status sentinel.</summary>
    Unknown = 0,

    /// <summary>The operation completed successfully.</summary>
    Succeeded,

    /// <summary>The operation was accepted but is still pending asynchronous completion.</summary>
    Pending,

    /// <summary>The operation is checking dependencies/readiness and must not be treated as success yet.</summary>
    Checking,

    /// <summary>The operation completed with degraded or stale information that must not be rendered as fresh success.</summary>
    Degraded,

    /// <summary>The authenticated caller is not authorized for the operation.</summary>
    NotAuthorized,

    /// <summary>The request shape or trusted server-side validation failed.</summary>
    ValidationFailed,

    /// <summary>The requested resource was not found in the caller's authorized scope.</summary>
    NotFound,

    /// <summary>The operation conflicts with the current resource state.</summary>
    Conflict,

    /// <summary>The operation could not proceed because the observed state is stale.</summary>
    Stale,

    /// <summary>A required dependency, projection, or deferred binding is unavailable.</summary>
    Unavailable,

    /// <summary>The governed workflow rejected the operation.</summary>
    Rejected,

    /// <summary>The operation was blocked by policy or dependency readiness gates.</summary>
    Blocked,
}
