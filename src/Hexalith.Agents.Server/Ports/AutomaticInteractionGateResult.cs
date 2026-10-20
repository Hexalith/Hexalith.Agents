using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.Security;
namespace Hexalith.Agents.Server.Ports;

/// <summary>Trusted fresh pre-creation gates 1–4; no Approver or membership effect is part of intake.</summary>
/// <param name="Authorized">The Authorized binding.</param>
/// <param name="Snapshot">The Snapshot binding.</param>
/// <param name="Dependency">The Dependency binding.</param>
public sealed record AutomaticInteractionGateResult(bool Authorized, AgentInteractionSnapshot? Snapshot, DependencyNotAvailable? Dependency = null);
