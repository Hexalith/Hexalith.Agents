using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Simulates an existing tokenizer that provides timeline counts only.</summary>
internal sealed class TimelineOnlyConversationContextTokenMeasurer : IConversationContextTokenMeasurer
{
    /// <inheritdoc />
    public Task<ConversationContextTokenMeasurement> MeasureAsync(
        IReadOnlyList<ConversationContextMessage> messages, string providerId, string modelId, CancellationToken ct)
        => Task.FromResult(new ConversationContextTokenMeasurement(true, 1));
}
