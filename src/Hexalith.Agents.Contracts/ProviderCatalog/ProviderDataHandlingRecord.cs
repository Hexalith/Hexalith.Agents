namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Governed, display-safe provider data handling terms.</summary>
/// <param name="RetentionDays">Maximum number of days provider data is retained.</param>
/// <param name="AllowsTrainingUse">Whether provider data may be used for training.</param>
/// <param name="ProcessingRegions">The allowed processing regions.</param>
/// <param name="TermsReferenceId">The safe reference to the terms presented to the tenant.</param>
/// <param name="DataHandlingVersion">The non-reusable version of these terms.</param>
public sealed record ProviderDataHandlingRecord(
    int RetentionDays,
    bool AllowsTrainingUse,
    IReadOnlyList<string> ProcessingRegions,
    string TermsReferenceId,
    int DataHandlingVersion,
    DateTimeOffset? EffectiveAt = null,
    ProviderDataHandlingTighteningDeclaration? TighteningDeclaration = null);
