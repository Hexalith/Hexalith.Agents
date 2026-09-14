namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Default identity factory. Values are canonical ULIDs suitable for EventStore command and correlation identities.
/// </summary>
/// <remarks>
/// Canonical form is required, not incidental: receipt identity is compared by exact string, so a value that
/// normalizes differently would break the comparison the acceptance depends on. A ULID embeds a 48-bit
/// millisecond timestamp, which is echoed back to the caller in <c>AgentCommandAcceptance</c>; that timestamp is
/// the caller's own submission time and nothing else. No tenant, user, Party, Agent, or other request content is
/// derivable from the value — the remaining 80 bits are random.
/// </remarks>
public sealed class AgentCommandIdentityFactory : IAgentCommandIdentityFactory
{
    /// <inheritdoc />
    public string NewMessageId() => NUlid.Ulid.NewUlid().ToString();

    /// <inheritdoc />
    public string NewCorrelationId() => NUlid.Ulid.NewUlid().ToString();
}
