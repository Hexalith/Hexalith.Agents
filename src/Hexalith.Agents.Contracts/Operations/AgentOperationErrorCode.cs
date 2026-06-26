using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Safe automation error classes shared by the Agents public API and client facade.
/// </summary>
/// <remarks>
/// These values deliberately classify failures without carrying exception type names, stack traces, raw provider
/// payloads, EventStore stream names, tenant fingerprints, prompt/generated content, or secrets.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentOperationErrorCode
{
    /// <summary>Absent or unrecognized error sentinel.</summary>
    Unknown = 0,

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
