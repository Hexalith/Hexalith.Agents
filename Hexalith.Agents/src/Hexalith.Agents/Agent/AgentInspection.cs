using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Agent;

/// <summary>
/// Pure, dependency-free read path over rehydrated <see cref="AgentState"/> for authorized inspection of the
/// governed Agent's lifecycle and configuration without exposing secrets (AC1, AC3, AC4). Because it operates on a
/// single Agent aggregate's state, cross-tenant isolation is structural — it can never observe another tenant's
/// Agent. Authorization is decided by the caller (server/application) from trusted claims and passed in as
/// <c>isAgentsAdmin</c>; an unauthorized or missing-state read returns a structured fail-closed result rather than
/// throwing or leaking whether the Agent exists (AC4).
/// </summary>
/// <remarks>
/// The status view never exposes the raw Agent Instructions text (AD-14) — only instruction presence, validity,
/// and version. The <see cref="AgentLifecycleStatus.Disabled"/> state is visible through this public path (AC3),
/// and the activation blockers it reports always match what an activation attempt would reject (they share
/// <see cref="AgentConfigurationPolicy"/>). Binding this logic to the EventStore SDK
/// <c>IDomainQueryHandler</c>/<c>IReadModelStore</c> DAPR read path is deferred to the dedicated Agents
/// read-model story (mirroring Story 1.2); the logic is kept pure so it is fully unit-testable here.
/// </remarks>
public static class AgentInspection
{
    /// <summary>
    /// Returns the safe public status view of the Agent, or a structured fail-closed result (AC3, AC4).
    /// </summary>
    /// <param name="state">The rehydrated Agent state (null/never-created when no Agent exists).</param>
    /// <param name="isAgentsAdmin">Whether the caller is an authorized Agents administrator for the tenant.</param>
    /// <returns>A structured inspection result; the status view is present only on success.</returns>
    public static AgentInspectionResult GetStatus(AgentState? state, bool isAgentsAdmin)
    {
        if (!isAgentsAdmin)
        {
            return AgentInspectionResult.NotAuthorized();
        }

        if (state is null || !state.IsCreated)
        {
            return AgentInspectionResult.NotFound();
        }

        return AgentInspectionResult.Success(ToView(state));
    }

    private static AgentStatusView ToView(AgentState state)
        => new(
            state.AgentId,
            state.TenantId,
            state.DisplayName,
            state.Description,
            state.Lifecycle,
            state.ConfigurationVersion,
            AgentConfigurationPolicy.HasInstructions(state.Instructions),
            AgentConfigurationPolicy.AreInstructionsValid(state.Instructions),
            state.InstructionsVersion,
            state.PartyId is not null,
            state.ProviderId is not null,
            state.ProviderId,
            state.ModelId,
            // AgentInspection is a *pure* read over Agent state only — it cannot freshly re-read the catalog, so it
            // trusts the last-validated recorded selection (selectedProviderReady: true) and surfaces only the static
            // MissingProviderSelection gate. Live ProviderUnavailable surfacing for the readiness badge is supplied by
            // the activation path (this story, via the trusted verdict) and by the 1.8 status/overview orchestration
            // that reads the catalog. Making this read the catalog would break purity and AD-3 — do not.
            AgentConfigurationPolicy.ComputeActivationBlockers(
                state.DisplayName,
                state.Instructions,
                state.PartyId is not null,
                hasProviderSelection: state.ProviderId is not null,
                selectedProviderReady: true));
}
