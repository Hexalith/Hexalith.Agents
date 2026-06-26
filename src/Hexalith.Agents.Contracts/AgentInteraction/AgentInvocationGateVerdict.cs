namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// One evaluated invocation gate check and its fail-closed outcome (AC1, AC4; AD-12, AD-14). The orchestration
/// produces exactly one verdict per <see cref="AgentInteractionGateCheck"/>; the pure aggregate blocks on any verdict
/// whose <see cref="Outcome"/> is not <see cref="AgentInteractionGateOutcome.Satisfied"/>. A verdict is a
/// <em>blocker</em> iff <see cref="Outcome"/> != <see cref="AgentInteractionGateOutcome.Satisfied"/>.
/// </summary>
/// <remarks>
/// A verdict carries ONLY the two safe enums — never raw claims, JWTs/tokens, <c>PartyId</c> personal data, provider
/// payloads, configured policy values, content, or messages (AD-14, AC4). It is the single safe unit that records,
/// and later lets an administrator distinguish, the gate failure classes.
/// </remarks>
/// <param name="Check">The dependency check this verdict classifies.</param>
/// <param name="Outcome">The fail-closed outcome of that check.</param>
public record AgentInvocationGateVerdict(
    AgentInteractionGateCheck Check,
    AgentInteractionGateOutcome Outcome);
