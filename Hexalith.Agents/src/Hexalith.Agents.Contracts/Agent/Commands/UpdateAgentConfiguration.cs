namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Updates the safe identity metadata and Agent Instructions of an existing Agent (<c>hexa</c>) (AC1; FR-1).
/// An update that changes nothing is a deterministic no-op; an accepted update bumps the configuration version
/// (and the instructions version only when the instructions text changes).
/// </summary>
/// <remarks>
/// <see cref="Instructions"/> is sensitive Agent-authored content (AD-14): persisted only in the success event
/// and durable state, never echoed on a rejection, status view, log, or audit summary. Lifecycle is not changed
/// by this command — activation and disabling are separate, explicitly gated transitions.
/// </remarks>
/// <param name="DisplayName">Safe display name (may be empty for an incomplete draft; required to activate).</param>
/// <param name="Description">Optional safe description.</param>
/// <param name="Instructions">Agent Instructions text (sensitive; required and valid to activate).</param>
public record UpdateAgentConfiguration(
    string DisplayName,
    string? Description,
    string Instructions);
