using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Ports;
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

    private static CurrentHumanActorEvidence ValidActor(Story54TimeProvider clock)
        => new("tenant-a", "authenticated-subject", "actor-a", "party-a", 1, 1, clock.Now, clock.Now.AddSeconds(5));
}
