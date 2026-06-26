using System;
using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>
/// Finite, allow-listed set of safe optional provider/model capability flags (AD-10 capability floor).
/// Free-form provider-specific settings are intentionally not representable here: provider-specific knobs
/// remain adapter-local until a future architecture decision promotes them. Serialized by name.
/// </summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProviderModelCapabilityFlags
{
    /// <summary>No optional capabilities beyond the required floor.</summary>
    None = 0,

    /// <summary>The model supports streamed token output.</summary>
    Streaming = 1,

    /// <summary>The model supports tool/function calling.</summary>
    ToolCalling = 1 << 1,

    /// <summary>The model supports image/vision input.</summary>
    Vision = 1 << 2,

    /// <summary>The model supports structured (schema-constrained) output.</summary>
    StructuredOutput = 1 << 3,
}
