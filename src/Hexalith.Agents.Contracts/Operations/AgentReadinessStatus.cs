using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>Canonical public Agent readiness terms used by API, client, and UI parity checks.</summary>
[JsonConverter(typeof(UnknownFallbackEnumConverter<AgentReadinessStatus>))]
public enum AgentReadinessStatus
{
    /// <summary>Absent or unrecognized readiness sentinel.</summary>
    Unknown = 0,

    /// <summary>The Agent is callable.</summary>
    Callable = 1,

    /// <summary>Readiness is being checked and is not success.</summary>
    Checking = 2,

    /// <summary>The Agent has invalid configuration.</summary>
    InvalidConfiguration = 3,

    /// <summary>The Agent is missing a Party identity.</summary>
    MissingPartyIdentity = 4,

    /// <summary>The selected provider/model is unavailable.</summary>
    ProviderUnavailable = 5,

    /// <summary>The Agent is disabled.</summary>
    Disabled = 6,
}
