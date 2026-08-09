namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Outcome of an authorized Agent status/configuration inspection (AC3, AC4). Inspection returns a structured
/// status rather than throwing, and a failed inspection carries no Agent data so it never fingerprints whether
/// the Agent exists or leaks unrelated tenant records.
/// </summary>
public enum AgentInspectionStatus
{
    /// <summary>The inspection succeeded; the status view is present.</summary>
    Success = 0,

    /// <summary>The caller is not authorized to administer Agents for the tenant; no Agent data is returned (AC4).</summary>
    NotAuthorized,

    /// <summary>No Agent exists for the requested aggregate; no Agent data is returned.</summary>
    AgentNotFound,

    /// <summary>
    /// The read could not be served — the backing surface was unreachable or failed. No Agent data is returned, and
    /// the outcome is deliberately distinct from <see cref="NotAuthorized"/> and <see cref="AgentNotFound"/>: a
    /// transport failure must not be shown to an administrator as "denied" or "no such Agent", because both of
    /// those are claims about state this read never established.
    /// </summary>
    Unavailable,
}
