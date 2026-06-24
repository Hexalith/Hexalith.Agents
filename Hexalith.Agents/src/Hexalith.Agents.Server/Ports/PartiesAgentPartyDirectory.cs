using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;

using Hexalith.Parties.Client;
using Hexalith.Parties.Client.Abstractions;
using Hexalith.Parties.Contracts.Commands;
using Hexalith.Parties.Contracts.Models;
using Hexalith.Parties.Contracts.ValueObjects;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// <see cref="IAgentPartyDirectory"/> adapter over the public Parties client (AD-3, AD-7, AD-12). It maps observed
/// Party state to a fail-closed <see cref="PartyLinkValidationStatus"/> and provisions a new Agent Party as an
/// <c>Organization</c> with a deterministic id. It returns ONLY the verdict + the stable Party id — the Parties
/// <c>PartyDetail</c> PII fields (<c>DisplayName</c>, <c>SortName</c>, <c>PersonDetails</c>, <c>ContactChannels</c>,
/// …) never cross this boundary back into Agents (AC1; AD-14).
/// </summary>
public sealed class PartiesAgentPartyDirectory : IAgentPartyDirectory
{
    private readonly IPartiesQueryClient _queryClient;
    private readonly IPartiesCommandClient _commandClient;

    /// <summary>Initializes a new instance of the <see cref="PartiesAgentPartyDirectory"/> class.</summary>
    /// <param name="queryClient">The public Parties query client.</param>
    /// <param name="commandClient">The public Parties command client.</param>
    public PartiesAgentPartyDirectory(IPartiesQueryClient queryClient, IPartiesCommandClient commandClient)
    {
        ArgumentNullException.ThrowIfNull(queryClient);
        ArgumentNullException.ThrowIfNull(commandClient);
        _queryClient = queryClient;
        _commandClient = commandClient;
    }

    /// <summary>
    /// Derives the deterministic Party id for an Agent's provisioned Organization Party, so a retried provision is
    /// idempotent and never creates duplicate Parties (AD-13).
    /// </summary>
    /// <param name="agentId">The Agent id.</param>
    /// <returns>The deterministic Party id.</returns>
    public static string DeriveProvisionedPartyId(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return $"agent-{agentId}";
    }

    /// <inheritdoc />
    public async Task<AgentPartyValidationResult> ValidateExistingPartyAsync(string tenantId, string partyId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partyId);

        // tenantId is part of the port contract for forward tenant scoping; per-call scoping is deferred with the
        // live runtime binding (Story 1.2/1.3). Today tenant isolation is enforced ambiently by the Parties client
        // (the "Parties:{ Tenant }" config / caller auth context), and a cross-tenant read surfaces as 401/403 →
        // Unauthorized below — never as a silently fresh Valid (AD-12 fail closed; FR-19 tenant isolation).
        try
        {
            PartyDetail party = await _queryClient.GetPartyAsync(partyId, ct).ConfigureAwait(false);
            PartyLinkValidationStatus status = MapExistingVerdict(party);
            return new AgentPartyValidationResult(
                status,
                status == PartyLinkValidationStatus.Valid ? party.Id : null);
        }
        catch (PartiesClientException ex) when (ex.Status == 404)
        {
            // No Party matched the supplied reference.
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Missing, null);
        }
        catch (PartiesClientException ex) when (ex.Status is 401 or 403)
        {
            // Wrong-tenant scope / caller not authorized to read the Party.
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Unauthorized, null);
        }
        catch (PartiesClientException)
        {
            // Any other gateway error is dependency uncertainty — fail closed (AD-12).
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Transport/unexpected adapter failure — fail closed (AD-12).
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null);
        }
    }

    /// <inheritdoc />
    public async Task<AgentPartyValidationResult> ProvisionAgentPartyAsync(string tenantId, AgentPartyProvisioningRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        string partyId = DeriveProvisionedPartyId(request.AgentId);

        var create = new CreateParty
        {
            PartyId = partyId,
            Type = PartyType.Organization, // no AiAgent/Bot/System type — Organization avoids person PII
            OrganizationDetails = new OrganizationDetails { LegalName = request.OrganizationLabel },
        };

        try
        {
            // The new Party detail (and any PII it carries) stays in Parties — only the deterministic id + verdict
            // return to the Agents side (AC1; AD-7).
            _ = await _commandClient.CreatePartyWithResultAsync(create, ct).ConfigureAwait(false);
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, partyId);
        }
        catch (PartiesClientException ex) when (ex.Status is 401 or 403)
        {
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Unauthorized, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null);
        }
    }

    // Maps a returned PartyDetail to a fail-closed verdict (Dev Notes › Parties State → Verdict Mapping). A
    // deactivated/erased/restricted Party is Disabled; a non-Current projection is Unavailable (never trusted as
    // fresh — AD-12); otherwise Valid.
    private static PartyLinkValidationStatus MapExistingVerdict(PartyDetail party)
    {
        if (!party.IsActive || party.IsErased || party.IsRestricted)
        {
            return PartyLinkValidationStatus.Disabled;
        }

        if (party.Freshness is { Status: not ProjectionFreshnessStatus.Current })
        {
            return PartyLinkValidationStatus.Unavailable;
        }

        return PartyLinkValidationStatus.Valid;
    }
}
