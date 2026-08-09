using System;
using System.Linq;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Derives the trusted <see cref="AgentAdministrationContext"/> from the authenticated principal on the current
/// HTTP request (Story 5.2 AC4). Nothing is read from the request body or query string, so a caller cannot name a
/// tenant it does not belong to or assert administration authority it was not granted.
/// </summary>
/// <remarks>
/// The resolution is fail-closed at every step: no HTTP context, an unauthenticated principal, a missing tenant
/// claim, or a missing Agents-administrator role all yield <see cref="AgentAdministrationContext.Anonymous"/>,
/// which the operations surface turns into a not-authorized result before any mutation or lookup-dependent
/// disclosure.
/// </remarks>
public sealed class HttpAgentAdministrationContextProvider(
    IHttpContextAccessor httpContextAccessor) : IAgentAdministrationContextProvider
{
    /// <summary>The role granting Agent administration authority within the caller's tenant.</summary>
    public const string AgentsAdministratorRole = "Agents.Administrator";

    /// <summary>The claim types carrying the caller's tenant scope, in resolution order.</summary>
    private static readonly string[] _tenantClaimTypes = ["tenantId", "tenant_id", "tid", "tenant"];

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor
        ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <inheritdoc />
    public AgentAdministrationContext GetContext()
    {
        if (_httpContextAccessor.HttpContext?.User is not { Identity.IsAuthenticated: true } user)
        {
            return AgentAdministrationContext.Anonymous;
        }

        string tenantId = _tenantClaimTypes
            .Select(user.FindFirstValue)
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

        string actorUserId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        return new AgentAdministrationContext(
            tenantId,
            actorUserId,
            user.IsInRole(AgentsAdministratorRole));
    }
}
