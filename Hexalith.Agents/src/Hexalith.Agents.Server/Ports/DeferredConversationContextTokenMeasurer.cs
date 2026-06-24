using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IConversationContextTokenMeasurer"/> registered so the Server DI graph is complete and
/// compiles cleanly, while the live token-measurement binding stays deferred (no tokenizer library is bound — provider
/// SDK is "deferred, adapter-local when selected"). It is never exercised by this story's unit tests, which substitute
/// the seam.
/// </summary>
/// <remarks>
/// This placeholder <b>fails closed by returning</b> <see cref="ConversationContextTokenMeasurement.NotAvailable"/>: the
/// budget decision then maps to <c>ModelBudgetUnavailable</c> and blocks, rather than guessing a token count
/// (AD-12; FR-21).
/// </remarks>
public sealed class DeferredConversationContextTokenMeasurer : IConversationContextTokenMeasurer
{
    /// <inheritdoc />
    public Task<ConversationContextTokenMeasurement> MeasureAsync(IReadOnlyList<ConversationContextMessage> messages, string providerId, string modelId, CancellationToken ct)
        => Task.FromResult(ConversationContextTokenMeasurement.NotAvailable);
}
