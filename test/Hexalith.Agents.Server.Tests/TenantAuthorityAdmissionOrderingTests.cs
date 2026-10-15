using System.Security.Claims;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Ports;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Proves invalid actor evidence stops before tenant authority lookup and expiry remains exclusive after an await.</summary>
public sealed class TenantAuthorityAdmissionOrderingTests
{
    /// <summary>Verifies that invalid actor evidence stops before tenant authority lookup.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("cross-tenant")]
    [InlineData("subject-mismatch")]
    [InlineData("expired")]
    [InlineData("future-observation")]
    [InlineData("blank-actor")]
    [InlineData("blank-party")]
    [InlineData("invalid-binding")]
    [InlineData("invalid-source")]
    [InlineData("caller-party-mismatch")]
    [InlineData("cached-observation")]
    [InlineData("non-utc-observation")]
    [InlineData("non-utc-expiry")]
    public async Task InvalidActorEvidenceStopsBeforeTenantAuthorityLookup(string invalidCase)
    {
        var clock = new Story54TimeProvider();
        CurrentHumanActorEvidence original = ValidActor(clock);
        CurrentHumanActorEvidence? actor = invalidCase switch
        {
            "missing" => null,
            "cross-tenant" => original with { TenantId = "tenant-b" },
            "subject-mismatch" => original with { Subject = "another-subject" },
            "expired" => original with { ValidUntil = clock.Now },
            "future-observation" => original with { ObservedAt = clock.Now.AddSeconds(1) },
            "blank-actor" => original with { ActorId = " " },
            "blank-party" => original with { PartyId = string.Empty },
            "invalid-binding" => original with { BindingVersion = 0 },
            "invalid-source" => original with { SourcePosition = 0 },
            "caller-party-mismatch" => original,
            "cached-observation" => original with { ObservedAt = clock.Now.AddTicks(-1) },
            "non-utc-observation" => original with { ObservedAt = clock.Now.ToOffset(TimeSpan.FromHours(1)) },
            "non-utc-expiry" => original with { ValidUntil = clock.Now.AddSeconds(5).ToOffset(TimeSpan.FromHours(1)) },
            _ => throw new ArgumentOutOfRangeException(nameof(invalidCase)),
        };
        var actors = Substitute.For<ICurrentHumanActorSource>();
        actors.ReadAsync("tenant-a", "authenticated-subject", Arg.Any<CancellationToken>()).Returns(actor);
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        var reader = new ProjectedTenantAccessReader(tenants, Substitute.For<IAgentDependencyAvailability>(), clock, actors);

        string callerParty = invalidCase == "caller-party-mismatch" ? "another-party" : string.Empty;
        TenantAccessReadResult result = await reader.ReadAsync("tenant-a", "authenticated-subject", callerParty, CancellationToken.None);

        result.Outcome.ShouldBe(AgentInteractionGateOutcome.Unauthorized);
        result.IsFresh.ShouldBeFalse();
        tenants.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Verifies that a binding expiring during a tenant-source await grants no access.</summary>
    [Fact]
    public async Task ActorExpiringDuringTenantAuthorityAwaitCannotGrantAccess()
    {
        var clock = new Story54TimeProvider();
        var actors = Substitute.For<ICurrentHumanActorSource>();
        actors.ReadAsync("tenant-a", "authenticated-subject", Arg.Any<CancellationToken>()).Returns(ValidActor(clock));
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        tenants.ReadAsync("tenant-a", false, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            clock.Now = clock.Now.AddSeconds(5);
            return new TenantAuthorityEvidence("tenant-a", "tenants", 2, clock.Now, clock.Now.AddSeconds(5),
                [new(1, string.Empty, null, "TenantActive", true), new(2, "actor-a", null, "Agents.Administrator", true)],
                SourceAggregateId: "tenant-a");
        });
        var reader = new ProjectedTenantAccessReader(tenants, Substitute.For<IAgentDependencyAvailability>(), clock, actors);

        TenantAccessReadResult result = await reader.ReadAsync("tenant-a", "authenticated-subject", string.Empty, CancellationToken.None);

        result.Outcome.ShouldBe(AgentInteractionGateOutcome.Unauthorized);
        result.IsFresh.ShouldBeFalse();
        await tenants.Received(1).ReadAsync("tenant-a", false, Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies invalid human binding stops both administration principal paths before protected role lookup.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("cross-tenant")]
    [InlineData("subject-mismatch")]
    [InlineData("expired")]
    [InlineData("future-observation")]
    [InlineData("cached-observation")]
    [InlineData("blank-actor")]
    [InlineData("blank-party")]
    [InlineData("invalid-binding")]
    [InlineData("invalid-source")]
    [InlineData("non-utc-observation")]
    [InlineData("non-utc-expiry")]
    public async Task InvalidAdministrationActorStopsBeforeTenantAndGlobalAuthorityLookup(string invalidCase)
    {
        foreach (bool platform in new[] { false, true })
        {
            var clock = new Story54TimeProvider();
            CurrentHumanActorEvidence original = ValidActor(clock);
            CurrentHumanActorEvidence? actor = invalidCase switch
            {
                "missing" => null,
                "cross-tenant" => original with { TenantId = "tenant-b" },
                "subject-mismatch" => original with { Subject = "another-subject" },
                "expired" => original with { ValidUntil = clock.Now },
                "future-observation" => original with { ObservedAt = clock.Now.AddTicks(1) },
                "cached-observation" => original with { ObservedAt = clock.Now.AddTicks(-1) },
                "blank-actor" => original with { ActorId = " " },
                "blank-party" => original with { PartyId = string.Empty },
                "invalid-binding" => original with { BindingVersion = 0 },
                "invalid-source" => original with { SourcePosition = 0 },
                "non-utc-observation" => original with { ObservedAt = clock.Now.ToOffset(TimeSpan.FromHours(1)) },
                "non-utc-expiry" => original with { ValidUntil = clock.Now.AddSeconds(5).ToOffset(TimeSpan.FromHours(1)) },
                _ => throw new ArgumentOutOfRangeException(nameof(invalidCase)),
            };
            var actors = Substitute.For<ICurrentHumanActorSource>();
            actors.ReadAsync("tenant-a", "authenticated-subject", Arg.Any<CancellationToken>()).Returns(actor);
            var tenants = Substitute.For<ICurrentTenantAuthoritySource>();

            AgentAdministrationContext result = await AdministrationProvider(tenants, actors, clock)
                .GetContextAsync(platform, CancellationToken.None);

            result.IsAgentsAdmin.ShouldBeFalse();
            result.IsPlatformOperator.ShouldBeFalse();
            tenants.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>Verifies exclusive actor expiry or clock rollback during protected role lookup cannot grant either principal.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task AdministrationAuthorityIsRecheckedAfterTheRoleAwait(bool platform, bool rollback)
    {
        var clock = new Story54TimeProvider();
        var actors = Substitute.For<ICurrentHumanActorSource>();
        actors.ReadAsync("tenant-a", "authenticated-subject", Arg.Any<CancellationToken>()).Returns(ValidActor(clock));
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        tenants.ReadAsync(platform ? "system" : "tenant-a", platform, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            clock.Now = rollback ? clock.Now.AddTicks(-1) : clock.Now.AddSeconds(5);
            return ValidAuthority(clock, platform);
        });

        AgentAdministrationContext result = await AdministrationProvider(tenants, actors, clock)
            .GetContextAsync(platform, CancellationToken.None);

        result.IsAgentsAdmin.ShouldBeFalse();
        result.IsPlatformOperator.ShouldBeFalse();
        await tenants.Received(1).ReadAsync(platform ? "system" : "tenant-a", platform, Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies fresh exact evidence selects only the requested principal and uses the global source for Platform.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdministrationSelectsOnePrincipalFromTheExactAuthoritySource(bool platform)
    {
        var clock = new Story54TimeProvider();
        var actors = Substitute.For<ICurrentHumanActorSource>();
        actors.ReadAsync("tenant-a", "authenticated-subject", Arg.Any<CancellationToken>()).Returns(ValidActor(clock));
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        tenants.ReadAsync(platform ? "system" : "tenant-a", platform, Arg.Any<CancellationToken>())
            .Returns(ValidAuthority(clock, platform));

        AgentAdministrationContext result = await AdministrationProvider(tenants, actors, clock)
            .GetContextAsync(platform, CancellationToken.None);

        result.IsAgentsAdmin.ShouldBe(!platform);
        result.IsPlatformOperator.ShouldBe(platform);
        result.TenantId.ShouldBe(platform ? "system" : "tenant-a");
        result.ActorUserId.ShouldBe("actor-a");
        await tenants.Received(1).ReadAsync(platform ? "system" : "tenant-a", platform, Arg.Any<CancellationToken>());
    }

    private static HttpAgentAdministrationContextProvider AdministrationProvider(ICurrentTenantAuthoritySource tenants,
        ICurrentHumanActorSource actors, Story54TimeProvider clock)
        => new(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("tenantId", "tenant-a"), new Claim("sub", "authenticated-subject")], "test")),
            },
        }, tenants, actors, Substitute.For<IAgentDependencyAvailability>(), clock);

    private static TenantAuthorityEvidence ValidAuthority(Story54TimeProvider clock, bool platform)
        => platform
            ? new("system", "global-administrators", 1, clock.Now, clock.Now.AddSeconds(5),
                [new(1, "actor-a", null, "Agents.PlatformOperator", true)], SourceAggregateId: "global-administrators")
            : new("tenant-a", "tenants", 2, clock.Now, clock.Now.AddSeconds(5),
                [new(1, string.Empty, null, "TenantActive", true), new(2, "actor-a", null, "Agents.Administrator", true)],
                SourceAggregateId: "tenant-a");

    private static CurrentHumanActorEvidence ValidActor(Story54TimeProvider clock)
        => new("tenant-a", "authenticated-subject", "actor-a", "party-a", 1, 1, clock.Now, clock.Now.AddSeconds(5));
}
