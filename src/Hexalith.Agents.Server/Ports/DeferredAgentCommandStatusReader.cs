namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IAgentCommandStatusReader"/> registered so the Server DI graph stays complete while the
/// live EventStore binding is deferred, mirroring <see cref="DeferredAgentCommandDispatcher"/>. Unlike the deferred
/// dispatcher it does not throw: a status read is supplementary evidence on an already-failing path, so the safe
/// deferred answer is "unknown", which leaves the outcome unverifiable instead of inventing a rejection.
/// </summary>
/// <remarks>
/// A future host may replace this placeholder after the production-like EventStore topology proves the status seam
/// is reachable. Until then every composition retains this implementation and therefore cannot invent a rejection.
/// </remarks>
public sealed class DeferredAgentCommandStatusReader : IAgentCommandStatusReader
{
    /// <inheritdoc />
    public Task<bool?> WasRejectedAsync(string messageId, CancellationToken ct) => Task.FromResult<bool?>(null);
}
