namespace Hexalith.Agents.Contracts.ProviderCatalog.Commands;

/// <summary>A tenant administrator's decision on the exact current provider terms.</summary>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="DataHandlingVersion">The version being decided.</param>
/// <param name="Accepted">Whether the terms were accepted.</param>
/// <param name="Justification">The administrator's explanation.</param>
/// <param name="ExpectedRevision">The current tenant stream revision.</param>
/// <param name="ConfirmedTerms">The exact terms shown to the administrator.</param>
/// <param name="DecidedAt">The trusted server decision time.</param>
public sealed record DecideProviderDataHandling(
    string ProviderId,
    string ModelId,
    int DataHandlingVersion,
    bool Accepted,
    string Justification,
    int ExpectedRevision,
    ProviderDataHandlingRecord ConfirmedTerms,
    DateTimeOffset DecidedAt);
