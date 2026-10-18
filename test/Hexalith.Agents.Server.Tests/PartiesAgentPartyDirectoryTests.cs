using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Ports;
using Hexalith.Parties.Client.Abstractions;
using NSubstitute;
using Shouldly;
#if HEXALITH_PARTIES_SOURCE
using Hexalith.Parties.Contracts.Models;
using Hexalith.Parties.Contracts.Queries;
using Hexalith.Parties.Contracts.ValueObjects;
#endif

namespace Hexalith.Agents.Server.Tests;

/// <summary>Local explicit-scope identity adapter tests; substituted clients provide no live availability evidence.</summary>
public sealed class PartiesAgentPartyDirectoryTests
{
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
        AgentPartyValidationResult provision = await directory.ProvisionAgentPartyAsync("tenant-a", new("hexa", "label"), CancellationToken.None);
        provision.Status.ShouldBe(PartyLinkValidationStatus.Unavailable);
        queries.ReceivedCalls().ShouldBeEmpty();
        commands.ReceivedCalls().ShouldBeEmpty();
    }
#if HEXALITH_PARTIES_SOURCE
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
            true, false, false, 1, clock.GetUtcNow(), "observation", null);
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>())
            .Returns(new PartyIdentityResult(PartyIdentityOutcome.Resolved, evidence));
        var directory = new PartiesAgentPartyDirectory(client, Substitute.For<IAgentDependencyAvailability>(), clock);
        AgentPartyValidationResult result = caller
            ? await directory.ValidateCallerPartyAsync("tenant-a", "party-a", CancellationToken.None)
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
        client.ReceivedCalls().ShouldBeEmpty();
    }
#endif
}
