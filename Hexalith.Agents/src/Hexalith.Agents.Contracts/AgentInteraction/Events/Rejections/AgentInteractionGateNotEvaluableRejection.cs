namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// An invocation gate command could not be evaluated at all — the interaction has no recorded request, or no verdicts
/// were supplied (AC4; AD-12). This is a structural rejection (no state change), distinct from a recorded
/// denied/blocked <em>decision</em> (<see cref="AgentInteractionGateFailed"/>): a denied call is a
/// successfully-recorded negative gate outcome, whereas an unevaluable command produces no gate record — mirroring how
/// <c>AgentActivationBlockedRejection</c> leaves the Agent untouched.
/// </summary>
/// <remarks>
/// The rejection carries only the deterministic aggregate id plus a safe <see cref="Reason"/> classification — never
/// the prompt, claims, tokens, <c>PartyId</c> personal data, provider payloads, or content (AC4; AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the gate command targeted.</param>
/// <param name="Reason">The safe classification of why the gate could not be evaluated.</param>
public record AgentInteractionGateNotEvaluableRejection(
    string AgentInteractionId,
    AgentInteractionGateNotEvaluableReason Reason) : IRejectionEvent;
