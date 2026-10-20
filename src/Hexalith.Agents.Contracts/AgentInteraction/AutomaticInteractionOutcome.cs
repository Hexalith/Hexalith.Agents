using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>Closed automatic-interaction vocabulary; zero never grants authority.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AutomaticInteractionOutcome>))]
public enum AutomaticInteractionOutcome
{
    /// <summary>Unknown classification.</summary>
    Unknown = 0,
    /// <summary>Authorized classification.</summary>
    Authorized = 1,
    /// <summary>Allowed classification.</summary>
    Allowed = 2,
    /// <summary>Blocked classification.</summary>
    Blocked = 3,
    /// <summary>DependencyNotAvailable classification.</summary>
    DependencyNotAvailable = 4,
    /// <summary>NotInvoked classification.</summary>
    NotInvoked = 5,
    /// <summary>InvocationSettlement classification.</summary>
    InvocationSettlement = 6,
    /// <summary>CancelNoInvocation classification.</summary>
    CancelNoInvocation = 7,
    /// <summary>Posted classification.</summary>
    Posted = 8,
    /// <summary>Discarded classification.</summary>
    Discarded = 9,
    /// <summary>Failed classification.</summary>
    Failed = 10,
    /// <summary>Suppressed classification.</summary>
    Suppressed = 11,
}
