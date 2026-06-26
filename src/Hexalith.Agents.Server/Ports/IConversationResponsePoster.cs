using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the posting orchestration uses to (1) verify/establish the Agent as an <c>AiAgent</c> participant
/// of the Source Conversation through a Conversations-owned membership seam and (2) append the generated response as a
/// Conversation Message authored by the Agent Party identity (Story 2.5; AC1, AC2, AC3; AD-3, AD-6, AD-7, AD-13, AD-14). It
/// is the Agents-owned wrapper over the Conversations <b>public</b> client (<c>IConversationClient</c>); Agents NEVER writes
/// Conversation streams/events directly. The live binding (<c>ConversationClientResponsePoster</c>) is registered only
/// behind a <c>Conversations</c> config section; the deferred default fails closed.
/// </summary>
/// <remarks>
/// Keeping this a port preserves the AD-3 round-trip: the pure aggregate receives the posting outcome through the trusted
/// <c>PostAgentResponse</c> command, never by calling Conversations itself. The sensitive generated content rides ONLY on
/// the <see cref="ConversationAppendRequest.Text"/> and stays inside the adapter (AD-14). Every operation fails closed and
/// never propagates a raw Conversations error/payload (AD-12, AD-14).
/// </remarks>
public interface IConversationResponsePoster
{
    /// <summary>Verifies (and, when the seam exists, establishes) the Agent as an <c>AiAgent</c> participant, fail-closed (AC1).</summary>
    /// <param name="request">The safe membership request (no content).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The fail-closed membership outcome.</returns>
    Task<ConversationMembershipResult> EnsureAiAgentParticipantAsync(ConversationMembershipRequest request, CancellationToken ct);

    /// <summary>Appends the message authored by the Agent Party with the deterministic message id / idempotency key, fail-closed (AC2, AC3).</summary>
    /// <param name="request">The append request (the sensitive content stays inside the adapter — AD-14).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The fail-closed append outcome.</returns>
    Task<ConversationAppendResult> AppendAgentMessageAsync(ConversationAppendRequest request, CancellationToken ct);
}
