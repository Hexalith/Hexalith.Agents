namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The server-assembled input to the pure posting decision (AC1–AC4; AD-3, AD-14). The orchestration assembles this from
/// the Agent Party read, the selected generated version read, the Conversations membership ensure, and the message append,
/// then puts it on <see cref="Commands.PostAgentResponse"/>; the pure aggregate decides on it and never reads any
/// dependency itself (AD-3). It mirrors <see cref="AgentOutputGenerationResult"/> as the server→aggregate carrier.
/// </summary>
/// <remarks>
/// <b>Crucially, it carries NO generated content</b> — the content was already durably recorded on
/// <see cref="Events.AgentOutputGenerated"/>/state by Story 2.4; posting transports only safe ids into the aggregate
/// (AD-14). Every member is a safe id: the deterministic <see cref="MessageId"/> (AD-13), the opaque
/// <see cref="SourceConversationId"/>, the Agent's stable <see cref="AgentPartyId"/> (a reference, not PII — AD-7), and
/// the selected <see cref="PostedVersionId"/>. On a fail-closed outcome the ids are the ones that were attempted.
/// </remarks>
/// <param name="Outcome">The server-assembled outcome classification the aggregate decides on.</param>
/// <param name="MessageId">The deterministic Conversation Message identifier (reused across retries — AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the message was appended to (an opaque reference — AD-6).</param>
/// <param name="AgentPartyId">The Agent's stable Party reference the message is authored by (a reference, not PII — AD-7).</param>
/// <param name="PostedVersionId">The selected generated version identifier that was posted (no content — AD-14).</param>
public record AgentResponsePostingResult(
    AgentResponsePostingOutcome Outcome,
    string MessageId,
    string SourceConversationId,
    string AgentPartyId,
    string PostedVersionId);
