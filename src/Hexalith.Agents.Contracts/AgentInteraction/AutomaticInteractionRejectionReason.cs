using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>Closed automatic-interaction vocabulary; zero never grants authority.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AutomaticInteractionRejectionReason>))]
public enum AutomaticInteractionRejectionReason
{
    /// <summary>Unknown classification.</summary>
    Unknown = 0,
    /// <summary>InvalidTarget classification.</summary>
    InvalidTarget = 1,
    /// <summary>InvalidPermit classification.</summary>
    InvalidPermit = 2,
    /// <summary>LegacyStream classification.</summary>
    LegacyStream = 3,
    /// <summary>DivergentDuplicate classification.</summary>
    DivergentDuplicate = 4,
    /// <summary>StaleRevision classification.</summary>
    StaleRevision = 5,
    /// <summary>InvalidPhase classification.</summary>
    InvalidPhase = 6,
    /// <summary>LeaseCommitRequired classification.</summary>
    LeaseCommitRequired = 7,
    /// <summary>ProtectedContentRequired classification.</summary>
    ProtectedContentRequired = 8,
    /// <summary>InvalidAuthority classification.</summary>
    InvalidAuthority = 9,
    /// <summary>Terminal classification.</summary>
    Terminal = 10,
}
