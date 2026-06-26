using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Outcome of an authorized single Proposed-Agent-Reply <em>detail</em> read backing the Story 3.7 approval workspace
/// (AC1, AC2; AD-12, AD-14). The read returns a structured status rather than throwing, and a failed/denied/not-found
/// read carries no detail so it never reveals whether the proposal exists in another tenant or leaks unrelated records
/// (AD-12, AD-14). It mirrors the <see cref="PendingProposalsInspectionStatus"/> <em>convention</em> (by-name
/// serialization, <see cref="Unknown"/> fail-safe sentinel, the <see cref="Unavailable"/>/<see cref="Stale"/> freshness
/// states) but, because a detail read targets a <b>single</b> record, additionally re-introduces
/// <see cref="NotFound"/> (which the list-shaped <see cref="PendingProposalsInspectionStatus"/> dropped) — a
/// missing/other-tenant interaction returns <see cref="NotFound"/> with no detail (no fingerprinting that the record
/// exists elsewhere).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProposalDetailInspectionStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>An authorized fresh read — the proposal detail is present.</summary>
    Success,

    /// <summary>Fail-closed denial — the caller is not authorized; no detail is disclosed (AD-12, AC1).</summary>
    NotAuthorized,

    /// <summary>The read faulted / a dependency is unreachable — the UI <c>Unavailable</c> surface; no detail is returned.</summary>
    Unavailable,

    /// <summary>The projection reports degraded/stale data and must not be rendered as fresh — the UI <c>Stale</c> surface.</summary>
    Stale,

    /// <summary>No interaction exists for the requested aggregate within the tenant — no detail is returned (never reveals cross-tenant existence; AD-12).</summary>
    NotFound,
}
