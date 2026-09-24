namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Exact governed fields changed by a declared tightening.</summary>
public sealed record ProviderDataHandlingFieldDiff(
    int PreviousRetentionDays,
    int NewRetentionDays,
    bool PreviousAllowsTrainingUse,
    bool NewAllowsTrainingUse,
    IReadOnlyList<string> RemovedProcessingRegions,
    IReadOnlyList<string> AddedProcessingRegions,
    string PreviousTermsReferenceId,
    string NewTermsReferenceId);
