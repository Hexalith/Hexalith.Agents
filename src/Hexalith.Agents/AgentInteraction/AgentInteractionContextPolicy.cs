using System;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, dependency-free Conversation context-budget evaluation shared by the <see cref="AgentInteractionAggregate"/>
/// context handler and the Server context orchestration so the two can never drift (AC2, AC3, AC4; AD-3, AD-11).
/// Centralizing the measurement → decision math mirrors <see cref="AgentInvocationGatePolicy"/>: the aggregate calls
/// <see cref="Evaluate"/> to emit the outcome event; the orchestrator calls <see cref="Decide"/> to return the same
/// decided status it dispatched.
/// </summary>
/// <remarks>
/// No method here reads I/O, time, secrets, or any sibling aggregate (AD-3). It operates only on the safe
/// <see cref="AgentInteractionContextMeasurement"/> — never raw Conversation text (AD-14). Fail-closed by construction:
/// a non-loaded read, an untrustworthy budget, or an oversized context with no approved bounded behavior all block;
/// bounded context is used ONLY when an approved behavior is present and fits, and its reference is recorded as
/// evidence so it is never a silent truncation (AC4; AD-11).
/// </remarks>
internal static class AgentInteractionContextPolicy
{
    /// <summary>
    /// Evaluates the supplied measurement into the durable context outcome event (AC2, AC3, AC4). Within bounds →
    /// <see cref="AgentInteractionContextReady"/> (full or bounded); otherwise <see cref="AgentInteractionContextBlocked"/>
    /// carrying the safe block reason and evidence (the fail-closed Audit Evidence; FR-24).
    /// </summary>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id) to stamp on the emitted event.</param>
    /// <param name="measurement">The server-assembled context measurement.</param>
    /// <returns>A success domain result carrying exactly one outcome event.</returns>
    internal static DomainResult Evaluate(string interactionId, AgentInteractionContextMeasurement measurement)
    {
        ContextDecision decision = Compute(measurement);
        return decision.Status == AgentInteractionStatus.ContextReady
            ? DomainResult.Success([new AgentInteractionContextReady(interactionId, decision.Evidence)])
            : DomainResult.Success([new AgentInteractionContextBlocked(interactionId, decision.Reason, decision.Evidence)]);
    }

    /// <summary>
    /// Computes the decided context status from the supplied measurement using the same rule as <see cref="Evaluate"/>
    /// (within bounds → <see cref="AgentInteractionStatus.ContextReady"/>; otherwise
    /// <see cref="AgentInteractionStatus.ContextBlocked"/>). The orchestrator returns this so its reported status cannot
    /// drift from the aggregate's recorded decision.
    /// </summary>
    /// <param name="measurement">The server-assembled context measurement.</param>
    /// <returns>The decided terminal context status.</returns>
    internal static AgentInteractionStatus Decide(AgentInteractionContextMeasurement measurement)
        => Compute(measurement).Status;

