using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>Canonical public Agent call status terms.</summary>
[JsonConverter(typeof(UnknownFallbackEnumConverter<AgentCallOperationStatus>))]
public enum AgentCallOperationStatus
{
    /// <summary>Absent or unrecognized call status sentinel.</summary>
    Unknown = 0,

    /// <summary>The call was requested.</summary>
    Requested = 1,

    /// <summary>The call was authorized.</summary>
    Authorized = 2,

    /// <summary>The call was denied.</summary>
    Denied = 3,

    /// <summary>Conversation context is loading and is not success.</summary>
    ContextLoading = 4,

    /// <summary>Conversation context was blocked.</summary>
    ContextBlocked = 5,

    /// <summary>Output generation is in progress and is not success.</summary>
    Generating = 6,

    /// <summary>Output generation failed.</summary>
    GenerationFailed = 7,

    /// <summary>Output was generated.</summary>
    Generated = 8,
}
