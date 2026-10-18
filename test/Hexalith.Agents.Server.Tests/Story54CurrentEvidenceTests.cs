using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Local current-evidence invariants; source substitutes establish no external availability.</summary>
public sealed class Story54CurrentEvidenceTests
{
    /// <summary>Verifies that tenant authority requires contiguous events equal to current head.</summary>
    [Theory]
    [InlineData(2, 2, true)]
    [InlineData(3, 2, false)]
    [InlineData(2, 3, false)]
    public void TenantAuthorityRequiresContiguousEventsEqualToCurrentHead(long head, long grantPosition, bool allowed)
    {
        var clock = new Story54TimeProvider();
        var evidence = new TenantAuthorityEvidence("tenant-a", "tenants", head, clock.Now, clock.Now.AddSeconds(5),
            [new(1, string.Empty, null, "TenantActive", true), new(grantPosition, "actor-a", null, "Agents.Administrator", true)],
            SourceAggregateId: "tenant-a");
        ProjectedTenantAccessReader.Grants(evidence, "tenant-a", "actor-a", "Agents.Administrator", null, false, clock.Now).ShouldBe(allowed);
    }

    /// <summary>Verifies that revocation and global source substitution fail closed.</summary>
    [Fact]
    public void RevocationAndGlobalSourceSubstitutionFailClosed()
    {
        var clock = new Story54TimeProvider();
        var evidence = new TenantAuthorityEvidence("system", "global-administrators", 2, clock.Now, clock.Now.AddSeconds(5),
            [new(1, "actor-a", null, "Agents.PlatformOperator", true), new(2, "actor-a", null, "Agents.PlatformOperator", false)],
            SourceAggregateId: "global-administrators");
        ProjectedTenantAccessReader.Grants(evidence, "system", "actor-a", "Agents.PlatformOperator", null, true, clock.Now).ShouldBeFalse();
        ProjectedTenantAccessReader.Grants(evidence with { Domain = "tenants" }, "system", "actor-a", "Agents.PlatformOperator", null, true, clock.Now).ShouldBeFalse();
    }

    /// <summary>Verifies that the actor's latest role assignment decides whichever Party it names.</summary>
    [Theory]
    [InlineData("party-a", true, "party-a", true)]
    [InlineData(null, false, "party-a", false)]
    [InlineData("party-b", false, "party-a", false)]
    [InlineData("party-b", true, "party-a", false)]
    [InlineData("party-b", true, "party-b", true)]
    public void LatestActorRoleAssignmentDecidesWhicheverPartyItNames(string? laterParty, bool laterGranted, string requestedParty, bool allowed)
    {
        var clock = new Story54TimeProvider();
        var evidence = new TenantAuthorityEvidence("tenant-a", "tenants", 3, clock.Now, clock.Now.AddSeconds(5),
            [new(1, string.Empty, null, "TenantActive", true), new(2, "actor-a", "party-a", "User", true),
                new(3, "actor-a", laterParty, "User", laterGranted)],
            SourceAggregateId: "tenant-a");
        ProjectedTenantAccessReader.Grants(evidence, "tenant-a", "actor-a", "User", requestedParty, false, clock.Now).ShouldBe(allowed);
    }

    /// <summary>Verifies that a Party-bound administrator revocation still revokes the Party-free administrator.</summary>
    [Fact]
    public void PartyBoundAdministratorRevocationRevokesThePartyFreeAssignment()
    {
        var clock = new Story54TimeProvider();
        var evidence = new TenantAuthorityEvidence("tenant-a", "tenants", 3, clock.Now, clock.Now.AddSeconds(5),
            [new(1, string.Empty, null, "TenantActive", true), new(2, "actor-a", null, "Agents.Administrator", true),
                new(3, "actor-a", "party-a", "Agents.Administrator", false)],
            SourceAggregateId: "tenant-a");
        ProjectedTenantAccessReader.Grants(evidence, "tenant-a", "actor-a", "Agents.Administrator", null, false, clock.Now).ShouldBeFalse();
    }

    /// <summary>Verifies that other FR-33 role history keeps the fold current while unknown roles block it.</summary>
    [Theory]
    [InlineData("Agents.Approver", true)]
    [InlineData("Agents.AuditOperator", true)]
    [InlineData("Agents.Operator", true)]
    [InlineData("Agents.Unknown", false)]
    [InlineData("agents.approver", false)]
    [InlineData("", false)]
    public void OtherFr33RoleHistoryKeepsTheFoldCurrentWhileUnknownRolesBlock(string otherRole, bool allowed)
    {
        var clock = new Story54TimeProvider();
        var evidence = new TenantAuthorityEvidence("tenant-a", "tenants", 3, clock.Now, clock.Now.AddSeconds(5),
            [new(1, string.Empty, null, "TenantActive", true), new(2, "actor-a", null, "Agents.Administrator", true),
                new(3, "actor-b", "party-b", otherRole, true)],
            SourceAggregateId: "tenant-a");
        ProjectedTenantAccessReader.Grants(evidence, "tenant-a", "actor-a", "Agents.Administrator", null, false, clock.Now).ShouldBe(allowed);
    }

