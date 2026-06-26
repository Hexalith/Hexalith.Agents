namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, audit-ready evidence of one automatic-posting attempt, recorded on <see cref="Events.AgentResponsePosted"/> and
/// <see cref="Events.AgentResponsePostingFailed"/> so an authorized administrator can see WHICH message was posted (or
/// attempted) and against which Conversation, by which Agent Party, for which generated version — without any generated
/// content (AC2, AC4; AD-7, AD-14). It mirrors <see cref="AgentGenerationAttemptEvidence"/>: safe ids only.
/// </summary>
/// <remarks>
/// Carries ONLY safe ids — deliberately NEVER the generated content, a raw Conversations payload, a Conversations-specific
/// error string, a stack trace, or a secret (AD-14). <see cref="AgentPartyId"/> is a stable Party reference, not PII
/// (AD-7). The ids are the deterministic <see cref="MessageId"/> (derived from interaction + version; AD-13), the opaque
/// Conversations-owned <see cref="SourceConversationId"/>, the Agent's <see cref="AgentPartyId"/>, and the selected
/// <see cref="PostedVersionId"/> that was (or would have been) posted.
/// </remarks>
/// <param name="MessageId">The deterministic Conversation Message identifier derived from the interaction + selected version (AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the message was appended to (an opaque reference — AD-6).</param>
/// <param name="AgentPartyId">The Agent's stable Party reference the message is authored by (a reference, not PII — AD-7).</param>
/// <param name="PostedVersionId">The selected generated version identifier that was posted (no content; AD-14).</param>
public record AgentPostedMessageEvidence(
    string MessageId,
    string SourceConversationId,
    string AgentPartyId,
    string PostedVersionId);
