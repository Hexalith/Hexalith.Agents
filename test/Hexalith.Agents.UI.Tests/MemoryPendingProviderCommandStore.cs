using Hexalith.Agents.UI.State;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Session-like pending command store for component tests.</summary>
public sealed class MemoryPendingProviderCommandStore : IPendingProviderCommandStore
{
    private readonly Dictionary<(string Family, string ResourceKey), PendingProviderCommand> _items = [];

    /// <inheritdoc />
    public Task<IReadOnlyList<PendingProviderCommand>> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PendingProviderCommand>>([.. _items.Values]);

    /// <inheritdoc />
    public Task SaveAsync(PendingProviderCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        _items[(command.Family, command.ResourceKey)] = command;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string family, string resourceKey, string messageId, CancellationToken cancellationToken = default)
    {
        if (_items.TryGetValue((family, resourceKey), out PendingProviderCommand? item)
            && item.Acceptance.MessageId == messageId)
        {
            _items.Remove((family, resourceKey));
        }

        return Task.CompletedTask;
    }
}
