namespace Hexalith.Agents.Contracts.ProviderCatalog.Commands;

/// <summary>A tenant administrator's decision on the exact current provider terms.</summary>
public sealed record DecideProviderDataHandling(
    string ProviderId,
    string ModelId,
    int DataHandlingVersion,
    bool Accepted,
    string Justification,
    int ExpectedRevision,
    ProviderDataHandlingRecord ConfirmedTerms,
    DateTimeOffset DecidedAt);
