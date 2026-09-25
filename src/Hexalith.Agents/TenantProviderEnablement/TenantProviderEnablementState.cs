using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.ProviderCatalog;

namespace Hexalith.Agents.TenantProviderEnablement;

/// <summary>Event-sourced tenant visibility and terms decisions.</summary>
public sealed class TenantProviderEnablementState
{
    /// <summary>Gets or sets the tenant identity.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the successful mutation revision.</summary>
    public int Revision { get; set; }

    /// <summary>Gets or sets provider/model entries.</summary>
    public Dictionary<string, TenantProviderEntryState> Entries { get; set; } = [];

    /// <summary>Applies a platform enablement decision.</summary>
    /// <param name="e">The enablement event.</param>
    public void Apply(TenantProviderModelEnablementSet e)
    {
        ArgumentNullException.ThrowIfNull(e);
        TenantId = e.TenantId;
        string key = ProviderCatalogState.EntryKey(e.ProviderId, e.ModelId);
        if (!Entries.TryGetValue(key, out TenantProviderEntryState? entry))
        {
            entry = new TenantProviderEntryState();
            Entries[key] = entry;
        }

        entry.Enabled = e.Enabled;
        entry.MigratedFrom ??= e.MigratedFrom;
        Revision = e.Revision;
    }

    /// <summary>Applies a tenant's accepted or declined terms.</summary>
    /// <param name="e">The decision event.</param>
    public void Apply(ProviderDataHandlingDecided e)
    {
        ArgumentNullException.ThrowIfNull(e);
        TenantId = e.TenantId;
        string key = ProviderCatalogState.EntryKey(e.ProviderId, e.ModelId);
        if (!Entries.TryGetValue(key, out TenantProviderEntryState? entry))
        {
            return;
        }

        entry.LastDecision = e;
        if (e.Accepted)
        {
            entry.AcceptedTerms = e.ConfirmedTerms;
            entry.DeclinedVersion = null;
        }
        else
        {
            entry.DeclinedVersion = e.ConfirmedTerms.DataHandlingVersion;
        }

        Revision = e.Revision;
    }

    /// <summary>No-op replay handler for a rejected command.</summary>
    /// <param name="e">The rejected event.</param>
    public void Apply(TenantProviderGovernanceRejected e)
    {
        ArgumentNullException.ThrowIfNull(e);
        _ = Revision;
    }
}
