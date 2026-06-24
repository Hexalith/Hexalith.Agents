namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// The safe inputs the pattern-agnostic invocation panel assembles for one Conversation-originated Agent Call (AC1). It
/// carries only the Source Conversation reference, the participant's prompt, and an optional client correlation id — it
/// does NOT construct the trusted <c>AgentId</c>/<c>Snapshot</c>, which the server populates per
/// <c>RequestAgentInteraction</c>.
/// </summary>
/// <remarks>
/// The <see cref="Prompt"/> is sensitive content (AD-14): it flows to the gateway only and is never echoed into a badge
/// label, accessible name, tooltip, <c>data-testid</c>, <c>aria-live</c> announcement, or log.
/// </remarks>
/// <param name="SourceConversationId">The source Conversation reference (an opaque reference — AD-6).</param>
/// <param name="Prompt">The participant's prompt (sensitive content — carried to the gateway only; AD-14).</param>
/// <param name="ClientCorrelationId">An optional client correlation id for idempotency/telemetry.</param>
public record ConversationAgentCallRequest(
    string SourceConversationId,
    string Prompt,
    string? ClientCorrelationId);
