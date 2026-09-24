namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Outcome of an Agent setup write attempted from an administration surface (Story 5.2 AC1, AC3, AC4).
/// </summary>
public enum AgentSetupWriteStatus
{
    /// <summary>The write was accepted for processing; the durable truth is not yet confirmed.</summary>
    Submitted = 0,

    /// <summary>The caller is not an authorized Agent administrator for the tenant; nothing was mutated.</summary>
    NotAuthorized = 1,

    /// <summary>No Agent exists for the requested aggregate; nothing was mutated.</summary>
    NotFound = 2,

    /// <summary>The submitted fields are invalid; prior state is preserved.</summary>
    ValidationFailed = 3,

    /// <summary>The write conflicted with a concurrent change; prior state is preserved.</summary>
    Conflict = 4,

    /// <summary>The write path is not bound or is unreachable; no mutation outcome can be claimed.</summary>
    Unavailable = 5,

    /// <summary>The command completed as a no-op because the requested setup was already present.</summary>
    AlreadyApplied = 6,

    /// <summary>The command completed, but its exact target is still absent from the projected setup.</summary>
    AwaitingProjection = 7,

    /// <summary>The command outcome could not be correlated safely with a canonical receipt and target version.</summary>
    UnableToVerify = 8,

    /// <summary>The exact command reached a terminal domain rejection.</summary>
    Rejected = 9,
}
