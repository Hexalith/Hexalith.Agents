namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// How current the projected Agent setup read model is relative to the accepted configuration version
/// (Story 5.2 AC2). Mirrors the EventStore read-model freshness convention rather than inventing Agents-only
/// metadata.
/// </summary>
public enum AgentSetupFreshness
{
    /// <summary>Freshness could not be established (the fail-closed default).</summary>
    Unknown = 0,

    /// <summary>The projection reflects the accepted configuration version.</summary>
    Current = 1,

    /// <summary>The projection is behind the accepted configuration version.</summary>
    Stale = 2,
}
