using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal outcome of the Provider/model selection orchestration (Story 1.5). <see cref="Authorized"/> is
/// <see langword="false"/> when the actor was not an Agents admin (the orchestration fails closed before any catalog
/// read or dispatch). When authorized, the computed <see cref="Verdict"/> is always dispatched to the aggregate —
/// even a non-<see cref="ProviderSelectionValidationStatus.Valid"/> verdict — so the aggregate records an auditable
/// rejection. Carries only the safe verdict + captured capability version (AC1; AD-9).
/// </summary>
/// <param name="Authorized">Whether the actor passed the Agents-admin gate.</param>
/// <param name="Dispatched">Whether a command was dispatched to the aggregate.</param>
/// <param name="Verdict">The computed provider-readiness verdict fed to the aggregate.</param>
/// <param name="CapabilityVersion">The provider capability version captured from the catalog read (0 when no entry was read).</param>
public sealed record AgentProviderSelectionOutcome(
    bool Authorized,
    bool Dispatched,
    ProviderSelectionValidationStatus Verdict,
    int CapabilityVersion)
{
    /// <summary>Creates the fail-closed outcome for an unauthorized actor — nothing was read or dispatched.</summary>
    /// <returns>The denied outcome.</returns>
    public static AgentProviderSelectionOutcome Denied()
        => new(Authorized: false, Dispatched: false, ProviderSelectionValidationStatus.Unknown, CapabilityVersion: 0);

    /// <summary>Creates the outcome for an authorized request whose command was dispatched with the computed verdict.</summary>
    /// <param name="verdict">The computed verdict fed to the aggregate.</param>
    /// <param name="capabilityVersion">The captured provider capability version.</param>
    /// <returns>The dispatched outcome.</returns>
    public static AgentProviderSelectionOutcome FromDispatch(ProviderSelectionValidationStatus verdict, int capabilityVersion)
        => new(Authorized: true, Dispatched: true, verdict, capabilityVersion);
}
