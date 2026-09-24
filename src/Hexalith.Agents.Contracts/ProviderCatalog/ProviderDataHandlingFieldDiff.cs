namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Exact governed fields changed by a declared tightening.</summary>
/// <param name="PreviousRetentionDays">The previous retention period.</param>
/// <param name="NewRetentionDays">The new retention period.</param>
/// <param name="PreviousAllowsTrainingUse">The previous training-use decision.</param>
/// <param name="NewAllowsTrainingUse">The new training-use decision.</param>
/// <param name="RemovedProcessingRegions">Regions removed from the allowed set.</param>
/// <param name="AddedProcessingRegions">Regions added to the allowed set.</param>
/// <param name="PreviousTermsReferenceId">The previous terms reference.</param>
/// <param name="NewTermsReferenceId">The new terms reference.</param>
public sealed record ProviderDataHandlingFieldDiff(
    int PreviousRetentionDays,
    int NewRetentionDays,
    bool PreviousAllowsTrainingUse,
    bool NewAllowsTrainingUse,
    IReadOnlyList<string> RemovedProcessingRegions,
    IReadOnlyList<string> AddedProcessingRegions,
    string PreviousTermsReferenceId,
    string NewTermsReferenceId);
