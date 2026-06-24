namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Requests the safe regeneration audit-evidence for the Proposed Agent Reply on the Agent Call (<c>AgentInteraction</c>)
/// identified by <see cref="AgentInteractionId"/> within the request's tenant scope (AC3, AC4; FR-14, FR-16). Returns an
/// <see cref="AgentProposalRegenerationEvidenceResult"/> carrying the coarse status and the safe regeneration-evidence view —
/// the regenerated version id, attempt id, requester, source Conversation, provider/model/policy versions, policy basis, the
/// safe failure class, and the EventStore-metadata timestamp — never the regenerated content, prompts, raw provider/
/// Conversations payloads, <c>PartyId</c> personal data, or internal stream/projection identifiers (AD-14).
/// </summary>
/// <remarks>
/// Binding this query to the EventStore read path (the proposal regeneration-evidence projection + query handler) is deferred
/// to the dedicated Agents read-model story (Epic 4, mirroring <see cref="GetAgentProposalEditEvidenceQuery"/> and the
/// Story 3.2 <see cref="ListPendingProposalsQuery"/>). The stable query/view/result contracts land here.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id) whose proposal regeneration evidence is inspected.</param>
public record GetAgentProposalRegenerationEvidenceQuery(
    string AgentInteractionId);
