using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// A specific reason an Agent (<c>hexa</c>) cannot be activated/made callable (AC2; FR-3). Returned both on the
/// activation rejection (so an administrator sees exactly what to fix) and on the public status view (so the
/// later <c>agent-readiness-badge</c> can explain a non-callable Agent). The blocker enum is deliberately
/// <em>additively extensible</em>: later stories add their own gates (party identity — 1.4, provider/model — 1.5,
/// response/approver — 1.6, content safety — 1.7) by appending new values without reshaping any event.
/// </summary>
/// <remarks>
/// Blockers are safe by construction — they classify <em>which</em> required field is missing/invalid and never
/// carry the raw Agent Instructions text (AD-14). Serialized by name so an absent value never deserializes to a
/// concrete blocker. <see cref="Unknown"/> (ordinal 0) is the unrecognized sentinel.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentActivationBlocker
{
    /// <summary>Absent/unrecognized blocker sentinel.</summary>
    Unknown = 0,

    /// <summary>The required display name is missing.</summary>
    MissingDisplayName,

    /// <summary>The required Agent Instructions are missing.</summary>
    MissingInstructions,

    /// <summary>The Agent Instructions are present but do not meet validity requirements.</summary>
    InvalidInstructions,

    /// <summary>No valid Party identity is linked, so the Agent has no attributable AI participant (AC2, AC4; 1.4).</summary>
    MissingPartyIdentity,

    /// <summary>No Provider/model has been selected yet, so the Agent has no model to call (AC2; 1.5).</summary>
    MissingProviderSelection,

    /// <summary>A Provider/model is selected but it is currently not selectable/ready — maps to the canonical UX <c>provider unavailable</c> readiness state (AC2; 1.5).</summary>
    ProviderUnavailable,

    /// <summary>No Response Mode has been chosen yet, so the Agent has no governed delivery policy (AC1; 1.6). Automatic mode requires no approver policy.</summary>
    MissingResponseMode,

    /// <summary>The Agent is in Confirmation mode but no approver source is configured, so confirmation has nothing to confirm with (AC3; 1.6).</summary>
    MissingApproverPolicy,

    /// <summary>The Agent is in Confirmation mode and a configured approver source is missing/disabled/ambiguous/unavailable/unauthorized — fails closed (AC3; 1.6).</summary>
    ApproverPolicyUnresolvable,
}
