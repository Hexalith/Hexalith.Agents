namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records an accepted update to an Agent's safe identity metadata and/or Agent Instructions (AC1; FR-1).
/// Emitted only when the update actually changes something; <see cref="ConfigurationVersion"/> is the bumped
/// version and <see cref="InstructionsVersion"/> bumps only when the instructions text changed.
/// </summary>
/// <remarks>
/// This is a success event and is the sanctioned durable home for the sensitive Agent Instructions text (AD-14):
/// <see cref="Instructions"/> lives only here, on <see cref="AgentCreated"/>, and in durable state. For
/// audit-facing "prior/new values where safe to expose" (AC1), safe fields (display name, description) may be
/// surfaced, but for instructions only the <see cref="InstructionsChanged"/> indicator and the new
/// <see cref="InstructionsVersion"/> are audit-safe — never the raw prior/new instruction text. No wall-clock
/// timestamp is carried (AD-3); occurrence time comes from EventStore event metadata.
/// </remarks>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
/// <param name="DisplayName">New safe display name.</param>
/// <param name="Description">New optional safe description.</param>
/// <param name="Instructions">New Agent Instructions text (sensitive; durable here only — AD-14).</param>
/// <param name="InstructionsChanged">Audit-safe indicator that the instructions text changed in this update.</param>
/// <param name="ConfigurationVersion">The bumped configuration version.</param>
/// <param name="InstructionsVersion">The instructions version (bumped only when the instructions text changed).</param>
public record AgentConfigurationUpdated(
    string AgentId,
    string DisplayName,
    string? Description,
    string Instructions,
    bool InstructionsChanged,
    int ConfigurationVersion,
    int InstructionsVersion) : IEventPayload;
