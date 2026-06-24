using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The trusted verdict classifying <em>which</em> provider-readiness state was observed when validating a
/// Provider/model selection (or re-validating a recorded selection at activation) for an Agent (<c>hexa</c>)
/// (AC2; FR-5, FR-21). The catalog read runs in the Server application orchestration/adapter and its verdict is
/// fed back to the pure aggregate through a trusted, server-populated command envelope extension (AD-3, AD-9,
/// AD-12). The aggregate records the selection only on <see cref="Valid"/> and otherwise records a typed
/// rejection — failing closed on any other state, with no provider SDK call or credential access.
/// </summary>
/// <remarks>
/// This verdict is safe by construction — it classifies the provider-readiness state and carries no secret value,
/// configuration reference, capability-metadata blob, or provider SDK type (AC1; AD-9, AD-14). Serialized by name
/// so an absent value never deserializes to a concrete verdict. <see cref="Unknown"/> (ordinal 0) is the fail-safe
/// sentinel: an absent/unparseable verdict is treated as "validation did not happen" and the selection is rejected.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProviderSelectionValidationStatus
{
    /// <summary>Absent/unrecognized verdict sentinel — treated as "validation did not happen" and fails closed.</summary>
    Unknown = 0,

    /// <summary>The Provider/model is enabled, configured, text-generation capable, and has valid capability metadata — the selection may proceed.</summary>
    Valid,

    /// <summary>The Provider/model entry exists but is not enabled (disabled/degraded/failed) — not selectable for new active use.</summary>
    Disabled,

    /// <summary>No catalog entry matched the supplied Provider/model reference.</summary>
    Missing,

    /// <summary>The Provider/model is enabled but has no safe configuration reference associated (not configured).</summary>
    NotConfigured,

    /// <summary>The Provider/model does not support text generation (the required V1 capability floor — AD-10).</summary>
    NotTextGenerationCapable,

    /// <summary>The Provider/model lacks required context/output/timeout capability metadata.</summary>
    MissingCapabilityMetadata,

    /// <summary>The caller is outside the catalog's tenant scope or is not authorized to read it (AC4).</summary>
    Unauthorized,

    /// <summary>The catalog state could not be read freshly (stale/degraded projection or transport failure) — fail closed.</summary>
    Unavailable,
}
