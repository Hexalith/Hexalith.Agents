namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// A create-proposed-reply command could not be evaluated at all — the interaction has no recorded request, is not in
/// Confirmation Response Mode, or has not generated output (AD-12). This is a structural rejection (no state change),
/// distinct from a recorded creation-failed <em>decision</em> (<see cref="ProposedAgentReplyCreationFailed"/>): a
/// creation-failed call is a successfully-recorded fail-closed outcome, whereas an unevaluable command produces no proposal
/// record — mirroring <see cref="AgentResponseNotPostableRejection"/>.
/// </summary>
/// <remarks>
/// The rejection carries only the deterministic aggregate id plus a safe <see cref="Reason"/> classification — never the
/// prompt, generated content, <c>PartyId</c> personal data, Conversations payloads, or content (AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the create command targeted.</param>
/// <param name="Reason">The safe classification of why proposal creation could not be evaluated.</param>
public record ProposedAgentReplyNotCreatableRejection(
    string AgentInteractionId,
    AgentProposedReplyNotCreatableReason Reason) : IRejectionEvent;