    // The deterministic budget decision (AC2/AC3/AC4 — the "Context Budget Rules" table). Evaluated in order:
    // (1) load failure → block; (2) untrustworthy budget → block; (3..4) fits → full; (5) approved bounded fits →
    // bounded; (6) oversized with no approved bounded behavior → block (never a silent truncation).
    private static ContextDecision Compute(AgentInteractionContextMeasurement m)
    {
        ArgumentNullException.ThrowIfNull(m);

        // (1) The Conversations read did not load authorized + fresh enough → block; numerics are zeroed (AC1/AC3).
        if (m.LoadOutcome != AgentInteractionContextLoadOutcome.Loaded)
        {
            AgentInteractionContextBlockReason reason = m.LoadOutcome == AgentInteractionContextLoadOutcome.Stale
                ? AgentInteractionContextBlockReason.ContextNotFresh
                : AgentInteractionContextBlockReason.ContextUnavailable; // Unauthorized / Unavailable / Unknown
            return Blocked(reason, ZeroedEvidence(m));
        }

        // (2) The model budget could not be trusted → block (fail closed; AC2/AC3).
        if (m.ContextWindowTokenLimit <= 0
            || m.ReservedOutputTokenCount < 0
            || m.ReservedOutputTokenCount >= m.ContextWindowTokenLimit
            || m.ProviderCapabilityVersion <= 0)
        {
            return Blocked(AgentInteractionContextBlockReason.ModelBudgetUnavailable, MeasuredEvidence(m, AgentInteractionContextMode.Unknown, 0));
        }

        // (3) The space available for context after reserving the configured output tokens (AC2).
        int availableContextBudget = m.ContextWindowTokenLimit - m.ReservedOutputTokenCount;

        // (4) The full Source Conversation fits → full context (AC2).
        if (m.FullContextTokenCount <= availableContextBudget)
        {
            return Ready(MeasuredEvidence(m, AgentInteractionContextMode.Full, m.FullContextTokenCount));
        }

        // (5) Oversized, but an approved bounded behavior fits → bounded context with its reference recorded (AC4).
        if (m.ApprovedBoundedBehavior is { } bounded
            && bounded.BoundedContextTokenLimit > 0
            && bounded.BoundedContextTokenLimit <= availableContextBudget)
        {
            int used = Math.Min(m.FullContextTokenCount, bounded.BoundedContextTokenLimit);
            AgentInteractionContextEvidence evidence = MeasuredEvidence(m, AgentInteractionContextMode.Bounded, used)
                with { BoundedBehaviorReference = bounded.BehaviorReference };
            return Ready(evidence);
        }

        // (6) Oversized with no approved bounded behavior → block (AC3 — never a silent truncation). Evidence records
        // the overflow (FullContextTokenCount + budget) so audit shows WHY.
        return Blocked(AgentInteractionContextBlockReason.ExceedsModelBudget, MeasuredEvidence(m, AgentInteractionContextMode.Unknown, 0));
    }

    // Evidence carrying the measured numerics/budget/policy (for a within-budget decision or a budget/overflow block).
    private static AgentInteractionContextEvidence MeasuredEvidence(AgentInteractionContextMeasurement m, AgentInteractionContextMode mode, int usedTokens)
        => new(
            mode,
            m.FullContextTokenCount,
            usedTokens,
            m.MessageCount,
            m.ReservedOutputTokenCount,
            m.ContextWindowTokenLimit,
            m.ProviderCapabilityVersion,
            m.ContextPolicyReference,
            BoundedBehaviorReference: null);

    // Evidence for a blocked-before-load record: numerics are 0/Unknown (the read failed before measurement), but the
    // policy reference is retained so audit still shows which policy was in force.
    private static AgentInteractionContextEvidence ZeroedEvidence(AgentInteractionContextMeasurement m)
        => new(
            AgentInteractionContextMode.Unknown,
            FullContextTokenCount: 0,
            UsedContextTokenCount: 0,
            MessageCount: 0,
            ReservedOutputTokenCount: 0,
            ContextWindowTokenLimit: 0,
            ProviderCapabilityVersion: 0,
            m.ContextPolicyReference,
            BoundedBehaviorReference: null);

    private static ContextDecision Ready(AgentInteractionContextEvidence evidence)
        => new(AgentInteractionStatus.ContextReady, AgentInteractionContextBlockReason.Unknown, evidence);

    private static ContextDecision Blocked(AgentInteractionContextBlockReason reason, AgentInteractionContextEvidence evidence)
        => new(AgentInteractionStatus.ContextBlocked, reason, evidence);

    // The internal carrier of one computed decision: the status, the block reason (Unknown when ready), and the safe
    // evidence. Lets Evaluate and Decide share the exact same computation.
    private readonly record struct ContextDecision(
        AgentInteractionStatus Status,
        AgentInteractionContextBlockReason Reason,
        AgentInteractionContextEvidence Evidence);
}
