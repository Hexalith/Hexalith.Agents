namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// A regenerate-proposed-reply command could not be evaluated at all — the interaction has no pending proposal, or the
/// proposal has reached a terminal/non-pending state (AC4; AD-12). This is a structural rejection (no state change, no new
/// version, no provider invocation), distinct from a recorded regeneration-failed <em>decision</em>
/// (<see cref="ProposedAgentReplyRegenerationFailed"/>): a regeneration-failed call is a successfully-recorded fail-closed
/// outcome that keeps the proposal retryable, whereas an unevaluable command produces no regeneration record — mirroring
/// <see cref="ProposedAgentReplyNotEditableRejection"/>. This is the structural enforcement of AC4: a terminal proposal can
/// never invoke the provider.
/// </summary>
/// <remarks>
/// The rejection carries only the deterministic aggregate id plus a safe <see cref="Reason"/> classification — never the
/// regenerated content, generated content, <c>PartyId</c> personal data, Conversations payloads, or content (AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the regenerate command targeted.</param>
/// <param name="Reason">The safe classification of why the proposal could not be regenerated.</param>
public record ProposedAgentReplyNotRegeneratableRejection(
    string AgentInteractionId,
    AgentProposedReplyNotRegeneratableReason Reason) : IRejectionEvent;
