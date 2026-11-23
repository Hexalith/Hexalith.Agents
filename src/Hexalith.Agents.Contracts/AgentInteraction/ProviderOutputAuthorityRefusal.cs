using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>Safe classification of an output-safety authority read that cannot authorize invocation.</summary>
[JsonConverter(typeof(UnknownFallbackEnumConverter<ProviderOutputAuthorityRefusal>))]
public enum ProviderOutputAuthorityRefusal
{
    /// <summary>No classified authority refusal was recorded.</summary>
    Unknown = 0,
    /// <summary>The authority decision is still open.</summary>
    Open = 1,
    /// <summary>The required authority decision is missing.</summary>
    Missing = 2,
    /// <summary>The authority evidence is malformed or inconsistent.</summary>
    Malformed = 3,
    /// <summary>The authority evidence is unavailable.</summary>
    Unavailable = 4,
}
