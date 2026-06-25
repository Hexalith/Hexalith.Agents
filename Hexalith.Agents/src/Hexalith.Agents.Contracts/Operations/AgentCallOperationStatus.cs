using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>Canonical public Agent call status terms.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentCallOperationStatus
{
    /// <summary>Absent or unrecognized call status sentinel.</summary>
    Unknown = 0,

    /// <summary>The call was requested.</summary>
    Requested,

    /// <summary>The call was authorized.</summary>
    Authorized,

    /// <summary>The call was denied.</summary>
    Denied,

    /// <summary>Conversation context is loading and is not success.</summary>
    ContextLoading,

    /// <summary>Conversation context was blocked.</summary>
    ContextBlocked,

    /// <summary>Output generation is in progress and is not success.</summary>
    Generating,

    /// <summary>Output generation failed.</summary>
    GenerationFailed,

    /// <summary>Output was generated.</summary>
    Generated,
}
