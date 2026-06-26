namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Outcome of an authorized inspection of an Agent Call's invocation gate evidence (AC3, AC4). Inspection returns a
/// structured status rather than throwing, and a failed inspection carries no evidence view so it never reveals
/// whether the interaction exists in another tenant or leaks unrelated records (AC3; AD-12).
/// </summary>
public enum AgentInteractionGateInspectionStatus
{
    /// <summary>The inspection succeeded; the safe evidence view is present.</summary>
    Success = 0,

    /// <summary>The caller is not authorized to inspect this interaction; no evidence is returned (AC3, AC4).</summary>
    NotAuthorized,

    /// <summary>No interaction exists for the requested aggregate; no evidence is returned.</summary>
    NotFound,
}
