namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Mints the message and correlation identities an Agent administration command travels under when the caller
/// supplied none. Kept behind a seam so tests can assert on exact accepted identities instead of chasing a
/// non-deterministic value.
/// </summary>
public interface IAgentCommandIdentityFactory
{
    /// <summary>Creates a new command message identity (also the gateway idempotency key).</summary>
    /// <returns>The message identity.</returns>
    string NewMessageId();

    /// <summary>Creates a new correlation identity for tracing.</summary>
    /// <returns>The correlation identity.</returns>
    string NewCorrelationId();
}
