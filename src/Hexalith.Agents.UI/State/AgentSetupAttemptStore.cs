using System.Security.Claims;

using Hexalith.Agents.UI.Services.Gateways;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Hexalith.Agents.UI.State;

/// <summary>
/// Retains one unresolved setup intent in circuit memory only. Binding changes and circuit disposal
/// release sensitive data. No browser storage, background submission or page callback is retained.
/// </summary>
internal sealed class AgentSetupAttemptStore : IDisposable
{
    private readonly AuthenticationStateProvider _authentication;
    private readonly IOptions<AgentSetupTargetOptions> _options;
    private readonly object _gate = new();
    private AgentSetupScope? _scope;
    private RetainedAgentSetupAttempt? _retained;
    private int _authenticationGeneration;
    private Task<AuthenticationState>? _suppliedAuthentication;
    private bool _authenticationPending;
    private bool _disposed;

    /// <summary>Creates a store bound to the circuit's authentication and Agent target.</summary>
    /// <param name="authentication">The circuit's authentication provider.</param>
    /// <param name="options">The same Agent target consumed by the live gateway.</param>
    public AgentSetupAttemptStore(AuthenticationStateProvider authentication, IOptions<AgentSetupTargetOptions> options)
    {
        _authentication = authentication;
        _options = options;
        _authentication.AuthenticationStateChanged += AuthenticationChanged;
    }

    /// <summary>Returns a lease only for one authenticated identity with agreeing subject and tenant aliases.</summary>
    /// <returns>The current scope, or null when binding cannot be established.</returns>
    internal Task<AgentSetupScope?> GetScopeAsync()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return Task.FromResult<AgentSetupScope?>(null);
            }

            Task<AuthenticationState> state;
            try
            {
                state = _suppliedAuthentication ?? _authentication.GetAuthenticationStateAsync();
            }
            catch (Exception)
            {
                ClearScope();
                return Task.FromResult<AgentSetupScope?>(null);
            }

            return ResolveAuthenticationAsync(state, _authenticationGeneration);
        }
    }

    /// <summary>Checks that an operation still owns the current authenticated scope and configured target.</summary>
    /// <param name="scope">The page's scope lease.</param>
    /// <returns>Whether the scope remains current.</returns>
    internal bool IsCurrent(AgentSetupScope? scope)
    {
        lock (_gate)
        {
            if (_scope is not null && _scope.AgentId != _options.Value.AgentId)
            {
                // An observed target change permanently invalidates this lease, even if the option changes back.
                _authenticationGeneration++;
                ClearScope();
            }

            return !_disposed && !_authenticationPending && scope is not null && ReferenceEquals(_scope, scope);
        }
    }

    /// <summary>Loads retained command data only within the current scope.</summary>
    /// <param name="scope">The page's scope lease.</param>
    /// <returns>The unresolved attempt, or null.</returns>
    internal RetainedAgentSetupAttempt? Load(AgentSetupScope? scope)
    {
        lock (_gate)
        {
            return IsCurrent(scope) ? _retained : null;
        }
    }

    /// <summary>Starts retention, refusing a second unresolved intent.</summary>
    /// <param name="scope">The page's scope lease.</param>
    /// <param name="state">The exact attempt and local draft state.</param>
    /// <returns>Whether the attempt was retained.</returns>
    internal bool TryBegin(AgentSetupScope? scope, RetainedAgentSetupAttempt state)
    {
        lock (_gate)
        {
            if (!IsCurrent(scope) || _retained is not null)
            {
                return false;
            }

            _retained = state;
            return true;
        }
    }

    /// <summary>Updates recovery evidence without allowing an old page to replace another attempt.</summary>
    /// <param name="scope">The page's scope lease.</param>
    /// <param name="state">The current receipt and draft state.</param>
    internal void Save(AgentSetupScope? scope, RetainedAgentSetupAttempt state)
    {
        lock (_gate)
        {
            if (IsCurrent(scope) && ReferenceEquals(_retained?.Attempt, state.Attempt))
            {
                _retained = state;
            }
        }
    }

    /// <summary>Releases retained sensitive data after resolution or explicit local abandonment.</summary>
    /// <param name="scope">The page's scope lease.</param>
    /// <param name="attempt">The attempt to release; this never cancels a server command.</param>
    internal void Release(AgentSetupScope? scope, AgentSetupAttempt? attempt)
    {
        lock (_gate)
        {
            if (IsCurrent(scope) && ReferenceEquals(_retained?.Attempt, attempt))
            {
                _retained = null;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _authentication.AuthenticationStateChanged -= AuthenticationChanged;
        lock (_gate)
        {
            _disposed = true;
            _authenticationGeneration++;
            ClearScope();
        }
    }

    private static string? ReadUniqueClaim(ClaimsIdentity? identity, params string[] aliases)
    {
        string[] values = identity?.Claims.Where(claim => aliases.Contains(claim.Type, StringComparer.Ordinal))
            .Select(claim => claim.Value).Distinct(StringComparer.Ordinal).ToArray() ?? [];
        return values.Length == 1 && !string.IsNullOrWhiteSpace(values[0]) ? values[0] : null;
    }

    private void AuthenticationChanged(Task<AuthenticationState> state)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _authenticationGeneration++;
            _suppliedAuthentication = state;
            _authenticationPending = true;
            // Pending authentication cannot use the old lease. Keep its data inaccessible until the supplied
            // state establishes whether this is an unchanged binding; fault/change releases it permanently.
            _ = ResolveAuthenticationAsync(state, _authenticationGeneration);
        }
    }

    private async Task<AgentSetupScope?> ResolveAuthenticationAsync(Task<AuthenticationState> state, int generation)
    {
        ClaimsPrincipal? user;
        try
        {
            user = (await state.ConfigureAwait(false)).User;
        }
        catch (Exception)
        {
            user = null;
        }

        lock (_gate)
        {
            if (_disposed || generation != _authenticationGeneration)
            {
                return null;
            }

            ClaimsIdentity[] identities = user?.Identities.Where(identity => identity.IsAuthenticated).ToArray() ?? [];
            ClaimsIdentity? identity = identities.Length == 1 ? identities[0] : null;
            string? userId = ReadUniqueClaim(identity, "sub", ClaimTypes.NameIdentifier);
            string? tenantId = ReadUniqueClaim(identity, "tenantId", "tenant_id", "tid", "tenant");
            string? agentId = _options.Value.AgentId;
            AgentSetupScope? candidate = userId is not null && tenantId is not null && !string.IsNullOrWhiteSpace(agentId)
                ? new(userId, tenantId, agentId) : null;
            if (_scope != candidate)
            {
                _retained = null;
                _scope = candidate;
            }

            _authenticationPending = false;
            return _scope;
        }
    }

    private void ClearScope()
    {
        _scope = null;
        _retained = null;
    }
}
