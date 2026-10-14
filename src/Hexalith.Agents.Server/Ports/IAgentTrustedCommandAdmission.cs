using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Host-composed AD-30 verifier; only its private closure owns signing/replay/security exception capabilities.</summary>
public interface IAgentTrustedCommandAdmission
{
    /// <summary>Authenticates exact operation/principal/payload/scope, durably registers issuer/nonce, then rechecks current authority.
    /// The returned envelope retains its LogicalCommandId as MessageId and adds authenticated trust metadata.
    /// A classified failure is durably spooled before it is thrown; failed spool admission throws SecurityAuditUnavailable.</summary>
    Task<CommandEnvelope> AdmitAsync(CommandEnvelope intent, CancellationToken cancellationToken);
}
