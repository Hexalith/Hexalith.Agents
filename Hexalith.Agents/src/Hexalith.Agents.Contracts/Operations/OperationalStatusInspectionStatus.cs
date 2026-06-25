using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Outcome of an authorized operational-status summary read (Story 4.3 AC1, AC4). Inspection returns a structured status
/// rather than throwing, and a failed inspection carries no summary so it never reveals whether records exist in another
/// tenant or leaks unrelated records via counts/rates (AD-12, AD-14). Mirrors
/// <see cref="AgentInteraction.PendingProposalsInspectionStatus"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationalStatusInspectionStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>The inspection succeeded; the safe summary is present.</summary>
    Success,

    /// <summary>The caller is not authorized; no summary is returned (AD-12).</summary>
    NotAuthorized,

    /// <summary>The read faulted or a dependency/projection is unreachable; no summary is returned (the UI <c>Unavailable</c> surface).</summary>
    Unavailable,

    /// <summary>The projection reports stale/degraded data; the summary may be carried behind a stale/degraded notice (never rendered as fresh).</summary>
    Stale,
}
