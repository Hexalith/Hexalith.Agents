using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;

using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Proves exact current approver evidence admission without executing an external dependency.</summary>
public sealed class ApproverEvidenceAdmissionTests
{
    private static readonly ApproverPolicySource _source = new(ApproverPolicySourceKind.TenantRole, null, "Approver");
    private static readonly AgentApproverPolicy _policy = new([_source], ApproverPolicyBasisDisclosure.OperatorOnly);

    /// <summary>Rejects malformed current bindings before confirmation and withholds their candidate identities.</summary>
    [Theory]
    [InlineData("blank-subject")]
    [InlineData("blank-actor")]
    [InlineData("blank-party")]
    [InlineData("wrong-tenant")]
    [InlineData("wrong-source")]
    [InlineData("non-utc-observation")]
    [InlineData("non-utc-expiry")]
    [InlineData("missing-binding")]
    [InlineData("missing-position")]
    [InlineData("null-actor")]
    [InlineData("null-candidate")]
    public async Task MalformedCurrentBindingsGrantNoAuthority(string scenario)
    {
        var clock = new Story54TimeProvider();
        CurrentApproverEvidence candidate = Candidate(clock);
        candidate = scenario switch
        {
            "blank-subject" => candidate with { Actor = candidate.Actor with { Subject = " " } },
            "blank-actor" => candidate with { Actor = candidate.Actor with { ActorId = " " } },
            "blank-party" => candidate with { Actor = candidate.Actor with { PartyId = " " } },
            "wrong-tenant" => candidate with { Actor = candidate.Actor with { TenantId = "tenant-b" } },
            "wrong-source" => candidate with { Source = _source with { TenantRole = "Other" } },
            "non-utc-observation" => candidate with { Actor = candidate.Actor with { ObservedAt = clock.Now.ToOffset(TimeSpan.FromHours(1)) } },
            "non-utc-expiry" => candidate with { Actor = candidate.Actor with { ValidUntil = clock.Now.AddMinutes(1).ToOffset(TimeSpan.FromHours(1)) } },
            "missing-binding" => candidate with { Actor = candidate.Actor with { BindingVersion = 0 } },
            "missing-position" => candidate with { Actor = candidate.Actor with { SourcePosition = 0 } },
            "null-actor" => candidate with { Actor = null! },
            "null-candidate" => null!,
            _ => throw new ArgumentException("Unknown fixture scenario.", nameof(scenario)),
        };
        var source = Substitute.For<ICurrentApproverEvidenceSource>();
        source.ReadAsync("tenant-a", _source, null, Arg.Any<CancellationToken>()).Returns(new[] { candidate });

        ApproverPolicyResolutionResult result = await Resolver(source, clock).ResolveAsync("tenant-a", _policy, CancellationToken.None);

        AssertUnavailable(result);
        await source.DidNotReceiveWithAnyArgs().ConfirmAsync(default!, default!, default);
    }

    /// <summary>Rejects incomplete request authority before any protected source read or confirmation.</summary>
    [Theory]
    [InlineData(" ", "actor-a", "party-a", "conversation-a")]
    [InlineData("tenant-a", " ", "party-a", "conversation-a")]
    [InlineData("tenant-a", "actor-a", " ", "conversation-a")]
    [InlineData("tenant-a", "actor-a", "party-a", " ")]
    public async Task IncompleteActionIdentityStopsBeforeSourceAccess(string tenant, string actor, string party, string conversation)
    {
        var source = Substitute.For<ICurrentApproverEvidenceSource>();

        ApproverPolicyResolutionResult result = await Resolver(source, new Story54TimeProvider()).ResolveForActorAsync(
            tenant, _policy, new(actor, party, conversation), CancellationToken.None);

        result.Sources.Single().Outcome.ShouldBe(ApproverSourceOutcome.Unauthorized);
        source.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Rechecks the current interval and monotonic evaluation clock after the confirmation await.</summary>
    [Theory]
    [InlineData("rollback")]
    [InlineData("expiry")]
    [InlineData("revocation")]
    public async Task ConfirmationCannotReviveExpiredRevokedOrRolledBackAuthority(string scenario)
    {
        var clock = new Story54TimeProvider();
        DateTimeOffset started = clock.Now;
        var source = Substitute.For<ICurrentApproverEvidenceSource>();
        source.ReadAsync("tenant-a", _source, null, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            clock.Now = started.AddSeconds(2);
            return new[] { Candidate(clock) with { Actor = Candidate(clock).Actor with { ObservedAt = started } } };
        });
        source.ConfirmAsync("tenant-a", Arg.Any<IReadOnlyList<CurrentApproverEvidence>>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            clock.Now = scenario switch
            {
                "rollback" => started.AddSeconds(1),
                "expiry" => started.AddMinutes(2),
                _ => clock.Now,
            };
            return scenario != "revocation";
        });

