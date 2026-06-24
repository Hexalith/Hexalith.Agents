using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IConversationAccessReader"/> registered so the Server DI graph is complete and compiles
/// cleanly, while the live binding over the Conversations <c>IConversationClient.GetConversationAsync</c> seam is
/// deferred to Story 2.3 (mirroring <c>DeferredAgentConfigurationSnapshotReader</c>). It is never exercised by this
/// story's unit tests, which substitute the seam.
/// </summary>
/// <remarks>
/// Like the snapshot-reader seam, this placeholder <b>fails closed by returning</b>
/// <see cref="ConversationAccessReadResult.Unavailable"/>: an accidental live call yields a clean <c>Denied</c> gate
/// (Source Conversation access is authorization-class) rather than a runtime fault (AD-12; FR-21).
/// </remarks>
public sealed class DeferredConversationAccessReader : IConversationAccessReader
{
    /// <inheritdoc />
    public Task<ConversationAccessReadResult> ReadAsync(string tenantId, string sourceConversationId, string callerPartyId, CancellationToken ct)
        => Task.FromResult(ConversationAccessReadResult.Unavailable);
}
