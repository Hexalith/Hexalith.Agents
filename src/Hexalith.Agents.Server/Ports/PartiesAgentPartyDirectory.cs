using Hexalith.Agents.Contracts.Agent;
using Hexalith.Parties.Client.Abstractions;
#if HEXALITH_PARTIES_SOURCE
using Hexalith.Parties.Contracts.Models;
using Hexalith.Parties.Contracts.Queries;
using Hexalith.Parties.Contracts.ValueObjects;
#endif

namespace Hexalith.Agents.Server.Ports;

/// <summary>Consumes explicit tenant/id identity evidence only after the complete Parties record is Available.</summary>
public sealed class PartiesAgentPartyDirectory : IAgentPartyDirectory
{
    private readonly IAgentDependencyAvailability _availability;
    private readonly TimeProvider _clock;
#if HEXALITH_PARTIES_SOURCE
    private readonly IPartiesIdentityClient? _identityClient;

    /// <summary>Composes the accepted explicit-scope current identity client.</summary>
    public PartiesAgentPartyDirectory(IPartiesIdentityClient identityClient, IAgentDependencyAvailability availability, TimeProvider clock)
    {
        _identityClient = identityClient;
        _availability = availability;
        _clock = clock;
    }
#endif
    /// <summary>Legacy construction remains closed; profile queries and general Party creation confer no identity authority.</summary>
    public PartiesAgentPartyDirectory(IPartiesQueryClient? queryClient = null, IPartiesCommandClient? commandClient = null)
    {
        _availability = new CommittedAgentDependencyAvailability();
        _clock = TimeProvider.System;
    }

    /// <summary>Preserves the historical deterministic identity; it never authorizes replacement/provisioning.</summary>
    public static string DeriveProvisionedPartyId(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return $"agent-{agentId}";
    }

    /// <inheritdoc />
    public async Task<AgentPartyValidationResult> ValidateExistingPartyAsync(string tenantId, string partyId, CancellationToken ct)
    {
        if (_availability.Check("EXT-PARTIES-1") is { } blocker)
        {
            return new(PartyLinkValidationStatus.Unavailable, null) { Dependency = blocker };
        }

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(partyId))
        {
            return new(PartyLinkValidationStatus.Unauthorized, null);
        }
#if HEXALITH_PARTIES_SOURCE
        if (_identityClient is not null)
        {
            try
            {
                DateTimeOffset started = _clock.GetUtcNow();
                PartyIdentityResult result = await _identityClient.ResolvePartyIdentityAsync(tenantId, new ResolvePartyIdentity(tenantId, partyId), ct).ConfigureAwait(false);
                DateTimeOffset completed = _clock.GetUtcNow();
                ct.ThrowIfCancellationRequested();
                if (result.Outcome != PartyIdentityOutcome.Resolved || result.Evidence is not { } evidence)
                {
                    return new(result.Outcome == PartyIdentityOutcome.Ineligible ? PartyLinkValidationStatus.Disabled : PartyLinkValidationStatus.Unavailable, null);
                }

                if (evidence.TenantId != tenantId || evidence.PartyId != partyId || evidence.ContractVersion != 1
                    || evidence.SourcePosition <= 0 || string.IsNullOrWhiteSpace(evidence.ObservationId)
                    || completed < started || evidence.ObservedAt < started || evidence.ObservedAt > completed
                    || evidence.ObservedAt.Offset != TimeSpan.Zero || evidence.Classification is not
                        (PartyIdentityClassification.Human or PartyIdentityClassification.Organization or PartyIdentityClassification.Agent))
                {
                    return new(PartyLinkValidationStatus.Unavailable, null);
                }

                return evidence.IsActive && !evidence.IsRestricted && !evidence.IsErasingOrErased
                    ? new(PartyLinkValidationStatus.Valid, partyId) : new(PartyLinkValidationStatus.Disabled, null);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return new(PartyLinkValidationStatus.Unavailable, null);
            }
        }
#endif
        await Task.CompletedTask.ConfigureAwait(false);
        return new(PartyLinkValidationStatus.Unavailable, null);
    }

    /// <inheritdoc />
    public Task<AgentPartyValidationResult> ProvisionAgentPartyAsync(string tenantId, AgentPartyProvisioningRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        // The legacy label/id request is not the owner's immutable versioned provisioning contract. It cannot
        // mint a replacement identity. Platform provisioning must retain the original accepted Parties intent.
        return Task.FromResult(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null)
        {
            Dependency = _availability.Check("EXT-PARTIES-1"),
        });
    }
}