    /// <summary>Verifies that committed dependency stops before any current source lookup.</summary>
    [Fact]
    public async Task CommittedDependencyStopsBeforeAnyCurrentSourceLookup()
    {
        var source = Substitute.For<ICurrentTenantAuthoritySource>();
        var reader = new ProjectedTenantAccessReader(source, new CommittedAgentDependencyAvailability(), new Story54TimeProvider());
        (await reader.ReadAsync("tenant-a", "actor-a", "party-a", CancellationToken.None)).Dependency.ShouldBe(new DependencyNotAvailable("EXT-HOST-1"));
        source.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Verifies that multiple role members allow only the exact current human with source access.</summary>
    [Theory]
    [InlineData("actor-a", "party-a", true, true)]
    [InlineData("actor-b", "party-a", true, false)]
    [InlineData("actor-a", "party-b", true, false)]
    [InlineData("actor-a", "party-a", false, false)]
    public async Task MultipleRoleMembersAllowOnlyTheExactCurrentHumanWithSourceAccess(string actor, string party, bool readAccess, bool allowed)
    {
        var clock = new Story54TimeProvider();
        var role = new ApproverPolicySource(ApproverPolicySourceKind.TenantRole, null, "Approver");
        var source = Substitute.For<ICurrentApproverEvidenceSource>();
        source.ReadAsync("tenant-a", role, Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(new CurrentApproverEvidence[]
        {
            Candidate(role, "actor-a", "party-a", readAccess, clock), Candidate(role, "actor-b", "party-b", true, clock),
        });
        source.ConfirmAsync("tenant-a", Arg.Any<IReadOnlyList<CurrentApproverEvidence>>(), Arg.Any<CancellationToken>()).Returns(true);
        var resolver = new ProjectedApproverPolicyResolver(source, Substitute.For<IAgentDependencyAvailability>(), clock);
        var policy = new AgentApproverPolicy([role], ApproverPolicyBasisDisclosure.OperatorOnly);
        var result = await resolver.ResolveForActorAsync("tenant-a", policy, new(actor, party, "conversation-a"), CancellationToken.None);
        result.Sources.Single().Outcome.ShouldBe(allowed ? ApproverSourceOutcome.Resolved : ApproverSourceOutcome.Unauthorized);
        (await resolver.ResolveAsync("tenant-a", policy, CancellationToken.None)).Sources.Single().Outcome.ShouldBe(ApproverSourceOutcome.Resolved);
    }

    /// <summary>Verifies that an action is authorized by the union of approver sources and still fails closed on uncertainty.</summary>
    [Theory]
    [InlineData("actor-a", "party-a", false, ApproverPolicyValidationStatus.Valid)]
    [InlineData("actor-b", "party-b", false, ApproverPolicyValidationStatus.Valid)]
    [InlineData("actor-c", "party-c", false, ApproverPolicyValidationStatus.Unauthorized)]
    [InlineData("actor-a", "party-b", false, ApproverPolicyValidationStatus.Unauthorized)]
    [InlineData("actor-a", "party-a", true, ApproverPolicyValidationStatus.Unavailable)]
    public async Task ActionIsAuthorizedByTheUnionOfApproverSourcesAndFailsClosedOnUncertainty(
        string actor, string party, bool secondUnavailable, ApproverPolicyValidationStatus expected)
    {
        var clock = new Story54TimeProvider();
        var first = new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, "party-a", null);
        var second = new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, "party-b", null);
        var source = Substitute.For<ICurrentApproverEvidenceSource>();
        source.ReadAsync("tenant-a", first, "conversation-a", Arg.Any<CancellationToken>())
            .Returns(new[] { Candidate(first, "actor-a", "party-a", true, clock) });
        if (secondUnavailable)
        {
            source.ReadAsync("tenant-a", second, "conversation-a", Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("source-unavailable"));
        }
        else
        {
            source.ReadAsync("tenant-a", second, "conversation-a", Arg.Any<CancellationToken>())
                .Returns(new[] { Candidate(second, "actor-b", "party-b", true, clock) });
        }

        source.ConfirmAsync("tenant-a", Arg.Any<IReadOnlyList<CurrentApproverEvidence>>(), Arg.Any<CancellationToken>()).Returns(true);
        var resolver = new ProjectedApproverPolicyResolver(source, Substitute.For<IAgentDependencyAvailability>(), clock);
        var policy = new AgentApproverPolicy([first, second], ApproverPolicyBasisDisclosure.OperatorOnly);

        var result = await resolver.ResolveForActorAsync("tenant-a", policy, new(actor, party, "conversation-a"), CancellationToken.None);

        ApproverPolicyVerdict.EvaluateAction(policy, result).ShouldBe(expected);
    }

