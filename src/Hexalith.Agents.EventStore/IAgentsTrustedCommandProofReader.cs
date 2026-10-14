using System.Security.Claims;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.EventStore;

/// <summary>Private host bridge for an already authenticated and durably replay-admitted exact request.</summary>
public interface IAgentsTrustedCommandProofReader
{
    /// <summary>Checks the verifier-owned receipt for this exact authenticated transport/request and reserved value.
    /// Raw flags, a client proof string or a prior request receipt confer no authority. Implementations cannot
    /// register replay here: admission must finish before the target SubmitCommandRequest is constructed.</summary>
    bool HasAdmittedProof(ClaimsPrincipal principal, SubmitCommandRequest command, string key, string value);
}
