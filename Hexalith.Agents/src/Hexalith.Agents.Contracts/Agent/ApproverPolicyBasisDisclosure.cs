using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The FR-7 disclosure category recorded with an Approver Policy, governing how the policy basis is reported in
/// later proposal/approval surfaces (Epic 3) (AC4). It is a safe classification only — it carries no secret and no
/// content (AD-14); it controls how <em>later</em> proposal-basis is shown, consistently across API/client
/// contracts and future UI surfaces.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never deserializes to a concrete category. <see cref="Unknown"/>
/// (ordinal 0) is the sentinel — a configured policy must record an explicit disclosure category.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApproverPolicyBasisDisclosure
{
    /// <summary>Absent/unrecognized disclosure-category sentinel.</summary>
    Unknown = 0,

    /// <summary>The policy basis may be shown to end users on user-visible surfaces.</summary>
    UserVisible,

    /// <summary>The policy basis is shown only to operators/administrators, not end users.</summary>
    OperatorOnly,

    /// <summary>The policy basis is shown in a redacted form.</summary>
    Redacted,

    /// <summary>The policy basis is omitted from reporting surfaces entirely.</summary>
    Omitted,
}
