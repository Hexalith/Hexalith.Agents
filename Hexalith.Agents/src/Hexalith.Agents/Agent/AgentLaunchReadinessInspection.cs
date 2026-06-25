using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Agent;

/// <summary>
/// Pure, dependency-free read path over rehydrated <see cref="AgentState"/> for authorized inspection of an Agent's
/// launch readiness without exposing secrets (Story 4.4 AC4). Because it operates on a single Agent aggregate's state,
/// cross-tenant isolation is structural — it can never observe another tenant's Agent. Authorization is decided by the
/// caller (server/application) from trusted claims and passed in as <c>isAuthorized</c>; an unauthorized or
/// missing-state read returns a structured fail-closed result rather than throwing or leaking whether the Agent exists
/// (indistinguishable <c>NotAuthorized</c>/<c>NotFound</c>; AD-12).
/// </summary>
/// <remarks>
/// The launch-readiness <see cref="AgentLaunchReadinessView.Blockers"/> are computed by the same
/// <see cref="AgentLaunchReadinessPolicy.ComputeLaunchReadinessBlockers"/> the enablement gate uses, so the view's
/// blockers always match what an enablement attempt would reject. The only externally-resolved input —
/// <c>auditGovernanceResolved</c> — is resolved in the Server read/orchestration layer from the
/// <c>IAgentAuditGovernanceReadinessProvider</c> port and passed in here as a bool; this pure read never touches a port
/// (AD-3). Binding this logic to the EventStore read-model/projection read path is deferred (mirroring
/// <see cref="AgentInspection"/>); the logic is kept pure so it is fully unit-testable here.
/// </remarks>
public static class AgentLaunchReadinessInspection
{
    /// <summary>
    /// Returns the safe launch-readiness view of the Agent, or a structured fail-closed result (AC4).
    /// </summary>
    /// <param name="state">The rehydrated Agent state (null/never-created when no Agent exists).</param>
    /// <param name="isAuthorized">Whether the caller is an authorized Agents administrator for the tenant.</param>
    /// <param name="auditGovernanceResolved">Whether the Agents audit-evidence governance is resolved (the Story 4.2 port verdict, resolved in the Server layer).</param>
    /// <returns>A structured inspection result; the readiness view is present only on success.</returns>
    public static AgentLaunchReadinessInspectionResult GetLaunchReadiness(
        AgentState? state,
        bool isAuthorized,
        bool auditGovernanceResolved)
    {
        if (!isAuthorized)
        {
            return AgentLaunchReadinessInspectionResult.NotAuthorized();
        }

        if (state is null || !state.IsCreated)
        {
            return AgentLaunchReadinessInspectionResult.NotFound();
        }

        return AgentLaunchReadinessInspectionResult.Success(ToView(state, auditGovernanceResolved));
    }

    private static AgentLaunchReadinessView ToView(AgentState state, bool auditGovernanceResolved)
    {
        bool hasContentSafetyPolicy = state.ContentSafety is not null;
        bool hasContextPolicy = !string.IsNullOrWhiteSpace(state.LaunchReadiness?.ContextPolicyReference);
        return new AgentLaunchReadinessView(
            state.LaunchReadiness?.Metrics ?? [],
            state.LaunchReadiness?.LatencyTargets ?? [],
            state.LaunchReadiness?.CostPosture ?? CostControlPosture.Unknown,
            state.LaunchReadinessVersion,
            hasContentSafetyPolicy,
            hasContextPolicy,
            state.ProductionLikeGenerationEnabled,
            AgentLaunchReadinessPolicy.ComputeLaunchReadinessBlockers(
                hasContentSafetyPolicy,
                hasContextPolicy,
                state.LaunchReadiness,
                auditGovernanceResolved));
    }
}
