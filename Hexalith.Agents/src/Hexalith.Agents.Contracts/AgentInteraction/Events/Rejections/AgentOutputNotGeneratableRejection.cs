namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// A generate-output command could not be evaluated at all — the interaction has no recorded request, or it has not built
/// its Conversation context (AD-12). This is a structural rejection (no state change), distinct from a recorded
/// generation-failed <em>decision</em> (<see cref="AgentOutputGenerationFailed"/>): a generation-failed call is a
/// successfully-recorded fail-closed outcome, whereas an unevaluable command produces no generation record — mirroring
/// <see cref="AgentInteractionContextNotBuildableRejection"/>.
/// </summary>
/// <remarks>
/// The rejection carries only the deterministic aggregate id plus a safe <see cref="Reason"/> classification — never the
/// prompt, generated content, claims, tokens, <c>PartyId</c> personal data, provider payloads, or content (AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the generate command targeted.</param>
/// <param name="Reason">The safe classification of why generation could not be evaluated.</param>
public record AgentOutputNotGeneratableRejection(
    string AgentInteractionId,
    AgentOutputNotGeneratableReason Reason) : IRejectionEvent;
