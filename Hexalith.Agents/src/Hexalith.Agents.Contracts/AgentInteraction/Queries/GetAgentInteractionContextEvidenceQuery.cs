namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Requests the safe Conversation context evidence for the Agent Call (<c>AgentInteraction</c>) identified by
/// <see cref="AgentInteractionId"/> within the request's tenant scope (AC4; FR-25). Returns an
/// <see cref="AgentInteractionContextEvidenceResult"/> carrying the coarse status, the safe context evidence, and the
/// block reason — never the raw prompt, message text, claims, tokens, <c>PartyId</c> personal data, provider payloads,
/// or internal stream/projection identifiers (AD-14).
/// </summary>
/// <remarks>
/// Binding this query to the EventStore read path is deferred to the dedicated Agents read-model story (mirroring
/// <see cref="GetAgentInteractionGateEvidenceQuery"/>). The stable query/view contracts land here.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id) to inspect.</param>
public record GetAgentInteractionContextEvidenceQuery(
    string AgentInteractionId)
{
    /// <summary>The EventStore query domain.</summary>
    public const string Domain = "agent-interaction";

    /// <summary>The query type discriminator.</summary>
    public const string QueryType = "get-agent-interaction-context-evidence";
}
