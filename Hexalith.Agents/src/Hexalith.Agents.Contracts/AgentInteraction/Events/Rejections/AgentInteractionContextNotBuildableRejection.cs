namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// A context-build command could not be evaluated at all — the interaction has no recorded request, or it has not
/// passed the invocation gate (AD-12). This is a structural rejection (no state change), distinct from a recorded
/// context-blocked <em>decision</em> (<see cref="AgentInteractionContextBlocked"/>): a context-blocked call is a
/// successfully-recorded fail-closed outcome, whereas an unevaluable command produces no context record — mirroring
/// <see cref="AgentInteractionGateNotEvaluableRejection"/>.
/// </summary>
/// <remarks>
/// The rejection carries only the deterministic aggregate id plus a safe <see cref="Reason"/> classification — never
/// the prompt, claims, tokens, <c>PartyId</c> personal data, provider payloads, or content (AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the context command targeted.</param>
/// <param name="Reason">The safe classification of why context could not be evaluated.</param>
public record AgentInteractionContextNotBuildableRejection(
    string AgentInteractionId,
    AgentInteractionContextNotBuildableReason Reason) : IRejectionEvent;
