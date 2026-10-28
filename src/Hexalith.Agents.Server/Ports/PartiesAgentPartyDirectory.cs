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
    public Task<AgentPartyValidationResult> ValidateExistingPartyAsync(string tenantId, string partyId, CancellationToken ct)
        => ValidateAsync(tenantId, partyId, null, ct);

    /// <inheritdoc />
    public Task<AgentPartyValidationResult> ValidateCallerPartyAsync(string tenantId, string partyId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null)
        {
            Dependency = _availability.Check("EXT-PARTIES-1"),
        });
    }

    /// <inheritdoc />
    public Task<AgentPartyValidationResult> ValidateCallerPartyAsync(string tenantId, string partyId, CurrentHumanActorEvidence actor, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return actor is null
            ? Task.FromResult(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null))
            : ValidateAsync(tenantId, partyId, actor, ct);
    }

    // An Agent identity is Organization (Branch B, verified by id) or Agent; a caller is Human. A well-formed Party of
    // the other kind is Unauthorized for that purpose, so a person cannot speak as hexa nor hexa act as a caller.
    private async Task<AgentPartyValidationResult> ValidateAsync(string tenantId, string partyId, CurrentHumanActorEvidence? actor, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
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
                if (actor is not null && (!IsCanonicalActorId(actor.ActorId)
                    || !actor.IsCurrentFor(tenantId, actor.Subject, partyId, actor.ObservedAt, started)))
                {
                    return new(PartyLinkValidationStatus.Unavailable, null);
                }

                PartyIdentityResult result = await _identityClient.ResolvePartyIdentityAsync(tenantId,
                    new ResolvePartyIdentity(tenantId, partyId, actor?.ActorId), ct).ConfigureAwait(false);
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

                if ((evidence.Classification == PartyIdentityClassification.Human) != (actor is not null))
                {
                    return new(PartyLinkValidationStatus.Unauthorized, null);
                }

                if (actor is not null && (!actor.IsCurrentFor(tenantId, actor.Subject, partyId, actor.ObservedAt, completed)
                    || !HasCurrentBinding(evidence, actor, completed)))
                {
                    return new(PartyLinkValidationStatus.Unavailable, null);
                }

                return evidence.IsActive && !evidence.IsRestricted && !evidence.IsErasingOrErased
                    ? new(PartyLinkValidationStatus.Valid, partyId)
                    {
                        CallerValidUntil = actor is null ? null : CallerDeadline(actor, evidence.HumanBinding!),
                    }
                    : new(PartyLinkValidationStatus.Disabled, null);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                ct.ThrowIfCancellationRequested();
                return new(PartyLinkValidationStatus.Unavailable, null);
            }
        }
#endif
        await Task.CompletedTask.ConfigureAwait(false);
        return new(PartyLinkValidationStatus.Unavailable, null);
    }

#if HEXALITH_PARTIES_SOURCE
    private static bool IsCanonicalActorId(string actorId)
        => actorId is { Length: 26 } && actorId[0] <= '7'
            && actorId.All(character => "0123456789ABCDEFGHJKMNPQRSTVWXYZ".Contains(character, StringComparison.Ordinal));

    private static DateTimeOffset CallerDeadline(CurrentHumanActorEvidence actor, HumanActorBindingEvidence binding)
    {
        DateTimeOffset deadline = actor.ValidUntil < binding.Custody.ExpiresAt ? actor.ValidUntil : binding.Custody.ExpiresAt;
        return binding.ValidUntil is { } until && until < deadline ? until : deadline;
    }

    private static bool HasCurrentBinding(PartyIdentityEvidence evidence, CurrentHumanActorEvidence actor, DateTimeOffset completed)
        => evidence.HumanBinding is { } binding
            && binding.TenantId == actor.TenantId && binding.PartyId == actor.PartyId && binding.ActorId == actor.ActorId
            && binding.BindingVersion == actor.BindingVersion && binding.ActorRevision > 0
            && binding.ValidFrom != default && binding.ValidFrom.Offset == TimeSpan.Zero && binding.ValidFrom <= evidence.ObservedAt
            && binding.ValidFrom <= actor.ObservedAt
            && binding.ValidUntil is { Offset: var offset } until && offset == TimeSpan.Zero
            && until > binding.ValidFrom && until > completed
            && !string.IsNullOrWhiteSpace(binding.SourceId) && !string.IsNullOrWhiteSpace(binding.ProvenanceId)
            && binding.Custody is { } custody && !string.IsNullOrWhiteSpace(custody.PolicyId)
            && custody.Purpose == "party-actor-history-v1" && custody.LifecycleRevision > 0 && !string.IsNullOrWhiteSpace(custody.EvidenceId)
            && custody.ExpiresAt.Offset == TimeSpan.Zero && custody.ExpiresAt >= until
            && custody.ExpiresAt > completed && custody.ExpiresAt < DateTimeOffset.MaxValue
            && custody.SourceExpiryEnforced && custody.RestoreSafe && custody.DerivedCopiesCovered;
#endif

    /// <inheritdoc />
    public Task<AgentPartyValidationResult> ProvisionAgentPartyAsync(string tenantId, AgentPartyProvisioningRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();
        // The legacy label/id request is not the owner's immutable versioned provisioning contract. It cannot
        // mint a replacement identity. Platform provisioning must retain the original accepted Parties intent.
        return Task.FromResult(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null)
        {
            Dependency = _availability.Check("EXT-PARTIES-1"),
        });
    }
}
