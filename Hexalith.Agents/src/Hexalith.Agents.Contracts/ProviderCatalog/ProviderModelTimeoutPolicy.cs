namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>
/// Safe timeout metadata for a provider/model entry (AD-10 capability floor). A bounded request duration plus
/// a bounded retry count. Carries no provider-SDK option types and no secret values; aggregate handlers
/// validate the bounds (see <c>ProviderCatalogAggregate</c>).
/// </summary>
/// <param name="RequestTimeoutMilliseconds">Bounded per-request timeout in milliseconds (must be positive).</param>
/// <param name="MaxRetries">Bounded retry count for a single invocation (must be zero or positive).</param>
public record ProviderModelTimeoutPolicy(int RequestTimeoutMilliseconds, int MaxRetries);
