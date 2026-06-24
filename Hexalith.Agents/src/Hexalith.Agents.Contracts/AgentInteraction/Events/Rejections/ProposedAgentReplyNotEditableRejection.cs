namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// An edit-proposed-reply command could not be evaluated at all — the interaction has no pending proposal, or the proposal
/// has reached a terminal/non-pending state (AC2; AD-12). This is a structural rejection (no state change, no new version),
/// distinct from a recorded edit-failed <em>decision</em> (<see cref="ProposedAgentReplyEditFailed"/>): an edit-failed call
/// is a successfully-recorded fail-closed outcome, whereas an unevaluable command produces no edit record — mirroring
/// <see cref="ProposedAgentReplyNotCreatableRejection"/>.
/// </summary>
/// <remarks>
/// The rejection carries only the deterministic aggregate id plus a safe <see cref="Reason"/> classification — never the
/// edited content, generated content, <c>PartyId</c> personal data, Conversations payloads, or content (AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the edit command targeted.</param>
/// <param name="Reason">The safe classification of why the proposal could not be edited.</param>
public record ProposedAgentReplyNotEditableRejection(
    string AgentInteractionId,
    AgentProposedReplyNotEditableReason Reason) : IRejectionEvent;
