using Hexalith.EventStore.Contracts.Commands;

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

    /// <summary>Reads rejection evidence for the exact authorized command and identity-verified receipt.</summary>
    /// <param name="tenantId">The tenant from the trusted command scope.</param>
    /// <param name="domain">The domain from the trusted command scope.</param>
    /// <param name="messageId">The verified receipt message identity.</param>
    /// <param name="correlationId">The verified receipt correlation identity.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <remarks>Legacy message-only registrations remain valid, but cannot establish scoped evidence. Hosts must
    /// explicitly implement this overload and <see cref="ReadAsync"/> to supply scoped rejection and status DTO evidence.</remarks>
    /// <returns>Whether the exact command was rejected, or null when it cannot be verified.</returns>
    Task<bool?> WasRejectedAsync(string tenantId, string domain, string messageId, string correlationId, CancellationToken ct)
        => Task.FromResult<bool?>(null);

    /// <summary>Reads authenticated status for an authorized tenant and exact domain without a user-route fallback.</summary>
    /// <param name="tenantId">The authorized tenant.</param>
    /// <param name="domain">The exact command domain.</param>
    /// <param name="messageId">The requested command identity.</param>
    /// <param name="correlationId">The original receipt correlation when known.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching status, or null when evidence is unavailable.</returns>
    Task<CommandStatusQueryResponse?> ReadAsync(string tenantId, string domain, string messageId, string? correlationId, CancellationToken ct)
        => Task.FromResult<CommandStatusQueryResponse?>(null);
}
