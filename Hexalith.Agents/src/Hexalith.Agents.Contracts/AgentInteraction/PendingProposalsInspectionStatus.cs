using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Outcome of an authorized pending-proposal queue read (AC2, AC3, AC4). The read returns a structured status rather
/// than throwing, and a failed/denied read carries no records so it never reveals whether proposals exist in another
/// tenant or leaks unrelated records (AD-12, AD-14). It mirrors the <see cref="AgentInteractionInspectionStatus"/>
/// <em>convention</em> (by-name serialization, <see cref="Unknown"/> fail-safe sentinel), but is an <b>extension, not a
/// copy</b>: it drops <c>NotFound</c> (a list read never targets a single record) and adds the two freshness states
/// <see cref="Unavailable"/>/<see cref="Stale"/> that back the UI <c>Error</c>/<c>Stale</c> surfaces (read models expose
/// freshness/degraded/blocked states — architecture Consistency Conventions; AD-12).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PendingProposalsInspectionStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>An authorized fresh read — the proposal list is present (it may still be empty).</summary>
    Success,

    /// <summary>Fail-closed denial — the caller is not authorized; no records are disclosed (AD-12, AC4).</summary>
    NotAuthorized,

    /// <summary>The read faulted / a dependency is unreachable — the UI <c>Error</c> surface; no records are returned.</summary>
    Unavailable,

    /// <summary>The projection reports degraded/stale data and must not be rendered as fresh — the UI <c>Stale</c> surface.</summary>
    Stale,
}
