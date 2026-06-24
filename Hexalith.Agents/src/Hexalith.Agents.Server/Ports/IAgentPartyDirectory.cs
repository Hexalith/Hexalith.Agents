using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the application orchestration uses to validate an existing Party identity, or provision a
/// new Agent Party, through the Parties module (AD-3, AD-7, AD-12). The implementation calls the public Parties
/// client and maps the dependency state to a safe <see cref="AgentPartyValidationResult"/>; it MUST never surface
/// Party PII (display names, contacts, personal-data objects) back across this boundary (AC1; AD-14).
/// </summary>
/// <remarks>
/// Keeping this a port (rather than calling the Parties client inline) preserves the AD-3 round-trip: the pure
/// aggregate receives the resulting verdict through a trusted command extension, never by calling Parties itself.
/// </remarks>
public interface IAgentPartyDirectory
{
    /// <summary>Validates an existing Party identity for linking and maps its state to a fail-closed verdict (AC2).</summary>
    /// <param name="tenantId">The Agent's tenant scope.</param>
    /// <param name="partyId">The stable Party id to validate.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe verdict plus the validated Party id (no PII).</returns>
    Task<AgentPartyValidationResult> ValidateExistingPartyAsync(string tenantId, string partyId, CancellationToken ct);

    /// <summary>Provisions a new Agent Party (Organization, deterministic id) and returns its id with a verdict (AD-13).</summary>
    /// <param name="tenantId">The Agent's tenant scope.</param>
    /// <param name="request">The provisioning request (Agent id + non-personal label).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe verdict plus the new Party id (no PII).</returns>
    Task<AgentPartyValidationResult> ProvisionAgentPartyAsync(string tenantId, AgentPartyProvisioningRequest request, CancellationToken ct);
}
