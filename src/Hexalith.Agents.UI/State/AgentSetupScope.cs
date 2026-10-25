namespace Hexalith.Agents.UI.State;

/// <summary>A circuit-local scope lease; reference identity invalidates pages after an authentication change.</summary>
/// <param name="UserId">The authenticated subject.</param>
/// <param name="TenantId">The unambiguous authenticated tenant.</param>
/// <param name="AgentId">The configured Agent target.</param>
internal sealed record AgentSetupScope(string UserId, string TenantId, string AgentId);
