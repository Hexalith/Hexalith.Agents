using System.Security.Claims;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Parties.Client.Abstractions;
#if HEXALITH_PARTIES_SOURCE
using Hexalith.Parties.Contracts.Models;
using Hexalith.Parties.Contracts.Queries;
using Hexalith.Parties.Contracts.ValueObjects;
#endif

using Microsoft.AspNetCore.Http;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Proves cancellation stops current-authority reads and Party link dispatch; substitutes grant no live readiness.</summary>
public sealed class AuthorityCancellationAdmissionTests
{
    /// <summary>A cancelled request never starts an authenticated actor read on either administration path.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdministrationCancellationBeforeAdmissionPerformsNoSourceRead(bool platform)
    {
        var actors = Substitute.For<ICurrentHumanActorSource>();
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await Administration(tenants, actors).GetContextAsync(platform, cancellation.Token));

        actors.ReceivedCalls().ShouldBeEmpty();
        tenants.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A failed noncooperative authority source cannot convert cancellation into an identity result.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task AdministrationCancellationDuringSourceFailureIsPreserved(bool platform, bool roleRead)
    {
        using var cancellation = new CancellationTokenSource();
        var clock = new Story54TimeProvider();
        var actors = Substitute.For<ICurrentHumanActorSource>();
        actors.ReadAsync("tenant-a", "subject", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (!roleRead)
            {
                cancellation.Cancel();
                throw new InvalidOperationException("private source failure");
            }

            return Actor(clock);
        });
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        tenants.ReadAsync(Arg.Any<string>(), platform, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cancellation.Cancel();
            return Task.FromException<TenantAuthorityEvidence?>(new InvalidOperationException("private source failure"));
        });

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await Administration(tenants, actors, clock).GetContextAsync(platform, cancellation.Token));

        if (!roleRead)
        {
            tenants.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>A cancelled tenant read never starts authenticated actor resolution.</summary>
    [Fact]
    public async Task TenantCancellationBeforeAdmissionPerformsNoSourceRead()
    {
        var actors = Substitute.For<ICurrentHumanActorSource>();
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        var reader = new ProjectedTenantAccessReader(tenants, Substitute.For<IAgentDependencyAvailability>(), new Story54TimeProvider(), actors);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => reader.ReadAsync("tenant-a", "subject", "party-a", cancellation.Token));

        actors.ReceivedCalls().ShouldBeEmpty();
        tenants.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Actor or tenant source failure after cancellation never becomes an ordinary unavailable response.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TenantCancellationDuringSourceFailureIsPreserved(bool roleRead)
    {
        using var cancellation = new CancellationTokenSource();
        var clock = new Story54TimeProvider();
        var actors = Substitute.For<ICurrentHumanActorSource>();
        actors.ReadAsync("tenant-a", "subject", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (!roleRead)
            {
                cancellation.Cancel();
                throw new InvalidOperationException("private source failure");
            }

            return Actor(clock);
        });
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        tenants.ReadAsync("tenant-a", false, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cancellation.Cancel();
            return Task.FromException<TenantAuthorityEvidence?>(new InvalidOperationException("private source failure"));
        });
        var reader = new ProjectedTenantAccessReader(tenants, Substitute.For<IAgentDependencyAvailability>(), clock, actors);

        await Should.ThrowAsync<OperationCanceledException>(() => reader.ReadAsync("tenant-a", "subject", "party-a", cancellation.Token));

        if (!roleRead)
        {
            tenants.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>A failed cancelled approver read cannot proceed to another configured source or confirmation.</summary>
    [Fact]
    public async Task ApproverCancellationDuringSourceFailureStopsBeforeTheNextSource()
    {
        using var cancellation = new CancellationTokenSource();
        var source = Substitute.For<ICurrentApproverEvidenceSource>();
        var first = new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, PartyId: "party-a", TenantRole: null);
        var second = new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, PartyId: "party-b", TenantRole: null);
        source.ReadAsync("tenant-a", first, null, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cancellation.Cancel();
            return Task.FromException<IReadOnlyList<CurrentApproverEvidence>>(new InvalidOperationException("private source failure"));
        });
        var resolver = new ProjectedApproverPolicyResolver(source, Substitute.For<IAgentDependencyAvailability>(), new Story54TimeProvider());

        await Should.ThrowAsync<OperationCanceledException>(() => resolver.ResolveAsync("tenant-a", new([first, second], ApproverPolicyBasisDisclosure.UserVisible), cancellation.Token));

        await source.Received(1).ReadAsync("tenant-a", first, null, Arg.Any<CancellationToken>());
        await source.DidNotReceive().ReadAsync("tenant-a", second, null, Arg.Any<CancellationToken>());
        await source.DidNotReceiveWithAnyArgs().ConfirmAsync(default!, default!, default);
    }

    /// <summary>A cancelled Party link starts neither a lookup nor provisioning.</summary>
    [Theory]
    [InlineData(AgentPartyLinkSource.ExistingParty)]
    [InlineData(AgentPartyLinkSource.ProvisionNewParty)]
    public async Task PartyLinkCancellationBeforeAdmissionPerformsNoReadOrDispatch(AgentPartyLinkSource source)
    {
        var directory = Substitute.For<IAgentPartyDirectory>();
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            new AgentPartyIdentityOrchestrator(directory, dispatcher).ExecuteAsync(Request(source), cancellation.Token));

        directory.ReceivedCalls().ShouldBeEmpty();
        dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A dependency returning after cancellation cannot construct or dispatch a Party link intent.</summary>
    [Theory]
    [InlineData(AgentPartyLinkSource.ExistingParty)]
    [InlineData(AgentPartyLinkSource.ProvisionNewParty)]
    public async Task PartyLinkCancellationDuringLookupStopsBeforeIntentConstruction(AgentPartyLinkSource source)
    {
        var directory = Substitute.For<IAgentPartyDirectory>();
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        using var cancellation = new CancellationTokenSource();
        AgentPartyValidationResult CancelThenReturn()
        {
            cancellation.Cancel();
            return new(PartyLinkValidationStatus.Valid, "party-a");
        }

        directory.ValidateExistingPartyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => CancelThenReturn());
        directory.ProvisionAgentPartyAsync(Arg.Any<string>(), Arg.Any<AgentPartyProvisioningRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => CancelThenReturn());

        await Should.ThrowAsync<OperationCanceledException>(() =>
            new AgentPartyIdentityOrchestrator(directory, dispatcher).ExecuteAsync(Request(source), cancellation.Token));

        dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

