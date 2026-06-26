using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Outcome of an authorized Agent Call (<c>AgentInteraction</c>) status inspection (AC2, AC3). Inspection returns a
/// structured status rather than throwing, and a failed inspection carries no status view so it never reveals whether
/// the interaction exists in another tenant or leaks unrelated records (AD-12, AD-14). Mirrors
/// <see cref="Agent.AgentInspectionStatus"/> and <see cref="AgentInteractionContextInspectionStatus"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentInteractionInspectionStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>The inspection succeeded; the safe status view is present.</summary>
    Success,

    /// <summary>The caller is not authorized to inspect this interaction; no status view is returned (AD-12).</summary>
    NotAuthorized,

    /// <summary>No interaction exists for the requested aggregate; no status view is returned (never reveals cross-tenant existence; AD-12).</summary>
    NotFound,
}
