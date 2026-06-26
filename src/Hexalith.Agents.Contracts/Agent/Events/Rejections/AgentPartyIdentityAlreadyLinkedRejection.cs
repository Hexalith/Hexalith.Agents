namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// A <see cref="Commands.LinkAgentPartyIdentity"/> targeted an Agent (<c>hexa</c>) that already has a
/// <em>different</em> active Party identity (AC3; FR-2). The Agent can never hold more than one active Party
/// identity, so a second distinct link is rejected — changing identity requires the explicit
/// <see cref="Commands.ReplaceAgentPartyIdentity"/>. The current link is unchanged.
/// </summary>
/// <remarks>
/// Both the existing and the attempted ids are stable Parties-owned identifiers — <em>references, not PII</em>.
/// This rejection carries only the safe <see cref="AttemptedPartyId"/> (the rejected request's id); it never
/// carries a Party display name, contact value, personal identifier, or any Parties personal-data object (AC1;
/// AD-7, AD-14).
/// </remarks>
/// <param name="AgentId">The Agent aggregate identifier the link targeted.</param>
/// <param name="AttemptedPartyId">The stable identifier the rejected link attempted to set (a reference, not PII).</param>
public record AgentPartyIdentityAlreadyLinkedRejection(
    string AgentId,
    string AttemptedPartyId) : IRejectionEvent;
