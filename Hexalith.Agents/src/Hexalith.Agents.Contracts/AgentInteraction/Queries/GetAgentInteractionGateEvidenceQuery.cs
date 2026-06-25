namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Requests the safe invocation gate evidence for the Agent Call (<c>AgentInteraction</c>) identified by
/// <see cref="AgentInteractionId"/> within the request's tenant scope (AC4; FR-24). Returns an
/// <see cref="AgentInteractionGateEvidenceResult"/> carrying the coarse status and the safe per-check verdicts — never
/// the raw prompt, claims, tokens, <c>PartyId</c> personal data, provider payloads, content, or internal
/// stream/projection identifiers (AD-14).
/// </summary>
/// <remarks>
/// Binding this query to the EventStore read path is deferred to the dedicated Agents read-model story (mirroring
/// <see cref="GetAgentInteractionStatusQuery"/> / Story 1.2). The stable query/view contracts land here.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id) to inspect.</param>
public record GetAgentInteractionGateEvidenceQuery(
    string AgentInteractionId)
{
    /// <summary>The EventStore query domain.</summary>
    public const string Domain = "agent-interaction";

    /// <summary>The query type discriminator.</summary>
    public const string QueryType = "get-agent-interaction-gate-evidence";
}
