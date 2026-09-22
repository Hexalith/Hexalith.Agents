using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Canonical outer status for public Agents automation operations.
/// </summary>
[JsonConverter(typeof(UnknownFallbackEnumConverter<AgentOperationStatus>))]
public enum AgentOperationStatus
{
    /// <summary>Absent or unrecognized status sentinel.</summary>
    Unknown = 0,

    /// <summary>The operation completed successfully.</summary>
    Succeeded = 1,

    /// <summary>The operation was accepted but is still pending asynchronous completion.</summary>
    Pending = 2,

    /// <summary>The operation is checking dependencies/readiness and must not be treated as success yet.</summary>
    Checking = 3,

    /// <summary>The operation completed with degraded or stale information that must not be rendered as fresh success.</summary>
    Degraded = 4,

    /// <summary>The authenticated caller is not authorized for the operation.</summary>
    NotAuthorized = 5,

    /// <summary>The request shape or trusted server-side validation failed.</summary>
    ValidationFailed = 6,

    /// <summary>The requested resource was not found in the caller's authorized scope.</summary>
    NotFound = 7,

    /// <summary>The operation conflicts with the current resource state.</summary>
    Conflict = 8,

    /// <summary>The operation could not proceed because the observed state is stale.</summary>
    Stale = 9,

    /// <summary>A required dependency, projection, or deferred binding is unavailable.</summary>
    Unavailable = 10,

    /// <summary>The governed workflow rejected the operation.</summary>
    Rejected = 11,

    /// <summary>The operation was blocked by policy or dependency readiness gates.</summary>
    Blocked = 12,

    /// <summary>The operation completed without enough trusted evidence to verify its exact outcome.</summary>
    UnableToVerify = 13,
}
