namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// A single declared approver source in an Agent's Approver Policy (AC2; AD-8). It carries ONLY safe references —
/// the source <see cref="Kind"/>, an optional stable <see cref="PartyId"/> (a Parties-owned <em>reference, not
/// PII</em> — AD-7), and an optional safe <see cref="TenantRole"/> name. No Party display name, contact value,
/// personal identifier, or any personal-data object is carried here (AD-7, AD-14).
/// </summary>
/// <remarks>
/// The shape is constrained by the <see cref="Kind"/>: for <see cref="ApproverPolicySourceKind.Caller"/> and
/// <see cref="ApproverPolicySourceKind.ConversationOwner"/> both <see cref="PartyId"/> and <see cref="TenantRole"/>
/// are <see langword="null"/>; for <see cref="ApproverPolicySourceKind.PredefinedParty"/> only <see cref="PartyId"/>
/// is set; for <see cref="ApproverPolicySourceKind.TenantRole"/> only <see cref="TenantRole"/> is set. The aggregate
/// validates this structurally on configuration.
/// </remarks>
/// <param name="Kind">The approver source kind.</param>
/// <param name="PartyId">The stable predefined-Party reference (a reference, not PII — AD-7), or <see langword="null"/>.</param>
/// <param name="TenantRole">The safe tenant role name, or <see langword="null"/>.</param>
public record ApproverPolicySource(
    ApproverPolicySourceKind Kind,
    string? PartyId,
    string? TenantRole);
