using System;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// The trusted caller context an Agent administration operation runs under (Story 5.2 AC4). Every field is
/// server-derived from the authenticated principal — none of it may be accepted from a request payload, or a
/// caller could name another tenant or claim administration authority it does not hold.
/// </summary>
/// <param name="TenantId">The caller's tenant scope, or empty when no tenant could be established.</param>
/// <param name="ActorUserId">The authenticated user, or empty when the caller is anonymous.</param>
/// <param name="IsAgentsAdmin">Whether the caller holds Agent administration authority for <paramref name="TenantId"/>.</param>
public sealed record AgentAdministrationContext(string TenantId, string ActorUserId, bool IsAgentsAdmin)
{
    /// <summary>The fail-closed context used when no authenticated administrator can be established.</summary>
    public static AgentAdministrationContext Anonymous { get; } = new(string.Empty, string.Empty, IsAgentsAdmin: false);

    /// <summary>Gets a value indicating whether this context may perform an Agent administration operation.</summary>
    public bool IsAuthorized
        => IsAgentsAdmin
            && !string.IsNullOrWhiteSpace(TenantId)
            && !string.IsNullOrWhiteSpace(ActorUserId);
}

/// <summary>Resolves the trusted <see cref="AgentAdministrationContext"/> for the in-flight request.</summary>
public interface IAgentAdministrationContextProvider
{
    /// <summary>Resolves the current caller context, failing closed to <see cref="AgentAdministrationContext.Anonymous"/>.</summary>
    /// <returns>The trusted caller context.</returns>
    AgentAdministrationContext GetContext();
}
