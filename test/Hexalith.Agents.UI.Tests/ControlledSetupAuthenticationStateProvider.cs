using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Controls authentication notifications and their completion order for setup retention tests.</summary>
internal sealed class ControlledSetupAuthenticationStateProvider : AuthenticationStateProvider
{
    private Task<AuthenticationState> _state = Task.FromResult(State());

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _state;

    /// <summary>Publishes the supplied state, including deliberately incomplete or failing tasks.</summary>
    /// <param name="state">The next authentication state.</param>
    internal void Publish(Task<AuthenticationState> state)
    {
        _state = state;
        NotifyAuthenticationStateChanged(state);
    }

    /// <summary>Creates an authenticated, unambiguous setup binding.</summary>
    /// <param name="user">The subject.</param>
    /// <param name="tenant">The tenant.</param>
    /// <returns>The controlled authentication state.</returns>
    internal static AuthenticationState State(string user = "alice", string tenant = "tenant-a")
        => new(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", user), new Claim("tenantId", tenant)], "test")));
}
