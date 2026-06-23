using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>
/// Provider/model lifecycle and health status exposed on the public read surface (AD-9, AD-10; UX
/// Provider-And-Model). <see cref="Unknown"/> (ordinal 0) is the non-active sentinel so an absent or
/// unrecognized status never deserializes to an active state. Serialized by name so consuming services
/// never treat a missing status as enabled.
/// </summary>
/// <remarks>
/// Story 1.2 sets only <see cref="Enabled"/> / <see cref="Disabled"/> through the catalog aggregate.
/// <see cref="Degraded"/> and <see cref="Failed"/> are reserved for the later runtime-health stories and are
/// part of the stable surface so the future Provider catalog grid does not need a contract change.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProviderModelStatus
{
    /// <summary>Absent/unrecognized status sentinel — never selectable for active use.</summary>
    Unknown = 0,

    /// <summary>Entry is enabled and selectable for new active Agent configuration.</summary>
    Enabled,

    /// <summary>Entry is disabled — not selectable for new active use, history preserved (AC2).</summary>
    Disabled,

    /// <summary>Entry is degraded (reserved for runtime-health stories).</summary>
    Degraded,

    /// <summary>Entry has failed (reserved for runtime-health stories).</summary>
    Failed,
}
