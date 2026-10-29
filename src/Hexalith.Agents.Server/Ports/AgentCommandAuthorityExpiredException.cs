namespace Hexalith.Agents.Server.Ports;

/// <summary>Refuses protected reads, disclosure or command construction after current authority expiry or clock rollback.</summary>
public sealed class AgentCommandAuthorityExpiredException() : InvalidOperationException("CurrentAuthorityUnavailable")
{
}
