using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// How an Agent's Content Safety Policy treats a safety failure on generated content (Story 1.7 AC1; FR-27). It is a
/// safe governance classification only — it carries no secret and no unsafe content (AD-14). It records the
/// <em>governance choice</em>; <b>enforcement</b> of the choice (blocking unsafe output, gating an approver override)
/// is deferred to Epic 2 / Epic 3 (Story 2.4 / Story 3.5) — this story only records the choice.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never deserializes to a concrete handling. <see cref="Unknown"/>
/// (ordinal 0) is the fail-safe sentinel — it <b>cannot be configured</b>: a defined policy must specify how a safety
/// failure is handled.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentSafetyFailureHandling
{
    /// <summary>Absent/unrecognized fail-safe sentinel — cannot be configured (a defined policy must choose a handling).</summary>
    Unknown = 0,

    /// <summary>Failed content is blocked and audited with no override path.</summary>
    BlockAndAudit,

    /// <summary>Failed content is blocked, but the policy defines an auditable override path an Approver may use (FR-27; enforced in Story 3.5).</summary>
    BlockWithAuditableOverride,
}
