using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// Activation was rejected because one or more required fields are missing or invalid (AC2; FR-3). Carries the
/// specific <see cref="Blockers"/> so an administrator knows exactly what to fix. The rejected activation does
/// not change the Agent's lifecycle — <c>hexa</c> does not become callable (AC2).
/// </summary>
/// <remarks>
/// The blockers classify <em>which</em> field is missing/invalid; this rejection never carries the raw Agent
/// Instructions text (AD-14), only the safe blocker classification.
/// </remarks>
/// <param name="AgentId">The Agent aggregate identifier the activation targeted.</param>
/// <param name="Blockers">The specific activation blockers (non-empty).</param>
public record AgentActivationBlockedRejection(
    string AgentId,
    IReadOnlyList<AgentActivationBlocker> Blockers) : IRejectionEvent;
