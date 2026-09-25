namespace Hexalith.Agents.TenantProviderEnablement;

/// <summary>Safe tenant terms eligibility and an exclusive deadline when grace applies.</summary>
/// <param name="Status">The safe eligibility status.</param>
/// <param name="GraceExpiresAt">The exclusive grace deadline, when applicable.</param>
public sealed record TenantProviderEligibilityResult(string Status, DateTimeOffset? GraceExpiresAt);
