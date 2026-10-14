namespace Hexalith.Agents.Server.Ports;

/// <summary>Host-verified current authenticated subject, actor authority and active human Party binding.</summary>
/// <param name="TenantId">Exact actor tenant.</param><param name="Subject">Authenticated subject correspondence.</param>
/// <param name="ActorId">Stable non-PII human actor.</param><param name="PartyId">Exact active human Party.</param>
/// <param name="BindingVersion">Current immutable binding.</param><param name="SourcePosition">Complete authoritative source head.</param>
/// <param name="ObservedAt">Verified observation.</param><param name="ValidUntil">Exclusive current authority expiry.</param>
public sealed record CurrentHumanActorEvidence(string TenantId, string Subject, string ActorId, string PartyId,
    long BindingVersion, long SourcePosition, DateTimeOffset ObservedAt, DateTimeOffset ValidUntil);
