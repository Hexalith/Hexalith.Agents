
namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>Exact effective Product authority phase-pinned before Provider transport; no local status mapping.</summary>
/// <param name="DecisionRevision">The DecisionRevision binding.</param>
/// <param name="ContractDigest">The ContractDigest binding.</param>
/// <param name="FailureStatus">The FailureStatus binding.</param>
/// <param name="ReasonId">The ReasonId binding.</param>
/// <param name="MetricId">The MetricId binding.</param>
/// <param name="OpenLeaseDispositionId">The OpenLeaseDispositionId binding.</param>
/// <param name="CatalogActivationRevision">The CatalogActivationRevision binding.</param>
/// <param name="ObservationId">The ObservationId binding.</param>
public sealed record ProviderOutputAuthority(long DecisionRevision, string ContractDigest, AgentInteractionStatus FailureStatus, string ReasonId, string MetricId, string OpenLeaseDispositionId, long CatalogActivationRevision, string ObservationId)
{
    /// <summary>Checks the exact effective authority shape without inventing a decision or status mapping.</summary>
    public bool IsEffective() => DecisionRevision > 0 && CatalogActivationRevision > 0 && IsDigest(ContractDigest) && IsDigest(ObservationId)
        && FailureStatus != AgentInteractionStatus.Unknown && Enum.IsDefined(FailureStatus)
        && !string.IsNullOrWhiteSpace(ReasonId) && !string.IsNullOrWhiteSpace(MetricId) && !string.IsNullOrWhiteSpace(OpenLeaseDispositionId);
    private static bool IsDigest(string value) => value is { Length: 64 } && value.All(char.IsAsciiHexDigit);
}
