namespace Hexalith.Agents.Contracts.Agent.Commands;

/// <summary>
/// Creates (or, when re-sent identically, idempotently acknowledges) the governed Agent record <c>hexa</c>
/// (AC1; FR-1). The new Agent starts in <see cref="AgentLifecycleStatus.Draft"/> and is not callable until a
/// gated activation succeeds. The aggregate identifier (the Agent id) comes from the command envelope.
/// </summary>
/// <remarks>
/// <see cref="Instructions"/> is sensitive Agent-authored content (AD-14): it is persisted only in the create/
/// update success events and durable state, and never echoed on a rejection, status view, log, or audit summary.
/// Draft creation tolerates missing display name / instructions; those are enforced as activation blockers (AC2),
/// not create-time errors.
/// </remarks>
/// <param name="TenantId">Stable tenant scope captured at create (FR-1; stored, never inferred).</param>
/// <param name="DisplayName">Safe display name (may be empty for an incomplete draft; required to activate).</param>
/// <param name="Description">Optional safe description.</param>
/// <param name="Instructions">Agent Instructions text (sensitive; required and valid to activate).</param>
public record CreateAgent(
    string TenantId,
    string DisplayName,
    string? Description,
    string Instructions);