    /// <summary>Verifies that earlier configuration candidate expiring during alater source read is unavailable.</summary>
    [Fact]
    public async Task EarlierConfigurationCandidateExpiringDuringALaterSourceReadIsUnavailable()
    {
        var clock = new Story54TimeProvider();
        var first = new ApproverPolicySource(ApproverPolicySourceKind.TenantRole, null, "First");
        var second = new ApproverPolicySource(ApproverPolicySourceKind.TenantRole, null, "Second");
        var source = Substitute.For<ICurrentApproverEvidenceSource>();
        source.ReadAsync("tenant-a", first, null, Arg.Any<CancellationToken>()).Returns(new[] { Candidate(first, "actor-a", "party-a", true, clock) });
        source.ReadAsync("tenant-a", second, null, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            clock.Now = clock.Now.AddSeconds(10);
            return new[] { Candidate(second, "actor-b", "party-b", true, clock) };
        });
        source.ConfirmAsync("tenant-a", Arg.Any<IReadOnlyList<CurrentApproverEvidence>>(), Arg.Any<CancellationToken>()).Returns(true);
        var resolver = new ProjectedApproverPolicyResolver(source, Substitute.For<IAgentDependencyAvailability>(), clock);
        var result = await resolver.ResolveAsync("tenant-a", new([first, second], ApproverPolicyBasisDisclosure.OperatorOnly), CancellationToken.None);
        result.Sources.ShouldAllBe(item => item.Outcome == ApproverSourceOutcome.Unavailable);
    }

    /// <summary>Verifies that legacy caller and unknown source outcomes grant no authority.</summary>
    [Fact]
    public async Task LegacyCallerAndUnknownSourceOutcomesGrantNoAuthority()
    {
        var source = Substitute.For<ICurrentApproverEvidenceSource>();
        var resolver = new ProjectedApproverPolicyResolver(source, Substitute.For<IAgentDependencyAvailability>(), new Story54TimeProvider());
        var result = await resolver.ResolveAsync("tenant-a", new([new(ApproverPolicySourceKind.Caller, null, null)], ApproverPolicyBasisDisclosure.OperatorOnly), CancellationToken.None);
        result.Sources.Single().Outcome.ShouldBe(ApproverSourceOutcome.Unavailable);
        await source.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<ApproverPolicySource>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that query subject is resolved to the current stable actor before role comparison.</summary>
    [Theory]
    [InlineData("stable-actor", true)]
    [InlineData("substituted-actor", false)]
    public async Task QuerySubjectIsResolvedToTheCurrentStableActorBeforeRoleComparison(string resolvedActor, bool allowed)
    {
        var clock = new Story54TimeProvider();
        var actors = Substitute.For<ICurrentHumanActorSource>();
        actors.ReadAsync("tenant-a", "authenticated-subject", Arg.Any<CancellationToken>()).Returns(
            new CurrentHumanActorEvidence("tenant-a", "authenticated-subject", resolvedActor, "party-a", 1, 1, clock.Now, clock.Now.AddSeconds(5)));
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        tenants.ReadAsync("tenant-a", false, Arg.Any<CancellationToken>()).Returns(new TenantAuthorityEvidence(
            "tenant-a", "tenants", 2, clock.Now, clock.Now.AddSeconds(5),
            [new(1, string.Empty, null, "TenantActive", true), new(2, "stable-actor", null, "Agents.Administrator", true)],
            SourceAggregateId: "tenant-a"));
        var reader = new ProjectedTenantAccessReader(tenants, Substitute.For<IAgentDependencyAvailability>(), clock, actors);
        var result = await reader.ReadAsync("tenant-a", "authenticated-subject", string.Empty, CancellationToken.None);
        result.Outcome.ShouldBe(allowed ? AgentInteractionGateOutcome.Satisfied : AgentInteractionGateOutcome.Unauthorized);
    }

    private static CurrentApproverEvidence Candidate(ApproverPolicySource source, string actor, string party, bool readAccess, Story54TimeProvider clock)
        => new(source, new("tenant-a", "subject", actor, party, 1, 1, clock.Now, clock.Now.AddSeconds(5)), "conversation-a", readAccess);
}
