using System.Security.Claims;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Ports;

using Microsoft.AspNetCore.Http;

using NSubstitute;

using Shouldly;

#if HEXALITH_PARTIES_SOURCE
using Hexalith.Parties.Client.Abstractions;
#endif

namespace Hexalith.Agents.Server.Tests;

/// <summary>Human authority cannot read an owner source while its custody dependency is unavailable.</summary>
public sealed class Story54CustodyAvailabilityTests
{
    /// <summary>Tenant access refuses before either current actor or role evidence is read.</summary>
    [Fact]
    public async Task TenantAccessStopsBeforeAuthoritySourcesWhenCustodyIsUnavailable()
    {
        var availability = UnavailableCustody();
        var actors = Substitute.For<ICurrentHumanActorSource>();
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();

        TenantAccessReadResult result = await new ProjectedTenantAccessReader(tenants, availability, new Story54TimeProvider(), actors)
            .ReadAsync("tenant-a", "subject-a", "party-a", CancellationToken.None);

        result.Dependency.ShouldBe(new DependencyNotAvailable("EXT-SECRETS-1"));
        actors.ReceivedCalls().ShouldBeEmpty();
        tenants.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Neither tenant nor Platform administration starts an actor read without custody.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdministrationRemainsUnelevatedWhenCustodyIsUnavailable(bool platform)
    {
        var actors = Substitute.For<ICurrentHumanActorSource>();
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("tenantId", "tenant-a"), new Claim("sub", "subject-a")], "test"));
        var provider = new HttpAgentAdministrationContextProvider(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } },
            tenants, actors, UnavailableCustody(), new Story54TimeProvider());

        AgentAdministrationContext context = await provider.GetContextAsync(platform, CancellationToken.None);

        context.IsAuthorized.ShouldBeFalse();
        context.TenantId.ShouldBe("tenant-a");
        context.ActorUserId.ShouldBe("subject-a");
        actors.ReceivedCalls().ShouldBeEmpty();
        tenants.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>An approver policy never reads a configured source while Human custody is unavailable.</summary>
    [Fact]
    public async Task ApproverResolutionStopsBeforeCurrentSourceWhenCustodyIsUnavailable()
    {
        var source = Substitute.For<ICurrentApproverEvidenceSource>();
        var configured = new ApproverPolicySource(ApproverPolicySourceKind.TenantRole, null, "Approver");
        var policy = new AgentApproverPolicy([configured], ApproverPolicyBasisDisclosure.OperatorOnly);

        ApproverPolicyResolutionResult result = await new ProjectedApproverPolicyResolver(
            source, UnavailableCustody(), new Story54TimeProvider())
            .ResolveAsync("tenant-a", policy, CancellationToken.None);

        result.Dependency.ShouldBe(new DependencyNotAvailable("EXT-SECRETS-1"));
        source.ReceivedCalls().ShouldBeEmpty();
    }

#if HEXALITH_PARTIES_SOURCE
    /// <summary>Caller validation cannot ask Parties for a Human binding before custody is available.</summary>
    [Fact]
    public async Task CallerPartyValidationStopsBeforeIdentityReadWhenCustodyIsUnavailable()
    {
        var client = Substitute.For<IPartiesIdentityClient>();
        var clock = new Story54TimeProvider();
        AgentPartyValidationResult result = await new PartiesAgentPartyDirectory(client, UnavailableCustody(), clock)
            .ValidateCallerPartyAsync("tenant-a", "party-a", Story54PartyIdentityFixture.Actor(clock), CancellationToken.None);

        result.Dependency.ShouldBe(new DependencyNotAvailable("EXT-SECRETS-1"));
        client.ReceivedCalls().ShouldBeEmpty();
    }
#endif

    private static IAgentDependencyAvailability UnavailableCustody()
    {
        var availability = Substitute.For<IAgentDependencyAvailability>();
        availability.Check("EXT-SECRETS-1").Returns(new DependencyNotAvailable("EXT-SECRETS-1"));
        return availability;
    }
}
