namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// An Agent Call request was rejected because it failed pure structural validation in the <c>AgentInteraction</c>
/// aggregate — a required caller/source/prompt field was missing, or the server-assembled configuration snapshot
/// was absent (AC1, AC4; FR-8). No interaction is created and no provider/Conversation side effect occurs (AC3).
/// </summary>
/// <remarks>
/// <see cref="Status"/> is a safe classification of <em>which</em> required field was absent; it carries no prompt,
/// Conversation-derived content, or caller PII (AC4; AD-14). The aggregate id is the deterministic interaction id —
/// no raw caller/source values are echoed.
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the request targeted.</param>
/// <param name="Status">The safe classification of the missing required field.</param>
public record InvalidAgentInteractionRequestRejection(
    string AgentInteractionId,
    AgentInteractionRequestValidationStatus Status) : IRejectionEvent;
