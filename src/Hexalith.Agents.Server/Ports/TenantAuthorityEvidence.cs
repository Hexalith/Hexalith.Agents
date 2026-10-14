namespace Hexalith.Agents.Server.Ports;

/// <summary>One coherent complete stream fold and current independent source-head proof.</summary>
/// <param name="TenantId">Exact authority tenant.</param>
/// <param name="Domain">tenants or global-administrators.</param>
/// <param name="SourceHead">Independent authoritative stream head.</param>
/// <param name="ObservedAt">Source observation instant.</param>
/// <param name="ValidUntil">Exclusive source-authority expiry.</param>
/// <param name="Events">All events from position one through SourceHead.</param>
/// <param name="SourceTenantId">Reserved system namespace owned by Tenants.</param>
/// <param name="SourceAggregateId">Managed tenant or exact global-administrators aggregate.</param>
public sealed record TenantAuthorityEvidence(string TenantId, string Domain, long SourceHead,
    DateTimeOffset ObservedAt, DateTimeOffset ValidUntil, IReadOnlyList<TenantAuthorityEvent> Events,
    string SourceTenantId = "system", string? SourceAggregateId = null);
