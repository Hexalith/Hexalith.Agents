using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an Agent Call (<c>AgentInteraction</c>) failed the invocation gate, durably transitioning it to a
/// denied or blocked decision with safe blocker evidence (AC1, AC2, AC4; FR-20, FR-21, FR-24). This is a durable
/// success event (NOT an <c>IRejectionEvent</c>): a denied/blocked call is a <em>successfully-recorded negative
/// decision</em> and this record IS the Audit Evidence — distinct from
/// <see cref="Rejections.AgentInteractionGateNotEvaluableRejection"/>, which is used only when the gate cannot be
/// evaluated at all. No provider invocation, Proposed Agent Reply, or Conversation Message is created (AC2).
/// </summary>
/// <remarks>
/// <see cref="Decision"/> is <see cref="AgentInteractionStatus.Denied"/> (an authorization-class check failed) or
/// <see cref="AgentInteractionStatus.Blocked"/> (only readiness-class checks failed). <see cref="Blockers"/> are the
/// non-<see cref="AgentInteractionGateOutcome.Satisfied"/> verdicts — safe coarse classifications only, never raw
/// claims, tokens, <c>PartyId</c> personal data, provider payloads, or content (AD-14, AC4). Authorized administrators
/// inspect these to distinguish authorization vs dependency vs Agent-readiness vs Provider-readiness vs policy
/// failures (AC4). There is no wall-clock field — gate time is the EventStore event metadata (AD-3).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Decision">The recorded gate decision — <see cref="AgentInteractionStatus.Denied"/> or <see cref="AgentInteractionStatus.Blocked"/>.</param>
/// <param name="Blockers">The non-satisfied verdicts (the safe blocker evidence).</param>
public record AgentInteractionGateFailed(
    string AgentInteractionId,
    AgentInteractionStatus Decision,
    IReadOnlyList<AgentInvocationGateVerdict> Blockers) : IEventPayload;
