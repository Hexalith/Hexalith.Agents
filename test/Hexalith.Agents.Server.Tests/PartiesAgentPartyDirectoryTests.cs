using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Ports;
using Hexalith.Parties.Client.Abstractions;
using NSubstitute;
using Shouldly;
#if HEXALITH_PARTIES_SOURCE
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Parties.Client;
using Hexalith.Parties.Contracts;
using Hexalith.Parties.Contracts.Models;
using Hexalith.Parties.Contracts.Queries;
using Hexalith.Parties.Contracts.ValueObjects;
#endif

namespace Hexalith.Agents.Server.Tests;

/// <summary>Local explicit-scope identity adapter tests; substituted clients provide no live availability evidence.</summary>
public sealed class PartiesAgentPartyDirectoryTests
{
    /// <summary>An implementation of only the legacy caller method cannot authorize the new actor-bearing seam.</summary>
    [Fact]
    public async Task ActorBearingInterfaceDefaultNeverDelegatesToLegacyCallerValidation()
    {
        var legacy = new LegacyCallerOnlyPartyDirectory();
        IAgentPartyDirectory directory = legacy;

        (await directory.ValidateCallerPartyAsync("tenant-a", "party-a", Story54PartyIdentityFixture.Actor(new Story54TimeProvider()), CancellationToken.None))
            .ShouldBe(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null));

        legacy.LegacyCalled.ShouldBeFalse();
    }

    /// <summary>Verifies that legacy profile query and provisioning never execute while committed.</summary>
    [Fact]
    public async Task LegacyProfileQueryAndProvisioningNeverExecuteWhileCommitted()
    {
        var queries = Substitute.For<IPartiesQueryClient>();
        var commands = Substitute.For<IPartiesCommandClient>();
        var directory = new PartiesAgentPartyDirectory(queries, commands);
        AgentPartyValidationResult read = await directory.ValidateExistingPartyAsync("tenant-a", "party-a", CancellationToken.None);
        read.Status.ShouldBe(PartyLinkValidationStatus.Unavailable);
        read.Dependency.ShouldBe(new DependencyNotAvailable("EXT-PARTIES-1"));
        (await directory.ValidateCallerPartyAsync("tenant-a", "party-a", CancellationToken.None)).Status
            .ShouldBe(PartyLinkValidationStatus.Unavailable);
        (await directory.ValidateCallerPartyAsync("tenant-a", "party-a", Story54PartyIdentityFixture.Actor(new Story54TimeProvider()), CancellationToken.None)).Status
            .ShouldBe(PartyLinkValidationStatus.Unavailable);
        AgentPartyValidationResult provision = await directory.ProvisionAgentPartyAsync("tenant-a", new("hexa", "label"), CancellationToken.None);
        provision.Status.ShouldBe(PartyLinkValidationStatus.Unavailable);
        queries.ReceivedCalls().ShouldBeEmpty();
        commands.ReceivedCalls().ShouldBeEmpty();
    }
