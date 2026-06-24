using System.Collections.Generic;

using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal result of reading the authorized Source Conversation content for context building (Story 2.3; AC1,
/// AC2, AC3; AD-6, AD-12). It carries the fail-closed <see cref="Outcome"/>, the loaded visible timeline
/// <see cref="Messages"/> (present only on a <see cref="AgentInteractionContextLoadOutcome.Loaded"/> read, for
/// in-memory token measurement), the <see cref="MessageCount"/>, and a coarse <see cref="IsFresh"/> flag.
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type (mirroring <c>ConversationAccessReadResult</c>). The
/// <see cref="Messages"/> hold sensitive content used ONLY transiently for measurement inside the Server adapter; they
/// are never persisted, put on a command/event/view, or logged (AD-14). The implementation wraps the public
/// Conversations authorized read seam <c>IConversationClient.GetConversationAsync</c>.
/// </remarks>
/// <param name="Outcome">The fail-closed load classification (only <see cref="AgentInteractionContextLoadOutcome.Loaded"/> permits context building).</param>
/// <param name="Messages">The loaded visible timeline messages for token measurement (non-null only on a loaded read), or <see langword="null"/>.</param>
/// <param name="MessageCount">The visible message count of the loaded timeline (<c>0</c> when not loaded).</param>
/// <param name="IsFresh">Whether the consulted Conversation read is within its freshness threshold.</param>
public sealed record ConversationContextReadResult(
    AgentInteractionContextLoadOutcome Outcome,
    IReadOnlyList<ConversationContextMessage>? Messages,
    int MessageCount,
    bool IsFresh)
{
    /// <summary>Gets the fail-closed not-available result (the deferred default) — unavailable, no messages, not fresh.</summary>
    public static ConversationContextReadResult Unavailable { get; } = new(AgentInteractionContextLoadOutcome.Unavailable, null, 0, false);
}
