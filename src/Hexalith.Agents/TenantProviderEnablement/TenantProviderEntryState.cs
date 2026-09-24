using Hexalith.Agents.Contracts.ProviderCatalog;

namespace Hexalith.Agents.TenantProviderEnablement;

/// <summary>Replay state for one tenant's provider/model visibility and decision.</summary>
public sealed class TenantProviderEntryState
{
    /// <summary>Gets or sets whether the platform enabled this entry for the tenant.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the last accepted terms.</summary>
    public ProviderDataHandlingRecord? AcceptedTerms { get; set; }

    /// <summary>Gets or sets the most recent declined version, if any.</summary>
    public int? DeclinedVersion { get; set; }

    /// <summary>Gets or sets the last durable decision.</summary>
    public Contracts.ProviderCatalog.Events.ProviderDataHandlingDecided? LastDecision { get; set; }

    /// <summary>Gets or sets the migration provenance.</summary>
    public string? MigratedFrom { get; set; }

    /// <summary>Gets or sets the command identity of the last projected enablement event.</summary>
    public string? LastEnablementMessageId { get; set; }

    /// <summary>Gets or sets the command identity of the last projected terms decision.</summary>
    public string? LastDecisionMessageId { get; set; }
}
