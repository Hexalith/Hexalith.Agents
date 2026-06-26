using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Lifecycle state of a governed Agent (<c>hexa</c>) exposed on the public status surface (FR-3, AC3; AD-2).
/// <see cref="Unknown"/> (ordinal 0) is the non-active sentinel so an absent or unrecognized state never
/// deserializes to <see cref="Active"/>. Serialized by name so a consuming service never treats a missing
/// status as callable.
/// </summary>
/// <remarks>
/// This story sets only <see cref="Draft"/> (on create), <see cref="Active"/> (on a gated activation), and
/// <see cref="Disabled"/>. Activation is partial in V1: it gates on this story's required fields only; later
/// stories (1.4–1.7) add party-identity, provider/model, response/approver, and content-safety gates onto the
/// same aggregate. The lifecycle state and the (future, fuller) readiness/blocker set are intentionally distinct
/// concepts so the later <c>agent-readiness-badge</c> never collapses "active lifecycle" with "callable".
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentLifecycleStatus
{
    /// <summary>Absent/unrecognized state sentinel — never callable.</summary>
    Unknown = 0,

    /// <summary>The Agent record exists but has not passed activation gates; not callable.</summary>
    Draft,

    /// <summary>The Agent passed this story's activation gates and is active (full callability accretes across the epic).</summary>
    Active,

    /// <summary>The Agent is disabled — not callable; prior history is preserved (AC3).</summary>
    Disabled,
}
