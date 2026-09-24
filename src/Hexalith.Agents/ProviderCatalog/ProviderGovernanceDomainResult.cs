using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Agents.ProviderCatalog;

/// <summary>Reports the exact applied or no-op outcome of a provider governance command.</summary>
public sealed record ProviderGovernanceDomainResult : DomainResult
{
    private ProviderGovernanceDomainResult(IReadOnlyList<IEventPayload> events, string effect)
        : base(events)
    {
        ResultPayload = JsonSerializer.Serialize(new { effect });
    }

    /// <inheritdoc />
    public override string ResultPayload { get; }

    /// <summary>Reports an applied command whose event awaits projection.</summary>
    /// <param name="events">The events produced by the command.</param>
    /// <returns>The result with a command-specific effect.</returns>
    public static ProviderGovernanceDomainResult Applied(IReadOnlyList<IEventPayload> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count == 0)
        {
            throw new ArgumentException("An applied result requires an event.", nameof(events));
        }

        return new(events, "Applied");
    }

    /// <summary>Reports a successful command that produced no new event.</summary>
    /// <returns>The authoritative no-op result.</returns>
    public static ProviderGovernanceDomainResult AlreadyApplied() => new([], "AlreadyApplied");
}
