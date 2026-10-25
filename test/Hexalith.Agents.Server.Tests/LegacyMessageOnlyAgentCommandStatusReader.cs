using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Represents an existing host reader that implements only the original message-only contract.</summary>
internal sealed class LegacyMessageOnlyAgentCommandStatusReader : IAgentCommandStatusReader
{
    /// <summary>Gets the number of legacy evidence reads.</summary>
    internal int MessageReads { get; private set; }

    /// <inheritdoc />
    public Task<bool?> WasRejectedAsync(string messageId, CancellationToken ct)
    {
        MessageReads++;
        return Task.FromResult<bool?>(true);
    }
}
