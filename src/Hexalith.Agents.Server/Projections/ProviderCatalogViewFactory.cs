using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.ProviderCatalog;

namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// Builds the authoritative <see cref="ProviderCatalogInspectionResult"/> from the persisted catalog read model
/// (Story 5.3 AC2). Every catalog read — the query handler, the public client, and FrontComposer — goes through
/// this one factory.
/// </summary>
public static class ProviderCatalogViewFactory
{
    /// <summary>Creates the fail-closed catalog list result.</summary>
    /// <param name="model">The persisted read model, or <see langword="null"/>.</param>
    /// <param name="tenantId">The tenant scope of the read.</param>
    /// <param name="includeDisabled">Whether disabled entries are included. Enabled but ineligible entries remain visible and are flagged not selectable.</param>
    /// <param name="expectedProjectionVersion">The projection version the caller is waiting for, if any.</param>
    /// <param name="isProviderAdmin">Whether the caller is an authorized catalog administrator.</param>
    /// <returns>The structured inspection result.</returns>
    public static ProviderCatalogInspectionResult CreateList(
        ProviderCatalogReadModel? model,
        string tenantId,
        bool includeDisabled,
        string? expectedProjectionVersion,
        bool isProviderAdmin)
    {
        if (!isProviderAdmin)
        {
            return ProviderCatalogInspectionResult.NotAuthorized();
        }

        ProviderCatalogInspectionResult inspection = ProviderCatalogInspection.ListEntries(
            ProviderCatalogProjectionFold.ToState(model, tenantId),
            isProviderAdmin: true,
            includeDisabled);

        return WithFreshness(inspection, model, expectedProjectionVersion, expectedCapabilityVersion: null);
    }

    /// <summary>Creates the fail-closed single-entry result.</summary>
    /// <param name="model">The persisted read model, or <see langword="null"/>.</param>
    /// <param name="tenantId">The tenant scope of the read.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="expectedCapabilityVersion">The capability version the caller is waiting for, if any.</param>
    /// <param name="isProviderAdmin">Whether the caller is an authorized catalog administrator.</param>
    /// <returns>The structured inspection result.</returns>
    public static ProviderCatalogInspectionResult CreateEntry(
        ProviderCatalogReadModel? model,
        string tenantId,
        string providerId,
        string modelId,
        int? expectedCapabilityVersion,
        bool isProviderAdmin)
    {
        if (!isProviderAdmin)
        {
            return ProviderCatalogInspectionResult.NotAuthorized();
        }

        ProviderCatalogInspectionResult inspection = ProviderCatalogInspection.GetEntry(
            ProviderCatalogProjectionFold.ToState(model, tenantId),
            isProviderAdmin: true,
            providerId,
            modelId);

        return WithFreshness(inspection, model, expectedProjectionVersion: null, expectedCapabilityVersion);
    }

    private static ProviderCatalogInspectionResult WithFreshness(
        ProviderCatalogInspectionResult inspection,
        ProviderCatalogReadModel? model,
        string? expectedProjectionVersion,
        int? expectedCapabilityVersion)
    {
        if (inspection.Status is ProviderCatalogInspectionStatus.NotAuthorized
            or ProviderCatalogInspectionStatus.Unavailable)
        {
            return inspection;
        }

        bool behindProjection = IsBehindProjection(model, expectedProjectionVersion);
        bool waitingForCapability = expectedCapabilityVersion is not null;
        bool missingOrBehindCapability = waitingForCapability
            && (inspection.Status is ProviderCatalogInspectionStatus.EntryNotFound
                || inspection.Entries.Count == 0
                || inspection.Entries.All(entry => entry.CapabilityVersion < expectedCapabilityVersion));

        bool behind = behindProjection || missingOrBehindCapability;

        if (inspection.Status is ProviderCatalogInspectionStatus.EntryNotFound)
        {
            return waitingForCapability || behindProjection
                ? inspection with
                {
                    ProjectionVersion = model?.ProjectionVersion,
                    ProjectedAt = model?.ProjectedAt,
                    Freshness = AgentSetupFreshness.Stale,
                    TruthState = AgentSetupTruthState.AuthoritativePending,
                }
                : inspection;
        }

        return inspection with
        {
            ProjectionVersion = model?.ProjectionVersion,
            ProjectedAt = model?.ProjectedAt,
            Freshness = behind ? AgentSetupFreshness.Stale : AgentSetupFreshness.Current,
            TruthState = behind ? AgentSetupTruthState.AuthoritativePending : AgentSetupTruthState.ProjectionConfirmed,
        };
    }

    private static bool IsBehindProjection(ProviderCatalogReadModel? model, string? expectedProjectionVersion)
    {
        if (string.IsNullOrWhiteSpace(expectedProjectionVersion))
        {
            return false;
        }

        if (model is null)
        {
            return true;
        }

        return !long.TryParse(expectedProjectionVersion, NumberStyles.Integer, CultureInfo.InvariantCulture, out long expected)
            || model.LastSequenceNumber < expected;
    }
}
