using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The fail-closed outcome of one invocation gate check (AC1; FR-21; AD-12). The five failure values
/// <see cref="Missing"/>/<see cref="Stale"/>/<see cref="Ambiguous"/>/<see cref="Disabled"/>/<see cref="Unavailable"/>
/// are AD-12's exact fail-closed vocabulary; <see cref="Unauthorized"/> is the access-denied outcome; only
/// <see cref="Satisfied"/> means the check passed. Any value other than <see cref="Satisfied"/> is a blocking verdict
/// — missing, stale, ambiguous, disabled, unavailable, or unauthorized state never resolves to a pass.
/// </summary>
/// <remarks>
/// An outcome is safe by construction — it is a coarse classification carrying no raw claims, tokens, Party personal
/// data, provider payloads, or content (AD-14). <see cref="Unavailable"/> is returned identically whether a record is
/// absent or cross-tenant, so a probe cannot learn whether a record exists in another tenant (AC3). Serialized by name
/// so an absent value never deserializes to a concrete outcome; <see cref="Unknown"/> (ordinal 0) is the fail-safe
/// sentinel (treated as a degraded read that fails closed).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentInteractionGateOutcome
{
    /// <summary>Absent/unrecognized outcome sentinel — treated as a degraded read and fails closed.</summary>
    Unknown = 0,

    /// <summary>The check passed — the dependency is present, fresh, and permitting.</summary>
    Satisfied,

    /// <summary>The required dependency record is absent.</summary>
    Missing,

    /// <summary>The dependency could not be read freshly (stale/degraded projection) — fail closed.</summary>
    Stale,

    /// <summary>The dependency reference resolved to more than one candidate (ambiguous state).</summary>
    Ambiguous,

    /// <summary>The dependency exists but is deactivated, erased, or restricted.</summary>
    Disabled,

    /// <summary>The dependency could not be read at all (transport failure, reader threw, or not-available) — fail closed.</summary>
    Unavailable,

    /// <summary>The caller is not authorized for the dependency, or it is outside the Agent's tenant scope (access denied).</summary>
    Unauthorized,
}
