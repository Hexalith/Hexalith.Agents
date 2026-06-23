namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>
/// Outcome of an authorized provider-catalog inspection request (AC3). Inspection returns a structured status
/// rather than throwing, and never reveals unrelated tenant records or secret internals.
/// </summary>
public enum ProviderCatalogInspectionStatus
{
    /// <summary>The inspection succeeded; <c>Entries</c> carries the safe views.</summary>
    Success = 0,

    /// <summary>The caller is not authorized for provider administration; no entry data is returned.</summary>
    NotAuthorized,

    /// <summary>The requested single entry does not exist in the catalog.</summary>
    EntryNotFound,
}
