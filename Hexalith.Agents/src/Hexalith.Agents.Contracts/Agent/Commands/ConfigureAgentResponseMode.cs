namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Chooses the Response Mode for an Agent (<c>hexa</c>) (AC1; FR-6). The command payload carries only the safe
/// <see cref="Mode"/> choice; the Agent identifier comes from the command envelope. No wall-clock and no verdict
/// are on the payload — response-mode configuration has no external dependency to resolve.
/// </summary>
/// <remarks>
/// Configuring the mode records it and bumps the Agent's configuration version (AD-4 snapshot); the change applies
/// only to future Agent Calls and prior interactions are never rewritten (AC1). Re-asserting the already-recorded
/// mode is a deterministic no-op (AD-13). The sentinel <see cref="AgentResponseMode.Unknown"/> cannot be configured
/// — choosing it is rejected as invalid configuration.
/// </remarks>
/// <param name="Mode">The Response Mode to record (must be <see cref="AgentResponseMode.Automatic"/> or <see cref="AgentResponseMode.Confirmation"/>).</param>
public record ConfigureAgentResponseMode(AgentResponseMode Mode);
