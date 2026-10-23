using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>Canonical public provider/model readiness terms.</summary>
[JsonConverter(typeof(UnknownFallbackEnumConverter<ProviderModelReadinessStatus>))]
public enum ProviderModelReadinessStatus
{
    /// <summary>Absent or unrecognized provider/model status sentinel.</summary>
    Unknown = 0,

    /// <summary>The provider/model is enabled.</summary>
    Enabled = 1,

    /// <summary>The provider/model is disabled.</summary>
    Disabled = 2,

    /// <summary>The provider/model is degraded and must not be rendered as healthy success.</summary>
    Degraded = 3,

    /// <summary>The provider/model has failed.</summary>
    Failed = 4,

    /// <summary>The provider/model has no usable configuration.</summary>
    NotConfigured = 5,
}
