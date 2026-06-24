using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IConversationResponsePoster"/> registered when no <c>Conversations</c> config section is present,
/// so the Server DI graph is complete and compiles cleanly while the live binding over the Conversations
/// <c>IConversationClient</c> seam stays unwired (mirroring <c>DeferredConversationContextReader</c>). It is never
/// exercised by this story's unit tests, which substitute the seam.
/// </summary>
/// <remarks>
/// This placeholder <b>fails closed</b>: membership returns <see cref="ConversationMembershipResult.SeamUnavailable"/> and
/// append returns <see cref="ConversationAppendResult.ConversationUnavailable"/>, so an accidental production call yields a
/// clean <c>PostingFailed</c> rather than a runtime fault and never posts a Conversation Message (AD-6, AD-12).
/// </remarks>
public sealed class DeferredConversationResponsePoster : IConversationResponsePoster
{
    /// <inheritdoc />
    public Task<ConversationMembershipResult> EnsureAiAgentParticipantAsync(ConversationMembershipRequest request, CancellationToken ct)
        => Task.FromResult(ConversationMembershipResult.SeamUnavailable);

    /// <inheritdoc />
    public Task<ConversationAppendResult> AppendAgentMessageAsync(ConversationAppendRequest request, CancellationToken ct)
        => Task.FromResult(ConversationAppendResult.ConversationUnavailable);
}
