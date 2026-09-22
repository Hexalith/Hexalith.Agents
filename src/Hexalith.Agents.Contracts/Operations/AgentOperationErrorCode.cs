using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Safe automation error classes shared by the Agents public API and client facade.
/// </summary>
/// <remarks>
/// These values deliberately classify failures without carrying exception type names, stack traces, raw provider
/// payloads, EventStore stream names, tenant fingerprints, prompt/generated content, or secrets.
/// </remarks>
[JsonConverter(typeof(UnknownFallbackEnumConverter<AgentOperationErrorCode>))]
public enum AgentOperationErrorCode
{
    /// <summary>Absent or unrecognized error sentinel.</summary>
    Unknown = 0,

    /// <summary>The authenticated caller is not authorized for the operation.</summary>
    NotAuthorized = 1,

    /// <summary>The request shape or trusted server-side validation failed.</summary>
    ValidationFailed = 2,

    /// <summary>The requested resource was not found in the caller's authorized scope.</summary>
    NotFound = 3,

    /// <summary>The operation conflicts with the current resource state.</summary>
    Conflict = 4,

    /// <summary>The operation could not proceed because the observed state is stale.</summary>
    Stale = 5,

    /// <summary>A required dependency, projection, or deferred binding is unavailable.</summary>
    Unavailable = 6,

    /// <summary>The governed workflow rejected the operation.</summary>
    Rejected = 7,

    /// <summary>The operation was blocked by policy or dependency readiness gates.</summary>
    Blocked = 8,

    /// <summary>The operation completed without enough trusted evidence to verify its exact outcome.</summary>
    UnableToVerify = 9,
}