#if HEXALITH_PARTIES_SOURCE
    /// <summary>The historical caller signature cannot grant current Human authority or perform an owner read.</summary>
    [Fact]
    public async Task LegacyCallerValidationRemainsClosedWithoutVerifiedActorEvidence()
    {
        var client = Substitute.For<IPartiesIdentityClient>();
        var clock = new Story54TimeProvider();
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new PartyIdentityResult(PartyIdentityOutcome.Resolved, new PartyIdentityEvidence(1, "tenant-a", "party-a",
                PartyIdentityClassification.Human, true, false, false, 1, clock.Now, "observation", null)));
        var directory = new PartiesAgentPartyDirectory(client, Substitute.For<IAgentDependencyAvailability>(), clock);

        (await directory.ValidateCallerPartyAsync("tenant-a", "party-a", CancellationToken.None)).Status
            .ShouldBe(PartyLinkValidationStatus.Unavailable);
        client.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Verifies that only fresh exact tenant and immutable id evidence is valid.</summary>
    [Theory]
    [InlineData("tenant-a", "party-a", 1, true)]
    [InlineData("tenant-b", "party-a", 1, false)]
    [InlineData("tenant-a", "party-b", 1, false)]
    [InlineData("tenant-a", "party-a", 0, false)]
    public async Task OnlyFreshExactTenantAndImmutableIdEvidenceIsValid(string returnedTenant, string returnedParty, long sourcePosition, bool valid)
    {
        var client = Substitute.For<IPartiesIdentityClient>();
        var clock = new Story54TimeProvider();
        var availability = Substitute.For<IAgentDependencyAvailability>();
        var evidence = new PartyIdentityEvidence(1, returnedTenant, returnedParty, PartyIdentityClassification.Organization,
            true, false, false, sourcePosition, clock.GetUtcNow(), "observation", null);
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new PartyIdentityResult(PartyIdentityOutcome.Resolved, evidence));
        var directory = new PartiesAgentPartyDirectory(client, availability, clock);
        AgentPartyValidationResult result = await directory.ValidateExistingPartyAsync("tenant-a", "party-a", CancellationToken.None);
        result.Status.ShouldBe(valid ? PartyLinkValidationStatus.Valid : PartyLinkValidationStatus.Unavailable);
        result.PartyId.ShouldBe(valid ? "party-a" : null);
        await client.Received(1).ResolvePartyIdentityAsync("tenant-a", new ResolvePartyIdentity("tenant-a", "party-a"), Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that the Agent identity is an Organization or Agent Party and the caller is a Human Party.</summary>
    [Theory]
    [InlineData(PartyIdentityClassification.Organization, false, PartyLinkValidationStatus.Valid)]
    [InlineData(PartyIdentityClassification.Agent, false, PartyLinkValidationStatus.Valid)]
    [InlineData(PartyIdentityClassification.Human, false, PartyLinkValidationStatus.Unauthorized)]
    [InlineData(PartyIdentityClassification.Human, true, PartyLinkValidationStatus.Valid)]
    [InlineData(PartyIdentityClassification.Organization, true, PartyLinkValidationStatus.Unauthorized)]
    [InlineData(PartyIdentityClassification.Agent, true, PartyLinkValidationStatus.Unauthorized)]
    public async Task ClassificationMustMatchThePurpose(PartyIdentityClassification classification, bool caller, PartyLinkValidationStatus expected)
    {
        var client = Substitute.For<IPartiesIdentityClient>();
        var clock = new Story54TimeProvider();
        var evidence = new PartyIdentityEvidence(1, "tenant-a", "party-a", classification,
            true, false, false, 1, clock.GetUtcNow(), "observation",
            classification == PartyIdentityClassification.Human ? Story54PartyIdentityFixture.Binding(clock) : null);
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new PartyIdentityResult(PartyIdentityOutcome.Resolved, evidence));
        var directory = new PartiesAgentPartyDirectory(client, Substitute.For<IAgentDependencyAvailability>(), clock);
        AgentPartyValidationResult result = caller
            ? await directory.ValidateCallerPartyAsync("tenant-a", "party-a", Story54PartyIdentityFixture.Actor(clock), CancellationToken.None)
            : await directory.ValidateExistingPartyAsync("tenant-a", "party-a", CancellationToken.None);
        result.Status.ShouldBe(expected);
        result.PartyId.ShouldBe(expected == PartyLinkValidationStatus.Valid ? "party-a" : null);
    }

    /// <summary>Verifies that a committed record blocks before the identity client.</summary>
    [Fact]
    public async Task ACommittedRecordBlocksBeforeTheIdentityClient()
    {
        var client = Substitute.For<IPartiesIdentityClient>();
        var directory = new PartiesAgentPartyDirectory(client, new CommittedAgentDependencyAvailability(), new Story54TimeProvider());
        (await directory.ValidateExistingPartyAsync("tenant-a", "party-a", CancellationToken.None)).Dependency
            .ShouldBe(new DependencyNotAvailable("EXT-PARTIES-1"));
        (await directory.ValidateCallerPartyAsync("tenant-a", "party-a", CancellationToken.None)).Dependency
            .ShouldBe(new DependencyNotAvailable("EXT-PARTIES-1"));
        (await directory.ValidateCallerPartyAsync("tenant-a", "party-a", Story54PartyIdentityFixture.Actor(new Story54TimeProvider()), CancellationToken.None)).Dependency
            .ShouldBe(new DependencyNotAvailable("EXT-PARTIES-1"));
        client.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Tenant authorization's earlier observation remains valid and the query names its stable actor.</summary>
    [Fact]
    public async Task CallerQueryUsesTheVerifiedStableActorAndAcceptsAnEarlierActorObservation()
    {
        var clock = new Story54TimeProvider();
        CurrentHumanActorEvidence actor = Story54PartyIdentityFixture.Actor(clock);
        clock.Now = clock.Now.AddSeconds(1);
        var client = Substitute.For<IPartiesIdentityClient>();
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new PartyIdentityResult(PartyIdentityOutcome.Resolved, Story54PartyIdentityFixture.Identity(clock)));

        var result = await new PartiesAgentPartyDirectory(client, Substitute.For<IAgentDependencyAvailability>(), clock)
            .ValidateCallerPartyAsync("tenant-a", "party-a", actor, CancellationToken.None);

        result.ShouldBe(new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "party-a") { CallerValidUntil = actor.ValidUntil });
        actor.Subject.ShouldNotBe(actor.ActorId);
        await client.Received(1).ResolvePartyIdentityAsync("tenant-a", new ResolvePartyIdentity("tenant-a", "party-a", actor.ActorId), Arg.Any<CancellationToken>());
    }

    /// <summary>Direct callers cannot reach the owner with incomplete, foreign or noncurrent actor evidence.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("tenant")]
    [InlineData("subject")]
    [InlineData("party")]
    [InlineData("actor")]
    [InlineData("version")]
    [InlineData("position")]
    [InlineData("observation")]
    [InlineData("future")]
    [InlineData("expired")]
    [InlineData("observation-offset")]
    [InlineData("expiry-offset")]
    public async Task MalformedCallerActorStopsBeforeTheOwnerClient(string corruption)
    {
        var clock = new Story54TimeProvider();
        CurrentHumanActorEvidence actor = Story54PartyIdentityFixture.Actor(clock);
        CurrentHumanActorEvidence? supplied = corruption switch
        {
            "missing" => null,
            "tenant" => actor with { TenantId = "foreign-tenant" },
            "subject" => actor with { Subject = " " },
            "party" => actor with { PartyId = "foreign-party" },
            "actor" => actor with { ActorId = " " },
            "version" => actor with { BindingVersion = 0 },
            "position" => actor with { SourcePosition = 0 },
            "observation" => actor with { ObservedAt = default },
            "future" => actor with { ObservedAt = clock.Now.AddTicks(1) },
            "expired" => actor with { ValidUntil = clock.Now },
            "observation-offset" => actor with { ObservedAt = clock.Now.ToOffset(TimeSpan.FromHours(1)) },
            "expiry-offset" => actor with { ValidUntil = actor.ValidUntil.ToOffset(TimeSpan.FromHours(1)) },
            _ => throw new ArgumentOutOfRangeException(nameof(corruption)),
        };
        var client = Substitute.For<IPartiesIdentityClient>();

        AgentPartyValidationResult result = await new PartiesAgentPartyDirectory(client,
            Substitute.For<IAgentDependencyAvailability>(), clock)
            .ValidateCallerPartyAsync("tenant-a", "party-a", supplied!, CancellationToken.None);

        result.ShouldBe(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null));
        client.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Only canonical stable actor ULIDs may reach the public owner client.</summary>
    [Theory]
    [InlineData("stable-actor")]
    [InlineData("01arz3ndektsv4rrffq69g5fav")]
    [InlineData("81ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAI")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FA")]
    [InlineData(" 01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    public async Task NoncanonicalActorIdsStopBeforeTheOwnerClient(string actorId)
    {
        var clock = new Story54TimeProvider();
        CurrentHumanActorEvidence actor = Story54PartyIdentityFixture.Actor(clock) with { ActorId = actorId };
        PartyIdentityEvidence identity = Story54PartyIdentityFixture.Identity(clock);
        var client = Substitute.For<IPartiesIdentityClient>();
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new PartyIdentityResult(PartyIdentityOutcome.Resolved,
                identity with { HumanBinding = identity.HumanBinding! with { ActorId = actorId } }));

        AgentPartyValidationResult result = await new PartiesAgentPartyDirectory(client,
            Substitute.For<IAgentDependencyAvailability>(), clock)
            .ValidateCallerPartyAsync("tenant-a", "party-a", actor, CancellationToken.None);

        result.ShouldBe(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null));
        client.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A later owner observation cannot make a binding current at an earlier verified actor observation.</summary>
    [Fact]
    public async Task BindingEffectiveAfterTheVerifiedActorObservationIsRejected()
    {
        var clock = new Story54TimeProvider();
        CurrentHumanActorEvidence actor = Story54PartyIdentityFixture.Actor(clock);
        clock.Now = clock.Now.AddSeconds(1);
        DateTimeOffset effectiveAt = actor.ObservedAt.AddTicks(1);
        HumanActorBindingEvidence binding = Story54PartyIdentityFixture.Binding(clock) with { ValidFrom = effectiveAt };
        binding = binding with { Custody = binding.Custody with { ExpiresAt = effectiveAt.AddDays(365) } };
        var client = Substitute.For<IPartiesIdentityClient>();
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new PartyIdentityResult(PartyIdentityOutcome.Resolved,
                Story54PartyIdentityFixture.Identity(clock) with { HumanBinding = binding }));

        AgentPartyValidationResult result = await new PartiesAgentPartyDirectory(client,
            Substitute.For<IAgentDependencyAvailability>(), clock)
            .ValidateCallerPartyAsync("tenant-a", "party-a", actor, CancellationToken.None);

        result.ShouldBe(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null));
        await client.Received(1).ResolvePartyIdentityAsync("tenant-a",
            new ResolvePartyIdentity("tenant-a", "party-a", actor.ActorId), Arg.Any<CancellationToken>());
    }

    /// <summary>Inactive, restricted or erasing Humans never release identity or caller deadline evidence.</summary>
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task IneligibleHumanResultsReleaseNeitherPartyNorCallerDeadline(bool active, bool restricted, bool erasing)
    {
        var clock = new Story54TimeProvider();
        PartyIdentityEvidence identity = Story54PartyIdentityFixture.Identity(clock) with
        {
            IsActive = active,
            IsRestricted = restricted,
            IsErasingOrErased = erasing,
        };
        var client = Substitute.For<IPartiesIdentityClient>();
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new PartyIdentityResult(PartyIdentityOutcome.Resolved, identity));

        AgentPartyValidationResult result = await new PartiesAgentPartyDirectory(client,
            Substitute.For<IAgentDependencyAvailability>(), clock)
            .ValidateCallerPartyAsync("tenant-a", "party-a", Story54PartyIdentityFixture.Actor(clock), CancellationToken.None);

        result.Status.ShouldBe(PartyLinkValidationStatus.Disabled);
        result.PartyId.ShouldBeNull();
        result.CallerValidUntil.ShouldBeNull();
    }

    /// <summary>Serialized local replies pass through the real public client's query and response validation.</summary>
    [Theory]
    [InlineData("current", true)]
    [InlineData("missing-end", false)]
    [InlineData("custody-too-short", false)]
    [InlineData("actor", false)]
    [InlineData("tenant", false)]
    [InlineData("failed-envelope", false)]
    public async Task SerializedHumanIdentityRoundTripUsesThePublicClient(string scenario, bool valid)
    {
        var clock = new Story54TimeProvider();
        CurrentHumanActorEvidence actor = Story54PartyIdentityFixture.Actor(clock);
        PartyIdentityEvidence identity = Story54PartyIdentityFixture.Identity(clock);
        HumanActorBindingEvidence binding = identity.HumanBinding!;
        identity = scenario switch
        {
            "missing-end" => identity with { HumanBinding = binding with { ValidUntil = null } },
            "custody-too-short" => identity with
            {
                HumanBinding = binding with { Custody = binding.Custody with { ExpiresAt = binding.ValidUntil!.Value.AddTicks(-1) } },
            },
            "actor" => identity with { HumanBinding = binding with { ActorId = "01ARZ3NDEKTSV4RRFFQ69G5FAW" } },
            "tenant" => identity with { TenantId = "foreign-tenant" },
            _ => identity,
        };
        SubmitQueryRequest? submitted = null;
        Uri? requestedUri = null;
        HttpMethod? requestedMethod = null;
        int sent = 0;
        using var http = new HttpClient(new CallerIdentityReplyHttpMessageHandler(async (request, cancellationToken) =>
        {
            sent++;
            requestedUri = request.RequestUri;
            requestedMethod = request.Method;
            submitted = await request.Content!.ReadFromJsonAsync<SubmitQueryRequest>(PartiesJsonOptions.Default, cancellationToken);
            var reply = new SubmitQueryResponse("local-query",
                JsonSerializer.SerializeToElement(new PartyIdentityResult(PartyIdentityOutcome.Resolved, identity), PartiesJsonOptions.Default),
                Success: scenario != "failed-envelope");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(reply, options: PartiesJsonOptions.Default) };
        })) { BaseAddress = new Uri("https://identity.invalid/") };

        AgentPartyValidationResult result = await new PartiesAgentPartyDirectory(new HttpPartiesIdentityClient(http, clock),
            Substitute.For<IAgentDependencyAvailability>(), clock)
            .ValidateCallerPartyAsync("tenant-a", "party-a", actor, CancellationToken.None);

        sent.ShouldBe(1);
        requestedMethod.ShouldBe(HttpMethod.Post);
        requestedUri.ShouldNotBeNull().AbsolutePath.ShouldBe("/api/v1/queries");
        SubmitQueryRequest query = submitted.ShouldNotBeNull();
        query.Tenant.ShouldBe("tenant-a");
        query.Domain.ShouldBe("party");
        query.AggregateId.ShouldBe("party-a");
        query.EntityId.ShouldBe("party-a");
        query.Payload!.Value.Deserialize<ResolvePartyIdentity>(PartiesJsonOptions.Default)
            .ShouldBe(new ResolvePartyIdentity("tenant-a", "party-a", actor.ActorId));
        result.Status.ShouldBe(valid ? PartyLinkValidationStatus.Valid : PartyLinkValidationStatus.Unavailable);
        result.PartyId.ShouldBe(valid ? "party-a" : null);
        result.CallerValidUntil.ShouldBe(valid ? actor.ValidUntil : null);
    }

    /// <summary>The caller deadline carries the earliest actor, current binding or finite custody expiry.</summary>
    [Theory]
    [InlineData("actor")]
    [InlineData("binding")]
    [InlineData("custody")]
    public async Task SuccessfulCallerValidationCarriesTheEarliestAuthorityDeadline(string earliest)
    {
        var clock = new Story54TimeProvider();
        DateTimeOffset expected = clock.Now.AddSeconds(5);
        CurrentHumanActorEvidence actor = Story54PartyIdentityFixture.Actor(clock);
        HumanActorBindingEvidence binding = Story54PartyIdentityFixture.Binding(clock);
        if (earliest == "actor")
        {
            actor = actor with { ValidUntil = expected };
        }
        else if (earliest == "binding")
        {
            binding = binding with { ValidUntil = expected };
        }
        else
        {
            // Retention remains derived from immutable effective time even when its deadline is near.
            binding = binding with
            {
                ValidFrom = expected.AddDays(-365),
                ValidUntil = expected,
                Custody = binding.Custody with { ExpiresAt = expected },
            };
        }
        var client = Substitute.For<IPartiesIdentityClient>();
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new PartyIdentityResult(PartyIdentityOutcome.Resolved,
                Story54PartyIdentityFixture.Identity(clock) with { HumanBinding = binding }));

        AgentPartyValidationResult result = await new PartiesAgentPartyDirectory(client,
            Substitute.For<IAgentDependencyAvailability>(), clock)
            .ValidateCallerPartyAsync("tenant-a", "party-a", actor, CancellationToken.None);

        result.ShouldBe(new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "party-a") { CallerValidUntil = expected });
    }

    /// <summary>No resolved flag can replace exact current binding, provenance and finite custody evidence.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("tenant")]
    [InlineData("party")]
    [InlineData("actor")]
    [InlineData("version")]
    [InlineData("revision")]
    [InlineData("future")]
    [InlineData("expired")]
    [InlineData("missing-end")]
    [InlineData("empty-interval")]
    [InlineData("reversed-interval")]
    [InlineData("custody-shorter-than-binding")]
    [InlineData("effective-offset")]
    [InlineData("expiry-offset")]
    [InlineData("source")]
    [InlineData("provenance")]
    [InlineData("custody")]
    [InlineData("policy")]
    [InlineData("purpose")]
    [InlineData("lifecycle")]
    [InlineData("receipt")]
    [InlineData("custody-expired")]
    [InlineData("custody-infinite")]
    [InlineData("custody-offset")]
    [InlineData("source-expiry")]
    [InlineData("restore")]
    [InlineData("copies")]
    public async Task CallerRequiresExactCurrentBindingAndFiniteCustody(string corruption)
    {
        var clock = new Story54TimeProvider();
        HumanActorBindingEvidence binding = Story54PartyIdentityFixture.Binding(clock);
        HumanActorBindingEvidence? returned = corruption switch
        {
            "missing" => null,
            "tenant" => binding with { TenantId = "foreign-tenant" },
            "party" => binding with { PartyId = "foreign-party" },
            "actor" => binding with { ActorId = "foreign-actor" },
            "version" => binding with { BindingVersion = 8 },
            "revision" => binding with { ActorRevision = 0 },
            "future" => binding with { ValidFrom = clock.Now.AddTicks(1) },
            "expired" => binding with { ValidUntil = clock.Now },
            "missing-end" => binding with { ValidUntil = null },
            "empty-interval" => binding with { ValidUntil = binding.ValidFrom },
            "reversed-interval" => binding with { ValidUntil = binding.ValidFrom.AddTicks(-1) },
            "custody-shorter-than-binding" => binding with { Custody = binding.Custody with { ExpiresAt = binding.ValidUntil!.Value.AddTicks(-1) } },
            "effective-offset" => binding with { ValidFrom = binding.ValidFrom.ToOffset(TimeSpan.FromHours(1)) },
            "expiry-offset" => binding with { ValidUntil = binding.ValidUntil!.Value.ToOffset(TimeSpan.FromHours(1)) },
            "source" => binding with { SourceId = " " },
            "provenance" => binding with { ProvenanceId = " " },
            "custody" => binding with { Custody = null! },
            "policy" => binding with { Custody = binding.Custody with { PolicyId = " " } },
            "purpose" => binding with { Custody = binding.Custody with { Purpose = "different-purpose" } },
            "lifecycle" => binding with { Custody = binding.Custody with { LifecycleRevision = 0 } },
            "receipt" => binding with { Custody = binding.Custody with { EvidenceId = " " } },
            "custody-expired" => binding with { Custody = binding.Custody with { ExpiresAt = clock.Now } },
            "custody-infinite" => binding with { Custody = binding.Custody with { ExpiresAt = DateTimeOffset.MaxValue } },
            "custody-offset" => binding with { Custody = binding.Custody with { ExpiresAt = binding.Custody.ExpiresAt.ToOffset(TimeSpan.FromHours(1)) } },
            "source-expiry" => binding with { Custody = binding.Custody with { SourceExpiryEnforced = false } },
            "restore" => binding with { Custody = binding.Custody with { RestoreSafe = false } },
            "copies" => binding with { Custody = binding.Custody with { DerivedCopiesCovered = false } },
            _ => throw new ArgumentOutOfRangeException(nameof(corruption)),
        };
        var client = Substitute.For<IPartiesIdentityClient>();
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new PartyIdentityResult(PartyIdentityOutcome.Resolved, Story54PartyIdentityFixture.Identity(clock) with { HumanBinding = returned }));

        var result = await new PartiesAgentPartyDirectory(client, Substitute.For<IAgentDependencyAvailability>(), clock)
            .ValidateCallerPartyAsync("tenant-a", "party-a", Story54PartyIdentityFixture.Actor(clock), CancellationToken.None);

        result.ShouldBe(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null));
    }

    /// <summary>Actor, binding and custody expiry remain exclusive at the owner read's completion.</summary>
    [Theory]
    [InlineData("actor")]
    [InlineData("binding")]
    [InlineData("custody")]
    [InlineData("rollback")]
    public async Task CallerEvidenceIsRecheckedAfterTheOwnerRead(string transition)
    {
        var clock = new Story54TimeProvider();
        CurrentHumanActorEvidence actor = Story54PartyIdentityFixture.Actor(clock) with { ValidUntil = clock.Now.AddDays(366) };
        PartyIdentityEvidence identity = Story54PartyIdentityFixture.Identity(clock);
        if (transition is "actor")
        {
            actor = actor with { ValidUntil = clock.Now.AddSeconds(1) };
        }
        if (transition is "custody")
        {
            identity = identity with { HumanBinding = identity.HumanBinding! with { ValidUntil = identity.HumanBinding.Custody.ExpiresAt } };
        }
        var client = Substitute.For<IPartiesIdentityClient>();
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            clock.Now = transition switch
            {
                "actor" => actor.ValidUntil,
                "binding" => identity.HumanBinding!.ValidUntil!.Value,
                "custody" => identity.HumanBinding!.Custody.ExpiresAt,
                _ => clock.Now.AddTicks(-1),
            };
            return new PartyIdentityResult(PartyIdentityOutcome.Resolved, identity);
        });

        (await new PartiesAgentPartyDirectory(client, Substitute.For<IAgentDependencyAvailability>(), clock)
            .ValidateCallerPartyAsync("tenant-a", "party-a", actor, CancellationToken.None)).Status.ShouldBe(PartyLinkValidationStatus.Unavailable);
    }
#endif
}
