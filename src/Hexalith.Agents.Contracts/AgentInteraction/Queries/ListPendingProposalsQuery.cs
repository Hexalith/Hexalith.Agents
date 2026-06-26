namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Requests the authorized pending-proposal queue — the in-product discovery surface for Proposed Agent Replies
/// (AC1, AC2, AC3, AC4; FR-13, FR-14). Returns a <see cref="PendingProposalsResult"/> of safe
/// <see cref="PendingProposalView"/> rows — proposal/interaction/version ids, Source-Conversation/caller references,
/// Agent id, policy-version numbers, and the coarse state/freshness flags — never the generated content, Conversation
/// payloads, or internal stream/projection identifiers (AD-14). The query record carries <b>no tenantId</b>; tenant
/// scope is supplied by the request/envelope at dispatch (mirroring <see cref="ProviderCatalog.Queries.ListProviderCatalogEntriesQuery"/>).
/// </summary>
/// <remarks>
/// Binding this query to the EventStore SDK <c>IDomainQueryHandler</c>/<c>IReadModelStore</c> DAPR read path — and the
/// proposal read-model <em>projection</em> that computes <c>NeedsCurrentUserAction</c>/age/freshness — is deferred to the
/// dedicated Agents read-model story (Epic 4, mirroring Story 1.2 / <c>GetAgentInteractionStatusQuery</c>). The stable
/// query/view/result contracts land here.
/// </remarks>
/// <param name="IncludeHistorical">
/// When <see langword="false"/>, only proposals awaiting action are returned; when <see langword="true"/>, also the
/// authorized historical/terminal proposals once Stories 3.5/3.6 add them.
/// </param>
public record ListPendingProposalsQuery(bool IncludeHistorical);
