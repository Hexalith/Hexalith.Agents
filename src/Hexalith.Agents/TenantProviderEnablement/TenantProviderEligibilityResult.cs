namespace Hexalith.Agents.TenantProviderEnablement;

/// <summary>Safe tenant terms eligibility and an exclusive deadline when grace applies.</summary>
public sealed record TenantProviderEligibilityResult(string Status, DateTimeOffset? GraceExpiresAt);
