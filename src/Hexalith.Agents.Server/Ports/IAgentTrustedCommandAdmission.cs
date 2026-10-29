namespace Hexalith.Agents.Server.Ports;

/// <summary>Host-composed AD-30 verifier; only its private closure owns signing/replay/security exception capabilities.</summary>
public interface IAgentTrustedCommandAdmission
{
    /// <summary>Authenticates exact operation/principal/payload/scope, durably registers issuer/nonce, then rechecks current authority.
    /// No EventStore target command exists at this boundary. The returned intent retains its logical identity as
    /// MessageId and adds authenticated trust metadata, including the separate delivery nonce. The dispatcher refuses a
    /// result that changes any original field, byte or metadata value, or adds an entry outside the <c>trust:</c> namespace.
    /// Preserve the exact transient <see cref="AgentCommandIntent.Authority"/> reference when copying an intent;
    /// copy that reference explicitly when reconstructing an intent to add trust metadata.
    /// A classified failure is durably spooled before it is thrown; failed spool admission throws SecurityAuditUnavailable.</summary>
    Task<AgentCommandIntent> AdmitAsync(AgentCommandIntent intent, CancellationToken cancellationToken);
}
