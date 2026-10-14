namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// The fail-closed outcome of resolving a single approver source against its dependency (Story 1.6 AC3). It is safe
/// by construction — it classifies the resolution state and carries no PII or secret. <see cref="Unknown"/>
/// (ordinal 0) is the fail-safe sentinel.
/// </summary>
public enum ApproverSourceOutcome
{
    /// <summary>Absent/unrecognized outcome sentinel — treated as a degraded read and fails closed.</summary>
    Unknown = 0,

    /// <summary>The source resolved cleanly and is usable as an approver.</summary>
    Resolved,

    /// <summary>The source resolved to nothing (e.g. no such role / no such Party).</summary>
    Missing,

    /// <summary>The source exists but is deactivated, erased, or restricted.</summary>
    Disabled,

    /// <summary>The source reference resolved to more than one candidate (ambiguous authority).</summary>
    Ambiguous,

    /// <summary>The source could not be read freshly (stale/degraded projection or resolver unavailable) — fail closed.</summary>
    Unavailable,

    /// <summary>The source is outside the Agent's tenant scope or the caller is not authorized to read it.</summary>
    Unauthorized,
}
