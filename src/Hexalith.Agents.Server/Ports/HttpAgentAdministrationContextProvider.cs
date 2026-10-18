using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Authenticates scope/subject, then selects one principal using current Tenants and Parties evidence.</summary>
public sealed class HttpAgentAdministrationContextProvider(IHttpContextAccessor httpContextAccessor,
    ICurrentTenantAuthoritySource? tenantSource = null, ICurrentHumanActorSource? actorSource = null,
    IAgentDependencyAvailability? availability = null, TimeProvider? clock = null) : IAgentAdministrationContextProvider
{
    /// <summary>Gets the tenant administration operation role.</summary>
    public const string AgentsAdministratorRole = "Agents.Administrator";
    /// <summary>Gets the global administration operation role.</summary>
    public const string PlatformOperatorRole = "Agents.PlatformOperator";
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    /// <inheritdoc />
    public AgentAdministrationContext GetContext()
    {
        ClaimsIdentity[] identities = httpContextAccessor.HttpContext?.User.Identities.Where(item => item.IsAuthenticated).ToArray() ?? [];
        if (identities.Length != 1)
        {
            return AgentAdministrationContext.Anonymous;
        }

        string[] tenants = identities[0].Claims.Where(item => item.Type is "tenantId" or "tenant_id" or "tid" or "tenant")
            .Select(item => item.Value).Distinct(StringComparer.Ordinal).ToArray();
        string[] subjects = identities[0].Claims.Where(item => item.Type is "sub" or ClaimTypes.NameIdentifier)
            .Select(item => item.Value).Distinct(StringComparer.Ordinal).ToArray();
        return tenants.Length <= 1 && subjects.Length == 1 && !string.IsNullOrWhiteSpace(subjects[0])
            ? new(tenants.SingleOrDefault() ?? string.Empty, subjects[0], false)
            : AgentAdministrationContext.Anonymous;
    }

    /// <inheritdoc />
    public async ValueTask<AgentAdministrationContext> GetContextAsync(bool platform, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgentAdministrationContext identity = GetContext();
        if (tenantSource is null || actorSource is null || availability is null
            || availability.Check("EXT-HOST-1") is not null || availability.Check("EXT-PARTIES-1") is not null
            || string.IsNullOrWhiteSpace(identity.ActorUserId) || string.IsNullOrWhiteSpace(identity.TenantId))
        {
            return identity;
        }

        try
        {
            DateTimeOffset started = _clock.GetUtcNow();
            CurrentHumanActorEvidence? actor = await actorSource.ReadAsync(identity.TenantId, identity.ActorUserId, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (actor?.IsCurrentFor(identity.TenantId, identity.ActorUserId, null, started, _clock.GetUtcNow()) != true)
            {
                return identity;
            }

            TenantAuthorityEvidence? roles = await tenantSource.ReadAsync(platform ? "system" : identity.TenantId, platform, cancellationToken).ConfigureAwait(false);
            DateTimeOffset now = _clock.GetUtcNow();
            cancellationToken.ThrowIfCancellationRequested();
            if (!actor.IsCurrentFor(identity.TenantId, identity.ActorUserId, null, started, now)
                || !ProjectedTenantAccessReader.Grants(roles, platform ? "system" : identity.TenantId, actor.ActorId,
                    platform ? PlatformOperatorRole : AgentsAdministratorRole, null, platform, now, started))
            {
                return identity;
            }

            return new(platform ? "system" : identity.TenantId, actor.ActorId, !platform, platform);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return identity;
        }
    }
}
