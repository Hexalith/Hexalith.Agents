using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Safe effect evidence returned for a completed Agent setup command.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentSetupWriteEffect
{
    /// <summary>No recognized command effect was supplied.</summary>
    Unknown = 0,

    /// <summary>The command appended an Agent setup event.</summary>
    Applied,

    /// <summary>The requested setup was already present and no event was appended.</summary>
    AlreadyApplied,
}
