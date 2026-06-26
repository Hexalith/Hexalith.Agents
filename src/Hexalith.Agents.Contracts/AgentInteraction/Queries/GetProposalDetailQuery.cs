namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Requests the authorized single-proposal detail backing the Story 3.7 approval workspace — the bounded detail surface
/// that hosts the version history and the act-on controls for ONE Proposed Agent Reply (AC1, AC2; FR-13, FR-15, FR-16,
/// FR-17). Returns a <see cref="ProposalDetailResult"/> carrying the coarse status and the safe
/// <see cref="ProposalDetailView"/> — ids, the Source-Conversation/caller references, the snapshot response-mode and
/// provider/model, policy versions, the posting outcome, and the append-only content-free version history — never the
/// generated/edited content, Conversation payloads, or internal stream/projection identifiers (AD-14). The query record
/// carries <b>no tenantId</b>; tenant scope is supplied by the request/envelope at dispatch (mirroring
/// <see cref="ListPendingProposalsQuery"/>).
/// </summary>
/// <remarks>
/// Binding this query to the EventStore SDK <c>IDomainQueryHandler</c>/<c>IReadModelStore</c> DAPR read path — and the
/// proposal-detail read-model projection — is deferred to the dedicated Agents read-model story (Epic 4, mirroring
/// <see cref="ListPendingProposalsQuery"/> and <see cref="GetAgentInteractionStatusQuery"/>). The stable
/// query/view/result contracts land here; against the deferred gateway the detail surface renders the fail-closed
/// permission-denied/unavailable state (AD-12).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id) whose proposal detail is requested.</param>
public record GetProposalDetailQuery(
    string AgentInteractionId);
