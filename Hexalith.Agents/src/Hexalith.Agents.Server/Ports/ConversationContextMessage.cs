using System;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal, transient carrier of one visible Conversation timeline message, held ONLY in memory for token
/// measurement during context building (Story 2.3; AD-14). This is the sensitive "context-derived content" class — the
/// same class as the caller prompt — so it is NEVER serialized to EventStore, NEVER put on a command/event/view/result/
/// state, and NEVER logged. It lives inside the Server adapter boundary and is discarded after measurement; Conversations
/// remains the owner of the content.
/// </summary>
/// <param name="Text">The visible message text (sensitive — used only transiently for token measurement).</param>
/// <param name="CreatedAt">The message creation timestamp (used only for stable ordering).</param>
public sealed record ConversationContextMessage(
    string Text,
    DateTimeOffset CreatedAt);
