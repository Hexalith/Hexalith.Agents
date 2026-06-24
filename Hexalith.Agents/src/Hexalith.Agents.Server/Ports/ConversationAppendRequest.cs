namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal request to append the generated response to the Source Conversation, authored by the Agent Party with a
/// deterministic idempotent message identity (Story 2.5; AC2, AC3; AD-13, AD-14). The sensitive <see cref="Text"/> stays
/// inside the adapter — it is the ONLY content-bearing field and never crosses back into the aggregate command/result/event.
/// </summary>
/// <param name="TenantId">The interaction's tenant scope (the Conversations command tenant binding).</param>
/// <param name="SourceConversationId">The source Conversation reference the message is appended to (opaque — AD-6).</param>
/// <param name="AuthorPartyId">The Agent's stable Party reference the message is authored by (AC2; AD-7).</param>
/// <param name="Text">The generated content to post — sensitive; stays inside the adapter (AD-14).</param>
/// <param name="MessageId">The deterministic Conversation Message identifier derived from interaction + version (AD-13).</param>
/// <param name="IdempotencyKey">The deterministic idempotency key so a retry dedupes the append (AD-13).</param>
/// <param name="CorrelationId">The safe request correlation id threaded to the Conversations command.</param>
public sealed record ConversationAppendRequest(
    string TenantId,
    string SourceConversationId,
    string AuthorPartyId,
    string Text,
    string MessageId,
    string IdempotencyKey,
    string CorrelationId);
