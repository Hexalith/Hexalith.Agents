using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The trusted verdict classifying <em>which</em> dependency state was observed when validating (or provisioning)
/// the Party identity an Agent (<c>hexa</c>) is being linked to (AC2; FR-2, FR-21). The Parties validation/
/// provisioning runs in the Server application orchestration/adapter and its verdict is fed back to the pure
/// aggregate through a trusted, server-populated command envelope extension (AD-3, AD-12). The aggregate links the
/// Party only on <see cref="Valid"/> and otherwise records a typed rejection — failing closed on any other state.
/// </summary>
/// <remarks>
/// This verdict is safe by construction — it classifies the dependency state and carries no Party display name,
/// contact value, personal identifier, or any Parties personal-data object (AC1; AD-7, AD-14). Serialized by name
/// so an absent value never deserializes to a concrete verdict. <see cref="Unknown"/> (ordinal 0) is the fail-safe
/// sentinel: an absent/unparseable verdict is treated as "validation did not happen" and the link is rejected.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PartyLinkValidationStatus
{
    /// <summary>Absent/unrecognized verdict sentinel — treated as "validation did not happen" and fails closed.</summary>
    Unknown = 0,

    /// <summary>The Party exists, is active, and its projection is current — the link may proceed.</summary>
    Valid,

    /// <summary>No Party matched the supplied reference.</summary>
    Missing,

    /// <summary>The Party exists but is deactivated, erased, or restricted.</summary>
    Disabled,

    /// <summary>The reference resolved to more than one Party (ambiguous match).</summary>
    Ambiguous,

    /// <summary>The Party state could not be read freshly (stale/degraded projection or transport failure) — fail closed.</summary>
    Unavailable,

    /// <summary>The Party is outside the Agent's tenant scope or the caller is not authorized to read it.</summary>
    Unauthorized,
}
