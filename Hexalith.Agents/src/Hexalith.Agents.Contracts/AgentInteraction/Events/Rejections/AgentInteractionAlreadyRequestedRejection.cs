namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// An Agent Call request was rejected because a <em>conflicting</em> payload was submitted under a deterministic
/// interaction id that already has a recorded request (AC2; AD-13). Re-issuing the <em>same</em> call is a
/// deterministic no-op; only a payload that differs from the recorded one on the same id is rejected here, so the
/// recorded interaction is never silently mutated.
/// </summary>
/// <remarks>
/// The rejection carries only the deterministic aggregate id — never the prompt, Conversation content, caller PII,
/// or the conflicting field values (AC4; AD-14). It gives an auditable trail of the idempotency conflict without
/// exposing sensitive content.
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier whose recorded request the new payload conflicted with.</param>
public record AgentInteractionAlreadyRequestedRejection(
    string AgentInteractionId) : IRejectionEvent;
