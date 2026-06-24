using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// How an Agent's Content Safety Policy records audit evidence of a safety failure (Story 1.7 AC1; AD-14, FR-24). It
/// is a safe governance classification only — it carries no secret and no unsafe content (AD-14). It records the
/// <em>governance choice</em>; <b>enforcement</b> (writing the audit evidence) is deferred to Epic 2 / Epic 4
/// (Story 2.4 / Story 4.2) — this story only records the choice.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never deserializes to a concrete treatment. <see cref="Unknown"/>
/// (ordinal 0) is the fail-safe sentinel — it <b>cannot be configured</b>: a defined policy must specify how a safety
/// failure is audited.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentSafetyAuditTreatment
{
    /// <summary>Absent/unrecognized fail-safe sentinel — cannot be configured (a defined policy must choose a treatment).</summary>
    Unknown = 0,

    /// <summary>Audit records only the safety-failure classification/metadata, never the unsafe content (default safe; AD-14).</summary>
    MetadataOnly,

    /// <summary>Audit may record a redacted excerpt under EventStore payload-protection (enforced in Story 4.2).</summary>
    RedactedExcerpt,
}
