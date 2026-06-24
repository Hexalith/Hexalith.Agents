using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The kind of approver source declared in an Agent's Approver Policy for Confirmation Response Mode (AC2; AD-8).
/// These are the four V1 approver-source kinds. The <em>kind</em> is a policy declaration recorded at configuration
/// time; the concrete approving Party / Conversation is bound per Agent Call at runtime (Epic 3), never at config
/// time.
/// </summary>
/// <remarks>
/// <see cref="ConversationOwner"/> is resolved by the V1 facilitator-based resolver (AC2; AD-8) — current
/// Conversations contracts expose <c>ParticipantRole.Facilitator</c> but no owner field, so V1 treats conversation
/// "owner" authority as the Conversation Facilitator unless an explicit owner resolver is added. Serialized by name
/// so an absent value never deserializes to a concrete kind. <see cref="Unknown"/> (ordinal 0) is the sentinel.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApproverPolicySourceKind
{
    /// <summary>Absent/unrecognized source-kind sentinel — never a valid configured source.</summary>
    Unknown = 0,

    /// <summary>The caller (the Party that issued the Agent Call) is the approver; bound at call time.</summary>
    Caller,

    /// <summary>A specific predefined Party (a stable Parties-owned reference, not PII — AD-7) is the approver.</summary>
    PredefinedParty,

    /// <summary>Any holder of a named tenant role (resolved from the local Tenants projection) is an approver.</summary>
    TenantRole,

    /// <summary>The Conversation owner authority, resolved by the V1 facilitator-based resolver (AD-8); bound per Agent Call.</summary>
    ConversationOwner,
}
