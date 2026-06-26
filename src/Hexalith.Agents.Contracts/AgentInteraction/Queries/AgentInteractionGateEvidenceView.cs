using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Safe, audit-ready projection of an Agent Call's invocation gate evidence for authorized inspection (AC4; FR-24).
/// The <see cref="Verdicts"/> list lets an authorized administrator distinguish authorization vs dependency vs
/// Agent-readiness vs Provider-readiness vs policy failures (via each verdict's <see cref="AgentInteractionGateCheck"/>
/// category) without exposing anything sensitive.
/// </summary>
/// <remarks>
/// The view carries ONLY the safe identity reference, the coarse <see cref="Status"/>, and the safe per-check
/// verdicts — deliberately never the prompt, any Conversation-derived content, raw claims, tokens, <c>PartyId</c>
/// personal data, provider payloads, configured policy values, an EventStore stream name, or a stack trace (AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Status">The coarse Agent Call status (the recorded gate decision: Authorized/Denied/Blocked, or an earlier state).</param>
/// <param name="Verdicts">The safe per-check gate verdicts recorded for the interaction.</param>
public record AgentInteractionGateEvidenceView(
    string AgentInteractionId,
    AgentInteractionStatus Status,
    IReadOnlyList<AgentInvocationGateVerdict> Verdicts);
