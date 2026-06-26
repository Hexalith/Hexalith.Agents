using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>Canonical public Agent readiness terms used by API, client, and UI parity checks.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentReadinessStatus
{
    /// <summary>Absent or unrecognized readiness sentinel.</summary>
    Unknown = 0,

    /// <summary>The Agent is callable.</summary>
    Callable,

    /// <summary>Readiness is being checked and is not success.</summary>
    Checking,

    /// <summary>The Agent has invalid configuration.</summary>
    InvalidConfiguration,

    /// <summary>The Agent is missing a Party identity.</summary>
    MissingPartyIdentity,

    /// <summary>The selected provider/model is unavailable.</summary>
    ProviderUnavailable,

    /// <summary>The Agent is disabled.</summary>
    Disabled,
}
