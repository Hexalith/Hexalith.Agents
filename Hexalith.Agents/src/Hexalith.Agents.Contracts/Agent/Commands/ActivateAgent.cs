namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Requests activation of an existing Agent (<c>hexa</c>) (AC2; FR-3). Activation re-evaluates this story's
/// required-field gates (display name present, Agent Instructions present and valid). On any blocker the command
/// is rejected with the specific blockers and the lifecycle is left unchanged — a rejected activation never makes
/// the Agent callable (AC2). The Agent identifier comes from the command envelope.
/// </summary>
public record ActivateAgent();
