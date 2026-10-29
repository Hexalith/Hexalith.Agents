namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// The trusted caller context an Agent administration operation runs under (Story 5.2 AC4). Every field is
/// server-derived from the authenticated principal — none of it may be accepted from a request payload, or a
/// caller could name another tenant or claim administration authority it does not hold.
/// </summary>
/// <param name="TenantId">The caller's tenant scope, or empty when no tenant could be established.</param>
/// <param name="ActorUserId">The authenticated user, or empty when the caller is anonymous.</param>
/// <param name="IsAgentsAdmin">Whether the caller holds Agent administration authority for <paramref name="TenantId"/>.</param>
/// <param name="IsPlatformOperator">Whether the caller holds Platform Operator authority over the platform provider catalog and tenant enablement.</param>
public sealed record AgentAdministrationContext(string TenantId, string ActorUserId, bool IsAgentsAdmin, bool IsPlatformOperator = false)
{
    /// <summary>The fail-closed context used when no authenticated administrator can be established.</summary>
    public static AgentAdministrationContext Anonymous { get; } = new(string.Empty, string.Empty, IsAgentsAdmin: false);

    /// <summary>Gets the transient lifetime of verified current authority, when supplied by the current-evidence adapter.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public AgentAuthorityValidity? Authority { get; init; }

    /// <summary>Gets whether a supplied lifetime still permits protected work; this bound never grants a role.</summary>
    public bool HasCurrentAuthority => Authority?.IsCurrent != false;

    /// <summary>Gets a value indicating whether this context may perform an Agent administration operation.</summary>
    public bool IsAuthorized
        => IsAgentsAdmin
            && HasCurrentAuthority
            && !string.IsNullOrWhiteSpace(TenantId)
            && !string.IsNullOrWhiteSpace(ActorUserId);
}

