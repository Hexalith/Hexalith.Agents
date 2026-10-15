namespace Hexalith.Agents.Server.Ports;

/// <summary>Host-verified current authenticated subject, actor authority and active human Party binding.</summary>
/// <param name="TenantId">Exact actor tenant.</param><param name="Subject">Authenticated subject correspondence.</param>
/// <param name="ActorId">Stable non-PII human actor.</param><param name="PartyId">Exact active human Party.</param>
/// <param name="BindingVersion">Current immutable binding.</param><param name="SourcePosition">Complete authoritative source head.</param>
/// <param name="ObservedAt">Verified observation.</param><param name="ValidUntil">Exclusive current authority expiry.</param>
public sealed record CurrentHumanActorEvidence(string TenantId, string Subject, string ActorId, string PartyId,
    long BindingVersion, long SourcePosition, DateTimeOffset ObservedAt, DateTimeOffset ValidUntil)
{
    /// <summary>Checks exact subject/scope/binding correspondence and fresh UTC evidence at the completion instant.</summary>
    internal bool IsCurrentFor(string tenantId, string subject, string? partyId, DateTimeOffset started, DateTimeOffset now)
        => TenantId == tenantId && Subject == subject
            && !string.IsNullOrWhiteSpace(ActorId) && !string.IsNullOrWhiteSpace(PartyId)
            && BindingVersion > 0 && SourcePosition > 0 && ObservedAt != default
            && now >= started && ObservedAt >= started && ObservedAt <= now && ValidUntil > now
            && ObservedAt.Offset == TimeSpan.Zero && ValidUntil.Offset == TimeSpan.Zero
            && (partyId is null || PartyId == partyId);
}
