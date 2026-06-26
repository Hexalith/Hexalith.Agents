namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// Agent administration authorization failed closed (AC4; FR-20, FR-21). Emitted before any mutation when the
/// caller is not an authorized Agents administrator for the tenant. Deliberately reveals nothing about whether
/// the Agent exists, its instructions, or unrelated tenant data — it carries only the Agent identity, the actor,
/// and the attempted command name, so the denial is auditable without leaking sensitive content (AD-14).
/// </summary>
/// <param name="AgentId">The Agent aggregate identifier the command targeted.</param>
/// <param name="ActorUserId">The unauthorized actor.</param>
/// <param name="CommandName">The attempted command.</param>
public record AgentAdministrationDeniedRejection(
    string AgentId,
    string ActorUserId,
    string CommandName) : IRejectionEvent;
