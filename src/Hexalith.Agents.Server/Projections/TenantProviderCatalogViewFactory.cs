using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.TenantProviderEnablement;

namespace Hexalith.Agents.Server.Projections;

/// <summary>Creates the authorized and configuration-free tenant catalog join.</summary>
public static class TenantProviderCatalogViewFactory
{
    /// <summary>Lists only entries explicitly enabled for the tenant.</summary>
    public static TenantProviderCatalogInspectionResult CreateList(
        ProviderCatalogReadModel? platform,
        TenantProviderEnablementReadModel? tenant,
        bool authorized,
        DateTimeOffset evaluatedAt,
        bool includeDisabled = false)
    {
        if (!authorized)
        {
            return new(ProviderCatalogInspectionStatus.NotAuthorized, []);
        }

        if (platform is null || tenant is null)
        {
            return new(ProviderCatalogInspectionStatus.Success, [],
                platform?.ProjectionVersion, tenant?.ProjectionVersion,
                Freshness: AgentSetupFreshness.Stale,
                TruthState: AgentSetupTruthState.AuthoritativePending);
        }

        TenantProviderCatalogEntryView[] entries = [.. platform.Entries
            .Where(item => tenant.State.Entries.TryGetValue(ProviderCatalogState.EntryKey(item.ProviderId, item.ModelId), out TenantProviderEntryState? enabled)
                && enabled.Enabled && (includeDisabled || item.Status == ProviderModelStatus.Enabled))
            .OrderBy(item => item.ProviderId, StringComparer.Ordinal)
            .ThenBy(item => item.ModelId, StringComparer.Ordinal)
            .Select(item => ToTenantView(item,
                tenant.State.Entries[ProviderCatalogState.EntryKey(item.ProviderId, item.ModelId)],
                tenant.State.Revision,
                evaluatedAt))];

        return new(ProviderCatalogInspectionStatus.Success, entries,
            platform.ProjectionVersion, tenant.ProjectionVersion,
            ProjectedAt: platform.ProjectedAt is { } p && tenant.ProjectedAt is { } t
                ? p <= t ? p : t : null,
            Freshness: AgentSetupFreshness.Current,
            TruthState: AgentSetupTruthState.ProjectionConfirmed);
    }

    /// <summary>Gets an enabled entry; absent and nonenabled identities share the same result.</summary>
    public static TenantProviderCatalogInspectionResult CreateEntry(
        ProviderCatalogReadModel? platform,
        TenantProviderEnablementReadModel? tenant,
        bool authorized,
        string providerId,
        string modelId,
        DateTimeOffset evaluatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        TenantProviderCatalogInspectionResult listed = CreateList(platform, tenant, authorized, evaluatedAt, includeDisabled: true);
        if (listed.Status != ProviderCatalogInspectionStatus.Success)
        {
            return listed;
        }

        TenantProviderCatalogEntryView? found = listed.Entries.FirstOrDefault(item =>
            string.Equals(item.ProviderId, providerId, StringComparison.Ordinal)
            && string.Equals(item.ModelId, modelId, StringComparison.Ordinal));
        return found is null
            ? listed with { Status = ProviderCatalogInspectionStatus.EntryNotFound, Entries = [] }
            : listed with { Entries = [found] };
    }

    private static TenantProviderCatalogEntryView ToTenantView(
        ProviderCatalogEntryView platform,
        TenantProviderEntryState tenant,
        int revision,
        DateTimeOffset evaluatedAt)
    {
        TenantProviderEligibilityResult eligibility = TenantProviderEligibility.Evaluate(
            tenant, platform.DataHandlingHistory, evaluatedAt);
        bool platformReady = platform.IsSelectableForNewActiveUse;
        return new(
            platform.ProviderId,
            platform.ModelId,
            platform.DisplayLabel,
            platform.Status == ProviderModelStatus.Enabled,
            tenant.Enabled,
            platform.SupportsTextGeneration,
            platform.ContextWindowTokenLimit,
            platform.MaxOutputTokenLimit,
            platform.TimeoutPolicy,
            platform.SafeCapabilityFlags,
            platform.CapabilityVersion,
            platform.Pricing,
            platform.DataHandling,
            platformReady && (eligibility.Status is "Current" or "Grace"),
            platformReady ? eligibility.Status : "PlatformNotReady",
            eligibility.GraceExpiresAt,
            tenant.AcceptedTerms?.DataHandlingVersion,
            revision,
            tenant.LastDecisionMessageId);
    }
}
