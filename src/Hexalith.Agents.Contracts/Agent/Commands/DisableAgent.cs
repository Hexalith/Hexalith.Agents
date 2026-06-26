namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Disables an existing Agent (<c>hexa</c>) (AC3; FR-3). Disabling is a lifecycle flag flip only: it makes the
/// Agent non-callable and visibly disabled through the public status path, while preserving all prior history
/// (identity, instructions, configuration, and — in later epics — Audit Evidence, Proposed Agent Replies, and
/// Conversation Messages are never deleted or rewritten). The Agent identifier comes from the command envelope.
/// </summary>
public record DisableAgent();
