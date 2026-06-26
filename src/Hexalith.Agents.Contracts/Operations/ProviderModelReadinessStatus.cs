using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>Canonical public provider/model readiness terms.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProviderModelReadinessStatus
{
    /// <summary>Absent or unrecognized provider/model status sentinel.</summary>
    Unknown = 0,

    /// <summary>The provider/model is enabled.</summary>
    Enabled,

    /// <summary>The provider/model is disabled.</summary>
    Disabled,

    /// <summary>The provider/model is degraded and must not be rendered as healthy success.</summary>
    Degraded,

    /// <summary>The provider/model has failed.</summary>
    Failed,

    /// <summary>The provider/model has no usable configuration.</summary>
    NotConfigured,
}
