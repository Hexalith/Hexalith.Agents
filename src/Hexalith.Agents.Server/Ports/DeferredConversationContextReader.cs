using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IConversationContextReader"/> registered when no <c>Conversations</c> config section is
/// present, so the Server DI graph is complete and compiles cleanly while the live binding over the Conversations
/// <c>IConversationClient.GetConversationAsync</c> seam stays unwired (mirroring <c>DeferredConversationAccessReader</c>).
/// It is never exercised by this story's unit tests, which substitute the seam.
/// </summary>
/// <remarks>
/// This placeholder <b>fails closed by returning</b> <see cref="ConversationContextReadResult.Unavailable"/>: an
/// accidental production call yields a clean <c>ContextBlocked(ContextUnavailable)</c> rather than a runtime fault
/// (AD-12; FR-21).
/// </remarks>
public sealed class DeferredConversationContextReader : IConversationContextReader
{
    /// <inheritdoc />
    public Task<ConversationContextReadResult> ReadAsync(string tenantId, string sourceConversationId, string callerPartyId, string callerPrincipalId, CancellationToken ct)
        => Task.FromResult(ConversationContextReadResult.Unavailable);
}
