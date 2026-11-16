using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Hexalith.Agents.UI.State;

/// <summary>Stores only safe pending command identities in browser session storage.</summary>
public sealed class BrowserSessionPendingProviderCommandStore : IPendingProviderCommandStore, IDisposable
{
    private const string StorageKeyPrefix = "hexalith.agents.provider-governance.pending.v1.";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IJSRuntime _javascript;
    private readonly AuthenticationStateProvider _authentication;
    private readonly Dictionary<string, (string Key, long Generation)> _commandScopes = new(StringComparer.Ordinal);
    private long _authenticationGeneration;
    private bool _disposed;

    /// <summary>Creates a store and observes every authentication transition, including changes back.</summary>
    public BrowserSessionPendingProviderCommandStore(IJSRuntime javascript, AuthenticationStateProvider authentication)
    {
        _javascript = javascript ?? throw new ArgumentNullException(nameof(javascript));
        _authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
        _authentication.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingProviderCommand>> LoadAsync(CancellationToken cancellationToken = default)
    {
        (string key, long generation) = await CaptureScopeAsync().ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await GuardScopeAsync(key, generation, cancellationToken).ConfigureAwait(false);
            List<PendingProviderCommand> pending = await ReadAsync(key, generation, cancellationToken).ConfigureAwait(false);
            await GuardScopeAsync(key, generation, cancellationToken).ConfigureAwait(false);
            foreach (PendingProviderCommand command in pending)
            {
                _commandScopes.TryAdd(command.Acceptance.MessageId, (key, generation));
            }
            return pending;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(PendingProviderCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command);
        (string key, long generation) = await CaptureScopeAsync().ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await GuardScopeAsync(key, generation, cancellationToken).ConfigureAwait(false);
            GuardCommandScope(command.Acceptance.MessageId, key, generation);
            List<PendingProviderCommand> pending = await ReadAsync(key, generation, cancellationToken).ConfigureAwait(false);
            PendingProviderCommand? existing = pending.SingleOrDefault(item => item.Family == command.Family && item.ResourceKey == command.ResourceKey);
            if (existing is not null && (existing.TenantId != command.TenantId
                || existing.Acceptance.MessageId != command.Acceptance.MessageId
                || existing.Acceptance.CorrelationId != command.Acceptance.CorrelationId))
            {
                throw new InvalidOperationException("An unresolved governance command already locks this resource.");
            }
            pending.RemoveAll(item => item.Family == command.Family && item.ResourceKey == command.ResourceKey);
            pending.Add(command);
            await WriteAsync(key, generation, pending, cancellationToken).ConfigureAwait(false);
            _commandScopes.TryAdd(command.Acceptance.MessageId, (key, generation));
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string family, string resourceKey, string messageId, CancellationToken cancellationToken = default)
    {
        (string key, long generation) = await CaptureScopeAsync().ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await GuardScopeAsync(key, generation, cancellationToken).ConfigureAwait(false);
            GuardCommandScope(messageId, key, generation);
            List<PendingProviderCommand> pending = await ReadAsync(key, generation, cancellationToken).ConfigureAwait(false);
            pending.RemoveAll(item => item.Family == family && item.ResourceKey == resourceKey
                && item.Acceptance.MessageId == messageId);
            await WriteAsync(key, generation, pending, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        Interlocked.Increment(ref _authenticationGeneration);
        _authentication.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> state)
        => Interlocked.Increment(ref _authenticationGeneration);

    private async Task<(string Key, long Generation)> CaptureScopeAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        long generation = Interlocked.Read(ref _authenticationGeneration);
        string key = await GetScopedStorageKeyAsync().ConfigureAwait(false);
        EnsureGeneration(generation);
        return (key, generation);
    }

    private void GuardCommandScope(string messageId, string key, long generation)
    {
        if (_commandScopes.TryGetValue(messageId, out (string Key, long Generation) scope)
            && (scope.Key != key || scope.Generation != generation))
        {
            throw new InvalidOperationException("The pending governance command belongs to an earlier authentication scope.");
        }
    }

    private void EnsureGeneration(long generation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (generation != Interlocked.Read(ref _authenticationGeneration))
        {
            throw new InvalidOperationException("Authentication changed during a pending governance command operation.");
        }
    }

    private async Task GuardScopeAsync(string key, long generation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureGeneration(generation);
        string currentKey = await GetScopedStorageKeyAsync().ConfigureAwait(false);
        EnsureGeneration(generation);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(key, currentKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Authentication changed during a pending governance command operation.");
        }
    }

    [SuppressMessage("Usage", "BL0013", Justification = "This non-rendering store reads current state and subscribes to transitions to guard asynchronous storage operations.")]
    private async Task<string> GetScopedStorageKeyAsync()
    {
        AuthenticationState state = await _authentication.GetAuthenticationStateAsync().ConfigureAwait(false);
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

    private async Task<List<PendingProviderCommand>> ReadAsync(string key, long generation, CancellationToken cancellationToken)
    {
        try
        {
            await GuardScopeAsync(key, generation, cancellationToken).ConfigureAwait(false);
            string? json = await _javascript.InvokeAsync<string?>("sessionStorage.getItem", cancellationToken, key).ConfigureAwait(false);
            await GuardScopeAsync(key, generation, cancellationToken).ConfigureAwait(false);
            if (json is null)
            {
                return [];
            }
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Pending governance command storage is corrupt.");
            }
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || HasDuplicateProperties(item)
                    || !item.TryGetProperty("Acceptance", out JsonElement identity) || identity.ValueKind != JsonValueKind.Object || HasDuplicateProperties(identity)
                    || !identity.TryGetProperty("TruthState", out JsonElement truth) || !ValidEnum<AgentSetupTruthState>(truth)
                    || item.TryGetProperty("ExpectedStatus", out JsonElement status)
                        && status.ValueKind != JsonValueKind.Null && !ValidEnum<ProviderModelStatus>(status))
                {
                    throw new InvalidOperationException("Pending governance command storage is corrupt.");
                }
            }
            List<PendingProviderCommand> pending = JsonSerializer.Deserialize<List<PendingProviderCommand>>(json)
                ?? throw new InvalidOperationException("Pending governance command storage is corrupt.");
            Dictionary<(string Family, string ResourceKey), PendingProviderCommand> unique = [];
            foreach (PendingProviderCommand command in pending)
            {
                Validate(command);
                var resource = (command.Family, command.ResourceKey);
                if (unique.TryGetValue(resource, out PendingProviderCommand? previous) && previous != command)
                {
                    throw new InvalidOperationException("Pending governance command storage has conflicting resource locks.");
                }
                unique[resource] = command;
            }
            return [.. unique.Values];
        }
        catch (JSException exception)
        {
            throw new InvalidOperationException("Pending governance commands could not be read from browser session storage.", exception);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Pending governance command storage is corrupt.", exception);
        }
    }

    private async Task WriteAsync(string key, long generation, List<PendingProviderCommand> pending, CancellationToken cancellationToken)
    {
        try
        {
            await GuardScopeAsync(key, generation, cancellationToken).ConfigureAwait(false);
            await _javascript.InvokeVoidAsync("sessionStorage.setItem", cancellationToken,
                key, JsonSerializer.Serialize(pending)).ConfigureAwait(false);
            await GuardScopeAsync(key, generation, cancellationToken).ConfigureAwait(false);
        }
        catch (JSException exception)
        {
            throw new InvalidOperationException("Pending governance commands could not be saved to browser session storage.", exception);
        }
    }

    private static void Validate(PendingProviderCommand command)
    {
        if (command is null || command.Acceptance is not { } identity
            || string.IsNullOrWhiteSpace(command.TenantId)
            || string.IsNullOrWhiteSpace(identity.ProviderId) || string.IsNullOrWhiteSpace(identity.ModelId)
            || !CanonicalIdentity(identity.MessageId) || !CanonicalIdentity(identity.CorrelationId)
            || !Enum.IsDefined(identity.TruthState)
            || (command.IsPrepared ? identity.TruthState != AgentSetupTruthState.Unknown
                : identity.TruthState is not (AgentSetupTruthState.Submitted or AgentSetupTruthState.AuthoritativePending or AgentSetupTruthState.ProjectionConfirmed))
            || command.ExpectedCapabilityVersion is <= 0
            || command.ExpectedStatus is { } status && status is not (ProviderModelStatus.Enabled or ProviderModelStatus.Disabled))
        {
            throw new InvalidOperationException("Pending governance command storage contains an invalid command.");
        }
        string entryKey = ProviderCatalogIdentity.EntryId(identity.ProviderId, identity.ModelId);
        bool valid = command.Family switch
        {
            "ProviderCatalogMutation" => command.TenantId == "system" && command.ResourceKey == entryKey,
            "TenantProviderEnablement" => command.TenantId is not ("current" or "system")
                && command.ResourceKey == $"{command.TenantId}:{entryKey}"
                && command.ExpectedCapabilityVersion is null && command.ExpectedStatus is null,
            "DataHandlingAcceptance" => command.TenantId != "system" && command.ResourceKey == entryKey
                && command.ExpectedCapabilityVersion is null && command.ExpectedStatus is null,
            _ => false,
        };
        if (!valid)
        {
            throw new InvalidOperationException("Pending governance command storage contains an invalid resource lock.");
        }
    }

    private static bool HasDuplicateProperties(JsonElement element)
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        return element.EnumerateObject().Any(property => !names.Add(property.Name));
    }

    private static bool ValidEnum<T>(JsonElement element) where T : struct, Enum
        => element.ValueKind switch
        {
            JsonValueKind.String => Enum.TryParse(element.GetString(), ignoreCase: true, out T parsed) && Enum.IsDefined(parsed)
                && (string.Equals(Enum.GetName(parsed), element.GetString(), StringComparison.OrdinalIgnoreCase)
                    || Convert.ToInt32(parsed).ToString(System.Globalization.CultureInfo.InvariantCulture) == element.GetString()),
            JsonValueKind.Number => element.TryGetInt32(out int ordinal) && Enum.IsDefined(typeof(T), ordinal),
            _ => false,
        };

    private static bool CanonicalIdentity(string? value)
        => value is { Length: 26 } && NUlid.Ulid.TryParse(value, out NUlid.Ulid parsed)
            && string.Equals(value, parsed.ToString(), StringComparison.Ordinal);
}
