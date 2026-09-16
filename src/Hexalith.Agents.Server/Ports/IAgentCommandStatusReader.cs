namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal seam that reads the recorded terminal outcome of a dispatched command (Story 5.2 AC1). A
/// submission receipt carries a result payload only for a completed success or no-op, so a domain rejection and an
/// unverifiable submission are indistinguishable at the receipt alone. This seam supplies the missing evidence so a
/// rejection is reported as the terminal failure it is rather than as a retryable unverifiable outcome.
/// </summary>
public interface IAgentCommandStatusReader
{
    /// <summary>Reads whether the dispatched command was domain-rejected.</summary>
    /// <param name="messageId">The canonical command message identity from the receipt.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> when the command reached the rejected terminal state; <see langword="false"/> when it
    /// did not; <see langword="null"/> when no status is recorded yet or the status could not be read, which stays
    /// unverifiable rather than claiming either outcome.
    /// </returns>
    Task<bool?> WasRejectedAsync(string messageId, CancellationToken ct);
}
