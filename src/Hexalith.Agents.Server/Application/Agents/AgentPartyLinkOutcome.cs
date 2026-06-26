using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal outcome of the Party-identity link/replace orchestration. <see cref="Authorized"/> is
/// <see langword="false"/> when the actor was not an Agents admin (the orchestration fails closed before any
/// Parties call or dispatch). When authorized, the computed <see cref="Verdict"/> is always dispatched to the
/// aggregate — even a non-<see cref="PartyLinkValidationStatus.Valid"/> verdict — so the aggregate records an
/// auditable rejection. Carries no Party PII (AC1).
/// </summary>
/// <param name="Authorized">Whether the actor passed the Agents-admin gate.</param>
/// <param name="Dispatched">Whether a command was dispatched to the aggregate.</param>
/// <param name="Verdict">The computed Parties-validation verdict fed to the aggregate.</param>
/// <param name="PartyId">The stable Party id carried on the dispatched command (a reference, not PII), if any.</param>
public sealed record AgentPartyLinkOutcome(
    bool Authorized,
    bool Dispatched,
    PartyLinkValidationStatus Verdict,
    string? PartyId)
{
    /// <summary>Creates the fail-closed outcome for an unauthorized actor — nothing was validated or dispatched.</summary>
    /// <returns>The denied outcome.</returns>
    public static AgentPartyLinkOutcome Denied()
        => new(Authorized: false, Dispatched: false, PartyLinkValidationStatus.Unknown, PartyId: null);

    /// <summary>Creates the outcome for an authorized request whose command was dispatched with the computed verdict.</summary>
    /// <param name="verdict">The computed verdict fed to the aggregate.</param>
    /// <param name="partyId">The Party id carried on the dispatched command.</param>
    /// <returns>The dispatched outcome.</returns>
    public static AgentPartyLinkOutcome FromDispatch(PartyLinkValidationStatus verdict, string partyId)
        => new(Authorized: true, Dispatched: true, verdict, partyId);
}
