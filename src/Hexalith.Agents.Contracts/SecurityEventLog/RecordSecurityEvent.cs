namespace Hexalith.Agents.Contracts.SecurityEventLog;

/// <summary>Private spool-drain intent containing no claimed target identity, Party PII or secrets.</summary>
/// <param name="SecurityObservationId">Original immutable spool identity.</param><param name="RoutingTenantId">Authenticated actor route or system.</param>
/// <param name="UtcDay">Original spool-assigned day.</param><param name="ObservedAt">Original spool first-seen instant.</param>
/// <param name="ReasonCode">Closed denial class.</param><param name="UntrustedFieldsHmac">Reserved-system keyed digest only.</param>
/// <param name="DigestKeyVersion">Original retained digest version.</param>
public sealed record RecordSecurityEvent(string SecurityObservationId, string RoutingTenantId, string UtcDay,
    DateTimeOffset ObservedAt, string ReasonCode, string UntrustedFieldsHmac, string DigestKeyVersion);
