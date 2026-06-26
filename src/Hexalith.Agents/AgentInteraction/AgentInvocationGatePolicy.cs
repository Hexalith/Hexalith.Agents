using System.Collections.Generic;
using System.Linq;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, dependency-free invocation-gate evaluation shared by the <see cref="AgentInteractionAggregate"/> gate handler
/// and the Server gate orchestration so the two can never drift (AC1–AC4; AD-12). Centralizing the blockers → decision
/// math mirrors <see cref="AgentInteractionRequestPolicy"/> and the Agent activation gate's
/// <c>ComputeActivationBlockers</c> precedent: the aggregate calls <see cref="Evaluate"/> to emit the outcome event;
/// the orchestrator calls <see cref="Decide"/> to return the same decided status it dispatched.
/// </summary>
/// <remarks>
/// No method here reads I/O, time, secrets, or any sibling aggregate (AD-3). It operates only on the safe
/// <see cref="AgentInvocationGateVerdict"/> classifications — never claims, tokens, Party PII, provider payloads, or
/// content (AD-14). Fail-closed by construction: any verdict whose outcome is not
/// <see cref="AgentInteractionGateOutcome.Satisfied"/> is a blocker, and an authorization-class blocker dominates so a
/// failed-closed authorization is never downgraded to "blocked" (AC2/AC3 more-restrictive rule).
/// </remarks>
internal static class AgentInvocationGatePolicy
{
    /// <summary>
    /// The authorization-class checks (FR-19/FR-20). A blocker on any of these decides <see cref="AgentInteractionStatus.Denied"/>;
    /// the remaining six checks are readiness-class and decide <see cref="AgentInteractionStatus.Blocked"/> (FR-21).
    /// </summary>
    private static readonly AgentInteractionGateCheck[] _authorizationClassChecks =
    [
        AgentInteractionGateCheck.TenantAccess,
        AgentInteractionGateCheck.CallerPartyState,
        AgentInteractionGateCheck.SourceConversationAccess,
    ];

    /// <summary>
    /// Evaluates the supplied verdicts into the durable gate outcome event (AC1, AC2). All checks satisfied →
    /// <see cref="AgentInteractionAuthorized"/>; otherwise <see cref="AgentInteractionGateFailed"/> carrying the
    /// decided status and the non-satisfied blockers (the safe Audit Evidence; FR-24).
    /// </summary>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id) to stamp on the emitted event.</param>
    /// <param name="verdicts">The server-assembled per-check verdicts.</param>
    /// <returns>A success domain result carrying exactly one outcome event.</returns>
    internal static DomainResult Evaluate(string interactionId, IReadOnlyList<AgentInvocationGateVerdict> verdicts)
    {
        IReadOnlyList<AgentInvocationGateVerdict> blockers = Blockers(verdicts);
        return blockers.Count == 0
            ? DomainResult.Success([new AgentInteractionAuthorized(interactionId)])
            : DomainResult.Success([new AgentInteractionGateFailed(interactionId, Classify(blockers), blockers)]);
    }

    /// <summary>
    /// Computes the decided gate status from the supplied verdicts using the same rule as <see cref="Evaluate"/>
    /// (none blocking → <see cref="AgentInteractionStatus.Authorized"/>; any authorization-class blocker →
    /// <see cref="AgentInteractionStatus.Denied"/>; else <see cref="AgentInteractionStatus.Blocked"/>). The orchestrator
    /// returns this so its reported status cannot drift from the aggregate's recorded decision.
    /// </summary>
    /// <param name="verdicts">The server-assembled per-check verdicts.</param>
    /// <returns>The decided terminal gate status.</returns>
    internal static AgentInteractionStatus Decide(IReadOnlyList<AgentInvocationGateVerdict> verdicts)
    {
        IReadOnlyList<AgentInvocationGateVerdict> blockers = Blockers(verdicts);
        return blockers.Count == 0 ? AgentInteractionStatus.Authorized : Classify(blockers);
    }

    /// <summary>The blocking verdicts — those whose outcome is not <see cref="AgentInteractionGateOutcome.Satisfied"/>.</summary>
    /// <param name="verdicts">The verdicts to filter.</param>
    /// <returns>The non-satisfied verdicts, in their original order.</returns>
    private static IReadOnlyList<AgentInvocationGateVerdict> Blockers(IReadOnlyList<AgentInvocationGateVerdict> verdicts)
        => verdicts.Where(v => v.Outcome != AgentInteractionGateOutcome.Satisfied).ToList();

    /// <summary>Classifies a non-empty blocker set: any authorization-class blocker → Denied, else Blocked (Denied precedence).</summary>
    /// <param name="blockers">The non-empty blocker set.</param>
    /// <returns><see cref="AgentInteractionStatus.Denied"/> or <see cref="AgentInteractionStatus.Blocked"/>.</returns>
    private static AgentInteractionStatus Classify(IReadOnlyList<AgentInvocationGateVerdict> blockers)
        => blockers.Any(b => _authorizationClassChecks.Contains(b.Check))
            ? AgentInteractionStatus.Denied
            : AgentInteractionStatus.Blocked;
}
