using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Agents.Agent;

/// <summary>
/// Produces the bounded, replayable result payload used to correlate an Agent setup command with its authoritative
/// configuration version.
/// </summary>
internal sealed record AgentSetupDomainResult : DomainResult
{
    private AgentSetupDomainResult(
        IReadOnlyList<IEventPayload> events,
        AgentSetupWriteEffect effect,
        int configurationVersion)
        : base(Validate(events, effect, configurationVersion))
    {
        ResultPayload = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [AgentSetupResultPayload.EffectProperty] = effect.ToString(),
            [AgentSetupResultPayload.ConfigurationVersionProperty] = configurationVersion,
        });
    }

    /// <inheritdoc />
    public override string ResultPayload { get; }

    /// <summary>Creates a successful setup result with its resulting configuration version.</summary>
    /// <param name="events">The setup events appended by the command.</param>
    /// <param name="configurationVersion">The authoritative configuration version after applying the events.</param>
    /// <returns>The enriched domain result.</returns>
    public static AgentSetupDomainResult Applied(
        IReadOnlyList<IEventPayload> events,
        int configurationVersion)
        => new(events, AgentSetupWriteEffect.Applied, configurationVersion);

    /// <summary>Creates a no-op setup result with the unchanged authoritative configuration version.</summary>
    /// <param name="configurationVersion">The unchanged authoritative configuration version.</param>
    /// <returns>The enriched no-op domain result.</returns>
    public static AgentSetupDomainResult AlreadyApplied(int configurationVersion)
        => new([], AgentSetupWriteEffect.AlreadyApplied, configurationVersion);

    private static IReadOnlyList<IEventPayload> Validate(
        IReadOnlyList<IEventPayload> events,
        AgentSetupWriteEffect effect,
        int configurationVersion)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (configurationVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(configurationVersion),
                "The resulting configuration version must be positive.");
        }

        if (effect == AgentSetupWriteEffect.Applied && events.Count == 0)
        {
            throw new ArgumentException("An applied setup result requires at least one event.", nameof(events));
        }

        if (effect == AgentSetupWriteEffect.AlreadyApplied && events.Count != 0)
        {
            throw new ArgumentException("An already-applied setup result cannot contain events.", nameof(events));
        }

        return events;
    }
}
