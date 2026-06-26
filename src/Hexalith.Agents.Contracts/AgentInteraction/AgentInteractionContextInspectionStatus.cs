namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Outcome of an authorized inspection of an Agent Call's Conversation context evidence (AC2, AC3, AC4; FR-25).
/// Inspection returns a structured status rather than throwing, and a failed inspection carries no evidence view so it
/// never reveals whether the interaction exists in another tenant or leaks unrelated records (AC1; AD-12). Mirrors
/// <see cref="AgentInteractionGateInspectionStatus"/>.
/// </summary>
public enum AgentInteractionContextInspectionStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>The inspection succeeded; the safe evidence view is present.</summary>
    Success,

    /// <summary>The caller is not authorized to inspect this interaction; no evidence is returned (AC1).</summary>
    NotAuthorized,

    /// <summary>No interaction exists for the requested aggregate; no evidence is returned (never reveals cross-tenant existence; AC1).</summary>
    NotFound,
}
