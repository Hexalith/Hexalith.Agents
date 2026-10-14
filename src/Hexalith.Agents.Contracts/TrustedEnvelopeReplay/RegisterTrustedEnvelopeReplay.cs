namespace Hexalith.Agents.Contracts.TrustedEnvelopeReplay;

/// <summary>Private exact-target replay registration; target handlers and public callers cannot dispatch it.</summary>
/// <param name="Issuer">Authenticated issuer.</param><param name="DeliveryNonce">Original authenticated delivery nonce.</param>
/// <param name="TargetTenantId">Authenticated target tenant.</param><param name="LogicalCommandId">Logical AD-29 identity.</param>
/// <param name="SigningKeyVersion">Original signing version.</param><param name="CanonicalAndTagDigest">Authenticated delivery digest.</param>
/// <param name="FirstSeenAt">Original trusted first-seen instant.</param><param name="RetainUntil">Original exclusive retention boundary.</param>
public sealed record RegisterTrustedEnvelopeReplay(string Issuer, string DeliveryNonce, string TargetTenantId,
    string LogicalCommandId, string SigningKeyVersion, string CanonicalAndTagDigest, DateTimeOffset FirstSeenAt, DateTimeOffset RetainUntil);
