namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Requests the safe edit audit-evidence for the Proposed Agent Reply on the Agent Call (<c>AgentInteraction</c>)
/// identified by <see cref="AgentInteractionId"/> within the request's tenant scope (AC4; FR-14, FR-15). Returns an
/// <see cref="AgentProposalEditEvidenceResult"/> carrying the coarse status and the safe edit-evidence view — the edited
/// version id, source version id, editor, policy basis, and the EventStore-metadata timestamp — never the edited content,
/// raw provider/Conversations payloads, <c>PartyId</c> personal data, or internal stream/projection identifiers (AD-14).
/// </summary>
/// <remarks>
/// Binding this query to the EventStore read path (the proposal edit-evidence projection + query handler) is deferred to
/// the dedicated Agents read-model story (Epic 4, mirroring <see cref="GetAgentInteractionContextEvidenceQuery"/> and the
/// Story 3.2 <see cref="ListPendingProposalsQuery"/>). The stable query/view/result contracts land here.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id) whose proposal edit evidence is inspected.</param>
public record GetAgentProposalEditEvidenceQuery(
    string AgentInteractionId);