        ApproverPolicyResolutionResult result = await Resolver(source, clock).ResolveAsync("tenant-a", _policy, CancellationToken.None);

        AssertUnavailable(result);
        await source.Received(1).ConfirmAsync("tenant-a", Arg.Any<IReadOnlyList<CurrentApproverEvidence>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>A later source clock rollback invalidates every earlier accepted candidate.</summary>
    [Fact]
    public async Task LaterSourceClockRollbackInvalidatesTheEntirePolicy()
    {
        var clock = new Story54TimeProvider();
        DateTimeOffset started = clock.Now;
        ApproverPolicySource second = _source with { TenantRole = "Other" };
        var source = Substitute.For<ICurrentApproverEvidenceSource>();
        source.ReadAsync("tenant-a", _source, null, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            CurrentApproverEvidence candidate = Candidate(clock);
            clock.Now = started.AddSeconds(5);
            return new[] { candidate };
        });
        source.ReadAsync("tenant-a", second, null, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            clock.Now = started.AddSeconds(2);
            return new[] { Candidate(clock) with { Source = second } };
        });
        source.ConfirmAsync("tenant-a", Arg.Any<IReadOnlyList<CurrentApproverEvidence>>(), Arg.Any<CancellationToken>()).Returns(true);

        ApproverPolicyResolutionResult result = await Resolver(source, clock).ResolveAsync("tenant-a",
            new([_source, second], ApproverPolicyBasisDisclosure.OperatorOnly), CancellationToken.None);

        result.Sources.ShouldAllBe(item => item.Outcome == ApproverSourceOutcome.Unavailable
            && item.Candidates.Count == 0 && item.Evidence == null);
        await source.DidNotReceiveWithAnyArgs().ConfirmAsync(default!, default!, default);
    }

    /// <summary>Requires the exact configured source for both configuration and action verdicts.</summary>
    [Theory]
    [InlineData(ApproverPolicySourceKind.PredefinedParty, false)]
    [InlineData(ApproverPolicySourceKind.PredefinedParty, true)]
    [InlineData(ApproverPolicySourceKind.TenantRole, false)]
    [InlineData(ApproverPolicySourceKind.TenantRole, true)]
    public void KindOnlyAndSubstitutedSourcesCannotConfirmTheConfiguredBasis(ApproverPolicySourceKind kind, bool substituted)
    {
        var configured = new ApproverPolicySource(kind, kind == ApproverPolicySourceKind.PredefinedParty ? "party-a" : null,
            kind == ApproverPolicySourceKind.TenantRole ? "Approver" : null);
        var result = new ApproverPolicyResolutionResult([new(kind, ApproverSourceOutcome.Resolved)
        {
            Source = substituted ? configured with { PartyId = "party-b", TenantRole = "Other" } : null,
        }]);

        ApproverPolicyVerdict.Evaluate(new([configured], ApproverPolicyBasisDisclosure.OperatorOnly), result)
            .ShouldBe(ApproverPolicyValidationStatus.Unavailable);
    }

    /// <summary>Unknown serialized source kinds never become a valid policy through a resolved fixture.</summary>
    [Fact]
    public void UnknownSourceKindCannotBecomeValid()
    {
        var configured = new ApproverPolicySource((ApproverPolicySourceKind)999, null, null);
        var result = new ApproverPolicyResolutionResult([new(configured.Kind, ApproverSourceOutcome.Resolved) { Source = configured }]);

        ApproverPolicyVerdict.Evaluate(new([configured], ApproverPolicyBasisDisclosure.OperatorOnly), result)
            .ShouldBe(ApproverPolicyValidationStatus.Unavailable);
    }

    private static ProjectedApproverPolicyResolver Resolver(ICurrentApproverEvidenceSource source, Story54TimeProvider clock)
        => new(source, Substitute.For<IAgentDependencyAvailability>(), clock);

    private static CurrentApproverEvidence Candidate(Story54TimeProvider clock)
        => new(_source, new("tenant-a", "subject-a", "actor-a", "party-a", 1, 1, clock.Now, clock.Now.AddMinutes(1)),
            "conversation-a", true);

    private static void AssertUnavailable(ApproverPolicyResolutionResult result)
    {
        ApproverSourceResolution resolution = result.Sources.Single();
        resolution.Outcome.ShouldBe(ApproverSourceOutcome.Unavailable);
        resolution.Source.ShouldBe(_source);
        resolution.Candidates.ShouldBeEmpty();
        resolution.Evidence.ShouldBeNull();
    }
}
