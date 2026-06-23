namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// A create/update command carried structurally invalid configuration — e.g. a display name, description, or
/// instructions value that exceeds the safe stored bounds, or a missing tenant scope (AC1). The
/// <paramref name="Reason"/> is a safe, display-friendly classification and never echoes the offending value;
/// in particular it never contains the raw Agent Instructions text (AD-14).
/// </summary>
/// <param name="AgentId">The Agent aggregate identifier the command targeted.</param>
/// <param name="Reason">Safe classification of why the configuration was rejected (never raw input or instructions).</param>
public record InvalidAgentConfigurationRejection(
    string AgentId,
    string Reason) : IRejectionEvent;
