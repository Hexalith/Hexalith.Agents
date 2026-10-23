using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>Content-free failure classification; output safety status comes only from the pinned Product authority.</summary>
[JsonConverter(typeof(UnknownFallbackEnumConverter<AutomaticInteractionFailureKind>))]
public enum AutomaticInteractionFailureKind
{
    /// <summary>Unknown failure classification.</summary>
    Unknown = 0,
    /// <summary>ProviderUnavailable failure classification.</summary>
    ProviderUnavailable = 1,
    /// <summary>ProviderError failure classification.</summary>
    ProviderError = 2,
    /// <summary>ProviderTimeout failure classification.</summary>
    ProviderTimeout = 3,
    /// <summary>OutputSafetyDenied failure classification.</summary>
    OutputSafetyDenied = 4,
    /// <summary>OutputSafetyUnavailable failure classification.</summary>
    OutputSafetyUnavailable = 5,
}
