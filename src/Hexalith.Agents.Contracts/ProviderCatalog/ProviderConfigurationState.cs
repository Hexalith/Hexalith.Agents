using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>
/// Safe configured-state of a provider/model entry (AD-9, AD-14). Conveys whether a secret configuration
/// reference has been associated with the entry — <em>without</em> exposing any secret value. Serialized by
/// name so an absent value never deserializes to <see cref="Configured"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProviderConfigurationState
{
    /// <summary>Absent/unrecognized sentinel — treated as not configured.</summary>
    Unknown = 0,

    /// <summary>No safe configuration reference is associated with the entry.</summary>
    NotConfigured,

    /// <summary>A safe configuration reference is associated with the entry (no secret value is exposed).</summary>
    Configured,
}
