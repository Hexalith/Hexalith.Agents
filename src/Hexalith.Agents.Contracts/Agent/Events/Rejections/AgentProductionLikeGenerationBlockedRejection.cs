using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// Production-like generation enablement was rejected because one or more launch-readiness gates fail (Story 4.4 AC4;
/// FR-28). Carries the specific <see cref="Blockers"/> so a Release Operator knows exactly what to fix. The rejected
/// enablement does not change the Agent's state — production-like generation stays disabled (fail-closed; AD-12).
/// </summary>
/// <remarks>
/// The blockers classify <em>which</em> readiness decision is missing/invalid; this rejection never carries secrets,
/// raw payloads, or content (AD-14), only the safe blocker classification.
/// </remarks>
/// <param name="AgentId">The Agent aggregate identifier the enablement targeted.</param>
/// <param name="Blockers">The specific launch-readiness blockers (non-empty).</param>
public record AgentProductionLikeGenerationBlockedRejection(
    string AgentId,
    IReadOnlyList<AgentLaunchReadinessBlocker> Blockers) : IRejectionEvent;
