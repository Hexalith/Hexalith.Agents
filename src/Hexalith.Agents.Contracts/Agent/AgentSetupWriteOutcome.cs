using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>Versioned, fail-closed outcome of a setup command status read.</summary>
[JsonConverter(typeof(UnknownFallbackEnumConverter<AgentSetupWriteOutcome>))]
public enum AgentSetupWriteOutcome
{
    /// <summary>No recognized outcome was supplied; no acceptance or completion can be claimed.</summary>
    Unknown = 0,

    /// <summary>The write was accepted for processing.</summary>
    Submitted = 1,

    /// <summary>The caller is not an authorized Agent administrator for the tenant; nothing was mutated.</summary>
    NotAuthorized = 2,

    /// <summary>No Agent exists for the requested aggregate; nothing was mutated.</summary>
    NotFound = 3,

    /// <summary>The submitted fields are invalid; prior state is preserved.</summary>
    ValidationFailed = 4,

    /// <summary>The write conflicted with a concurrent change; prior state is preserved.</summary>
    Conflict = 5,

    /// <summary>The write path is not bound or is unreachable; no mutation outcome can be claimed.</summary>
    Unavailable = 6,

    /// <summary>The command completed as a no-op because the requested setup was already present.</summary>
    AlreadyApplied = 7,

    /// <summary>The command completed, but its exact target is still absent from the projected setup.</summary>
    AwaitingProjection = 8,

    /// <summary>The command outcome could not be correlated safely with a canonical receipt and target version.</summary>
    UnableToVerify = 9,

    /// <summary>The exact command reached a terminal domain rejection.</summary>
    Rejected = 10,
}
