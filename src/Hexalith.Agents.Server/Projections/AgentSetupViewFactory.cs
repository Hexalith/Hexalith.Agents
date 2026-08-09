using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// Builds the authoritative <see cref="AgentSetupResult"/> from the persisted setup read model (Story 5.2 AC2).
/// Every setup read — the query handler, the public client, and FrontComposer — goes through this one factory, so
/// the API surface and the UI can never disagree about the truth stage they are showing.
/// </summary>
public static class AgentSetupViewFactory
{
    /// <summary>Creates the fail-closed setup read result.</summary>
    /// <param name="model">The persisted read model, or <see langword="null"/> when the Agent has never been projected.</param>
    /// <param name="tenantId">The tenant scope of the read.</param>
    /// <param name="agentId">The Agent aggregate id of the read.</param>
    /// <param name="expectedConfigurationVersion">
    /// The configuration version the caller is waiting to see, or <see langword="null"/> when the caller wants
    /// current projected truth.
    /// </param>
    /// <param name="isAgentsAdmin">Whether the caller is an authorized Agents administrator for the tenant.</param>
    /// <returns>The structured read result; the setup view is present only on success.</returns>
    public static AgentSetupResult Create(
        AgentSetupReadModel? model,
        string tenantId,
        string agentId,
        int? expectedConfigurationVersion,
        bool isAgentsAdmin)
    {
        if (!isAgentsAdmin)
        {
            return AgentSetupResult.NotAuthorized();
        }

        AgentInspectionResult inspection = AgentInspection.GetStatus(
            AgentSetupProjectionFold.ToState(model, tenantId, agentId),
            isAgentsAdmin: true);

        if (inspection.Agent is not { } agent || model is null)
        {
            return AgentSetupResult.NotFound();
        }

        // A caller that named the version it is waiting for gets an honest answer about whether the projection has
        // caught up. Without an expectation there is nothing to be behind, so the projected value is the truth.
        bool behind = expectedConfigurationVersion is { } expected && model.ConfigurationVersion < expected;

        return AgentSetupResult.Success(new AgentSetupView(
            agent,
            model.ConfigurationVersion,
            model.ProjectionVersion,
            model.ProjectedAt,
            behind ? AgentSetupFreshness.Stale : AgentSetupFreshness.Current,
            behind ? AgentSetupTruthState.AuthoritativePending : AgentSetupTruthState.ProjectionConfirmed));
    }
}
