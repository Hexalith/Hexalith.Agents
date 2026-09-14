namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Default identity factory. Values are canonical ULIDs suitable for EventStore command and correlation identities.
/// </summary>
public sealed class AgentCommandIdentityFactory : IAgentCommandIdentityFactory
{
    /// <inheritdoc />
    public string NewMessageId() => NUlid.Ulid.NewUlid().ToString();

    /// <inheritdoc />
    public string NewCorrelationId() => NUlid.Ulid.NewUlid().ToString();
}
