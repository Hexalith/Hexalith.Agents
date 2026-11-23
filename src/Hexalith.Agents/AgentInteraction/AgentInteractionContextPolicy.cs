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
    private const int DefaultSafetyMarginPercent = 10;

    /// <summary>
    /// Evaluates the supplied measurement into the durable context outcome event (AC2, AC3, AC4). Within bounds →
    /// <see cref="AgentInteractionContextReady"/> (full or bounded); otherwise <see cref="AgentInteractionContextBlocked"/>
    /// carrying the safe block reason and evidence (the fail-closed Audit Evidence; FR-24).
    /// </summary>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id) to stamp on the emitted event.</param>
    /// <param name="measurement">The server-assembled context measurement.</param>
    /// <param name="capabilityFloor">The minimum capability version from the interaction snapshot.</param>
    /// <returns>A success domain result carrying exactly one outcome event.</returns>
    internal static DomainResult Evaluate(string interactionId, AgentInteractionContextMeasurement measurement, int capabilityFloor)
    {
        ContextDecision decision = Compute(measurement, capabilityFloor);
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
    /// <param name="capabilityFloor">The minimum capability version from the interaction snapshot.</param>
    /// <returns>The decided terminal context status.</returns>
    internal static AgentInteractionStatus Decide(AgentInteractionContextMeasurement measurement, int capabilityFloor)
        => Compute(measurement, capabilityFloor).Status;

    /// <summary>Computes the current payload budget, rounding the default safety margin up to a whole token.</summary>
    internal static (int MarginTokens, int AvailableTokens) CalculateDefaultBudget(int windowTokens, int reservedOutputTokens)
    {
        int marginTokens = (int)(((long)windowTokens * DefaultSafetyMarginPercent + 99) / 100);
        return (marginTokens, windowTokens - reservedOutputTokens - marginTokens);
    }

    // The deterministic budget decision (AC2/AC3/AC4 — the "Context Budget Rules" table). Evaluated in order:
    // (1) load failure → block; (2) untrustworthy budget → block; (3..4) fits → full; (5) approved bounded fits →
    // bounded; (6) oversized with no approved bounded behavior → block (never a silent truncation).
    private static ContextDecision Compute(AgentInteractionContextMeasurement m, int capabilityFloor)
    {
        ArgumentNullException.ThrowIfNull(m);

        // (1) The Conversations read did not load authorized + fresh enough → block; numerics are zeroed (AC1/AC3).
        if (m.LoadOutcome != AgentInteractionContextLoadOutcome.Loaded)
        {
            AgentInteractionContextBlockReason reason = m.LoadOutcome switch
            {
                AgentInteractionContextLoadOutcome.Stale => AgentInteractionContextBlockReason.ContextNotFresh,
                AgentInteractionContextLoadOutcome.Unavailable => AgentInteractionContextBlockReason.ContextReadUnavailable,
                _ => AgentInteractionContextBlockReason.ContextUnavailable,
            };
            return Blocked(reason, ZeroedEvidence(m));
        }

        // (2) The model budget could not be trusted → block (fail closed; AC2/AC3).
        if (m.FullContextTokenCount < 0 || m.MessageCount < 0 || (m.FullContextTokenCount == 0 && m.MessageCount > 0))
        {
            return Blocked(AgentInteractionContextBlockReason.ModelBudgetUnavailable, ZeroedEvidence(m));
        }

        if (m.ContextWindowTokenLimit <= 0
            || m.ReservedOutputTokenCount <= 0
            || m.ReservedOutputTokenCount >= m.ContextWindowTokenLimit
            || capabilityFloor <= 0
            || m.ProviderCapabilityVersion < capabilityFloor)
        {
            AgentInteractionContextEvidence evidence = ZeroedEvidence(m) with
            {
                FullContextTokenCount = m.FullContextTokenCount,
                MessageCount = m.MessageCount,
            };
            return Blocked(AgentInteractionContextBlockReason.ModelBudgetUnavailable, evidence);
        }

        (int marginTokens, int availableContextBudget) = CalculateDefaultBudget(m.ContextWindowTokenLimit, m.ReservedOutputTokenCount);
        AgentInteractionContextEvidence measured = new(
            AgentInteractionContextMode.Blocked,
            m.FullContextTokenCount,
            UsedContextTokenCount: 0,
            m.MessageCount,
            m.ReservedOutputTokenCount,
            m.ContextWindowTokenLimit,
            m.ProviderCapabilityVersion,
            m.ContextPolicyReference,
            BoundedBehaviorReference: null)
        {
            SafetyMarginPercent = DefaultSafetyMarginPercent,
            SafetyMarginTokenCount = marginTokens,
            AvailableContextTokenBudget = availableContextBudget,
        };

        // (4) The full Source Conversation fits → full context (AC2).
        if (m.FullContextTokenCount <= availableContextBudget)
        {
            return Ready(measured with { Mode = AgentInteractionContextMode.Full, UsedContextTokenCount = m.FullContextTokenCount });
        }

        // (5) Oversized, but an approved bounded behavior fits → bounded context with its reference recorded (AC4).
        if (m.ApprovedBoundedBehavior is { } bounded
            && bounded.BoundedContextTokenLimit > 0
            && bounded.BoundedContextTokenLimit <= availableContextBudget)
        {
            return Ready(measured with
            {
                Mode = AgentInteractionContextMode.Bounded,
                UsedContextTokenCount = bounded.BoundedContextTokenLimit,
                BoundedBehaviorReference = bounded.BehaviorReference,
            });
        }

        // (6) Oversized with no approved bounded behavior → block (AC3 — never a silent truncation). Evidence records
        // the overflow (FullContextTokenCount + budget) so audit shows WHY.
        return Blocked(AgentInteractionContextBlockReason.ExceedsModelBudget, measured);
    }

    // Evidence for a blocked-before-load record: numerics are 0/Unknown (the read failed before measurement), but the
    // policy reference is retained so audit still shows which policy was in force.
    private static AgentInteractionContextEvidence ZeroedEvidence(AgentInteractionContextMeasurement m)
        => new(
            AgentInteractionContextMode.Blocked,
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
