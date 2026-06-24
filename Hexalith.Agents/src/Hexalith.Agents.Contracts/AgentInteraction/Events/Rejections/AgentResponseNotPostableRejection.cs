namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// A post-response command could not be evaluated at all — the interaction has no recorded request, has not generated
/// output, or is not in Automatic Response Mode (AD-12). This is a structural rejection (no state change), distinct from a
/// recorded posting-failed <em>decision</em> (<see cref="AgentResponsePostingFailed"/>): a posting-failed call is a
/// successfully-recorded fail-closed outcome, whereas an unevaluable command produces no posting record — mirroring
/// <see cref="AgentOutputNotGeneratableRejection"/>.
/// </summary>
/// <remarks>
/// The rejection carries only the deterministic aggregate id plus a safe <see cref="Reason"/> classification — never the
/// prompt, generated content, <c>PartyId</c> personal data, Conversations payloads, or content (AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the post command targeted.</param>
/// <param name="Reason">The safe classification of why posting could not be evaluated.</param>
public record AgentResponseNotPostableRejection(
    string AgentInteractionId,
    AgentResponseNotPostableReason Reason) : IRejectionEvent;
