namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Requests the authorized, safe operational-status summary for the operator surface (Story 4.3 AC1, AC4; FR22, FR23,
/// FR25). Returns an <see cref="Operations.AgentOperationalStatusSummaryResult"/> of safe enums/ids/ints/ISO-8601 strings
/// — the Agent readiness state, readiness blockers, audit-governance blockers, audit availability, recent Agent Call
/// outcome counts, proposal terminal-state/posting outcome counts, and the pending-proposal count — never any prompt,
/// generated content, Conversation payload, or per-record summary text (AD-14). The query record carries <b>no
/// tenantId</b>; tenant scope is supplied by the request/envelope at dispatch (mirroring
/// <see cref="ListPendingProposalsQuery"/> / <see cref="ProviderCatalog.Queries.ListProviderCatalogEntriesQuery"/>).
/// </summary>
/// <remarks>
/// Binding this query to the EventStore SDK <c>IDomainQueryHandler</c>/<c>IReadModelStore</c> read path — and the
/// operational read-model <em>projection</em> that computes the recent-outcome / terminal-state-rate / posting-outcome
/// counts — is deferred to the operational read-model/topology work (AD-16; the Server <c>Projections/</c> folder stays
/// <c>.gitkeep</c>-only), mirroring Story 3.2 / <c>ListPendingProposalsQuery</c> and Story 1.2 /
/// <c>GetAgentInteractionStatusQuery</c>. The stable query/view/result contracts land here; the UI gateway seam fails
/// closed against the default DI graph until the live binding lands behind the same interface.
/// </remarks>
public record GetAgentOperationalStatusSummaryQuery()
{
    /// <summary>The EventStore query domain.</summary>
    public const string Domain = "agent-operational-status";

    /// <summary>The query type discriminator.</summary>
    public const string QueryType = "get-agent-operational-status-summary";
}
