namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal request to verify (and, when the seam exists, establish) the Agent as an <c>AiAgent</c> participant in
/// the Source Conversation before posting (Story 2.5; AC1; AD-6, AD-7). Carries safe inputs only — no generated content.
/// </summary>
/// <param name="TenantId">The interaction's tenant scope (cross-tenant membership is impossible by construction).</param>
/// <param name="SourceConversationId">The source Conversation reference the Agent must be a participant of (opaque — AD-6).</param>
/// <param name="AgentPartyId">The Agent's stable Party reference whose <c>AiAgent</c> participation is verified/established (a reference, not PII — AD-7).</param>
/// <param name="ActorPrincipalId">The caller security principal used for the Conversations authorized read.</param>
/// <param name="CorrelationId">The safe request correlation id threaded to the Conversations read.</param>
public sealed record ConversationMembershipRequest(
    string TenantId,
    string SourceConversationId,
    string AgentPartyId,
    string ActorPrincipalId,
    string CorrelationId);
