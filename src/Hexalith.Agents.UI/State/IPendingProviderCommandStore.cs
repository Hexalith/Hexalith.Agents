namespace Hexalith.Agents.UI.State;

/// <summary>Browser-session boundary for unresolved provider governance command identities.</summary>
public interface IPendingProviderCommandStore
{
    /// <summary>Loads pending identities for the current browser session.</summary>
    Task<IReadOnlyList<PendingProviderCommand>> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a prepared intention before submission or acknowledges its exact receipt; retains it until exact terminal truth.</summary>
    Task SaveAsync(PendingProviderCommand command, CancellationToken cancellationToken = default);

    /// <summary>Removes a command only after exact terminal truth is known.</summary>
    Task RemoveAsync(string family, string resourceKey, string messageId, CancellationToken cancellationToken = default);
}
