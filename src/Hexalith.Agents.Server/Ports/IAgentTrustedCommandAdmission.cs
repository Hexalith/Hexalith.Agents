namespace Hexalith.Agents.Server.Ports;

/// <summary>Host-composed AD-30 verifier; only its private closure owns signing/replay/security exception capabilities.</summary>
public interface IAgentTrustedCommandAdmission
{
    /// <summary>Authenticates exact operation/principal/payload/scope, durably registers issuer/nonce, then rechecks current authority.
    /// No EventStore target command exists at this boundary. The returned intent retains its logical identity as
    /// MessageId and adds authenticated trust metadata, including the separate delivery nonce.
    /// A classified failure is durably spooled before it is thrown; failed spool admission throws SecurityAuditUnavailable.</summary>
    Task<AgentCommandIntent> AdmitAsync(AgentCommandIntent intent, CancellationToken cancellationToken);
}