#if HEXALITH_PARTIES_SOURCE
    /// <summary>A cancelled identity read performs no Parties client call.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartyDirectoryCancellationBeforeAdmissionPerformsNoClientRead(bool caller)
    {
        var clock = new Story54TimeProvider();
        var client = Substitute.For<IPartiesIdentityClient>();
        var directory = new PartiesAgentPartyDirectory(client, Substitute.For<IAgentDependencyAvailability>(), clock);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => caller
            ? directory.ValidateCallerPartyAsync("tenant-a", "party-a", Story54PartyIdentityFixture.Actor(clock), cancellation.Token)
            : directory.ValidateExistingPartyAsync("tenant-a", "party-a", cancellation.Token));

        client.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A failed cancelled Parties client read cannot turn into a processed unavailable result.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task PartyDirectoryCancellationDuringClientReadIsPreserved(bool failure, bool caller)
    {
        var clock = new Story54TimeProvider();
        var client = Substitute.For<IPartiesIdentityClient>();
        using var cancellation = new CancellationTokenSource();
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cancellation.Cancel();
            if (failure)
            {
                throw new InvalidOperationException("private source failure");
            }

            return new PartyIdentityResult(PartyIdentityOutcome.Resolved, caller
                ? Story54PartyIdentityFixture.Identity(clock)
                : new PartyIdentityEvidence(1, "tenant-a", "party-a", PartyIdentityClassification.Organization,
                    true, false, false, 1, clock.GetUtcNow(), "observation", null));
        });
        var directory = new PartiesAgentPartyDirectory(client, Substitute.For<IAgentDependencyAvailability>(), clock);

        await Should.ThrowAsync<OperationCanceledException>(() => caller
            ? directory.ValidateCallerPartyAsync("tenant-a", "party-a", Story54PartyIdentityFixture.Actor(clock), cancellation.Token)
            : directory.ValidateExistingPartyAsync("tenant-a", "party-a", cancellation.Token));
    }
#endif

    private static AgentPartyLinkRequest Request(AgentPartyLinkSource source)
        => new("message", "correlation", "tenant-a", "hexa", "actor-a", true, source, AgentPartyLinkOperation.Link, "party-a");

    private static CurrentHumanActorEvidence Actor(Story54TimeProvider clock)
        => new("tenant-a", "subject", "actor-a", "party-a", 1, 1, clock.GetUtcNow(), clock.GetUtcNow().AddSeconds(5));

    private static HttpAgentAdministrationContextProvider Administration(ICurrentTenantAuthoritySource tenants,
        ICurrentHumanActorSource actors, Story54TimeProvider? clock = null)
        => new(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenantId", "tenant-a"), new Claim("sub", "subject")], "test")),
            },
        }, tenants, actors, Substitute.For<IAgentDependencyAvailability>(), clock ?? new Story54TimeProvider());
}
