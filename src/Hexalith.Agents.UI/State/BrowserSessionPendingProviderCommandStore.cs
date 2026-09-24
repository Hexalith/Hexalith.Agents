using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Hexalith.Agents.UI.State;

/// <summary>Stores only safe pending command identities in browser session storage.</summary>
public sealed class BrowserSessionPendingProviderCommandStore(
    IJSRuntime javascript,
    AuthenticationStateProvider authentication) : IPendingProviderCommandStore
{
    private const string StorageKeyPrefix = "hexalith.agents.provider-governance.pending.v1.";
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingProviderCommand>> LoadAsync(CancellationToken cancellationToken = default)
    {
        string key = await GetScopedStorageKeyAsync().ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReadAsync(key, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(PendingProviderCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        string key = await GetScopedStorageKeyAsync().ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<PendingProviderCommand> pending = await ReadAsync(key, cancellationToken).ConfigureAwait(false);
            pending.RemoveAll(item => item.Family == command.Family && item.ResourceKey == command.ResourceKey);
            pending.Add(command);
            await javascript.InvokeVoidAsync("sessionStorage.setItem", cancellationToken,
                key, JsonSerializer.Serialize(pending)).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string family, string resourceKey, string messageId, CancellationToken cancellationToken = default)
    {
        string key = await GetScopedStorageKeyAsync().ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<PendingProviderCommand> pending = await ReadAsync(key, cancellationToken).ConfigureAwait(false);
            pending.RemoveAll(item => item.Family == family && item.ResourceKey == resourceKey
                && item.Acceptance.MessageId == messageId);
            await javascript.InvokeVoidAsync("sessionStorage.setItem", cancellationToken,
                key, JsonSerializer.Serialize(pending)).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> GetScopedStorageKeyAsync()
    {
        AuthenticationState state = await authentication.GetAuthenticationStateAsync().ConfigureAwait(false);
        ClaimsPrincipal user = state.User;
        string? userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        string? tenantId = new[] { "tenantId", "tenant_id", "tid", "tenant" }
            .Select(type => user.FindFirst(type)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (string.IsNullOrWhiteSpace(tenantId) && user.IsInRole("Agents.PlatformOperator"))
        {
            tenantId = "system";
        }
        if (user.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId)
            || string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException("A current user and tenant are required for pending governance commands.");
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{tenantId}\0{userId}"));
        return StorageKeyPrefix + Convert.ToHexString(hash);
    }

    private async Task<List<PendingProviderCommand>> ReadAsync(string key, CancellationToken cancellationToken)
    {
        string? json = await javascript.InvokeAsync<string?>("sessionStorage.getItem", cancellationToken, key)
            .ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<PendingProviderCommand>>(json) ?? [];
    }
}
