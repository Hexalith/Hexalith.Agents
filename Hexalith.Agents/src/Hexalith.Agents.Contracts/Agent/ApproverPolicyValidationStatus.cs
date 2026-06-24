using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The trusted verdict classifying <em>which</em> approver-policy readiness state was observed when evaluating a
/// Confirmation-mode Agent's Approver Policy for activation (AC3; FR-21). The per-source resolution (Parties /
/// Tenants projection / Conversations facilitator) runs in the Server application orchestration/adapter and its
/// aggregated verdict is fed back to the pure aggregate through a trusted, server-populated command envelope
/// extension (AD-3, AD-8, AD-12). Activation clears the approver gate only on <see cref="Valid"/> and otherwise
/// fails closed — a missing/stale/ambiguous/disabled/unavailable/cross-tenant source never resolves to callable.
/// </summary>
/// <remarks>
/// This verdict is safe by construction — it classifies the readiness state and carries no secret, no Party PII,
/// and no provider/SDK type (AD-7, AD-9, AD-14). Serialized by name so an absent value never deserializes to a
/// concrete verdict. <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel: an absent/unparseable verdict is
/// treated as "validation did not happen" and blocks Confirmation-mode activation.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApproverPolicyValidationStatus
{
    /// <summary>Absent/unrecognized verdict sentinel — treated as "validation did not happen" and fails closed.</summary>
    Unknown = 0,

    /// <summary>Every configured approver source resolved cleanly — the policy is ready and activation may proceed.</summary>
    Valid,

    /// <summary>No approver source is configured at all (an empty policy) — confirmation mode has nothing to confirm with.</summary>
    Incomplete,

    /// <summary>A configured source resolved to nothing (e.g. no such tenant role / no such Party).</summary>
    Missing,

    /// <summary>A configured source exists but is deactivated, erased, or restricted.</summary>
    Disabled,

    /// <summary>A configured source reference resolved to more than one candidate (ambiguous authority).</summary>
    Ambiguous,

    /// <summary>A configured source could not be read freshly (stale/degraded projection or resolver unavailable) — fail closed.</summary>
    Unavailable,

    /// <summary>A configured source is outside the Agent's tenant scope or the caller is not authorized to read it.</summary>
    Unauthorized,
}
