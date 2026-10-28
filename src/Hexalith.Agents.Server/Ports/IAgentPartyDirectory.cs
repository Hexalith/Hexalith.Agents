using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;

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
    /// <summary>
    /// Validates an existing Party as the Agent's own identity and maps its state to a fail-closed verdict (AC2). An
    /// Organization or Agent Party qualifies; a Human Party is <see cref="PartyLinkValidationStatus.Unauthorized"/>.
    /// </summary>
    /// <param name="tenantId">The Agent's tenant scope.</param>
    /// <param name="partyId">The stable Party id to validate.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe verdict plus the validated Party id (no PII).</returns>
    Task<AgentPartyValidationResult> ValidateExistingPartyAsync(string tenantId, string partyId, CancellationToken ct);

    /// <summary>
    /// Preserves the historical caller-validation signature as unavailable: no current actor binding is supplied.
    /// Current Human validation requires the actor-bearing overload.
    /// </summary>
    /// <param name="tenantId">The caller's tenant scope.</param>
    /// <param name="partyId">The caller's stable Party id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe verdict plus the validated Party id (no PII).</returns>
    Task<AgentPartyValidationResult> ValidateCallerPartyAsync(string tenantId, string partyId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null));
    }

    /// <summary>Validates the exact current Human binding using verified tenant-authorization evidence.</summary>
    /// <param name="tenantId">The exact caller tenant.</param>
    /// <param name="partyId">The exact caller Party.</param>
    /// <param name="actor">Transient server-verified subject, stable actor and current binding.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A safe verdict. Implementations without current-binding support remain closed.</returns>
    Task<AgentPartyValidationResult> ValidateCallerPartyAsync(string tenantId, string partyId, CurrentHumanActorEvidence actor, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null));
    }

    /// <summary>Provisions a new Agent Party (Organization, deterministic id) and returns its id with a verdict (AD-13).</summary>
    /// <param name="tenantId">The Agent's tenant scope.</param>
    /// <param name="request">The provisioning request (Agent id + non-personal label).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe verdict plus the new Party id (no PII).</returns>
    Task<AgentPartyValidationResult> ProvisionAgentPartyAsync(string tenantId, AgentPartyProvisioningRequest request, CancellationToken ct);
}
