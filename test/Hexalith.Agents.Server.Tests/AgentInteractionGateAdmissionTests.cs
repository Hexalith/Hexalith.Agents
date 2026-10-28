using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;
#if HEXALITH_PARTIES_SOURCE
using Hexalith.Parties.Client.Abstractions;
using Hexalith.Parties.Contracts.Models;
using Hexalith.Parties.Contracts.Queries;
using Hexalith.Parties.Contracts.ValueObjects;
#endif

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Local admission ordering; substitutes establish no live dependency availability.</summary>
public sealed class AgentInteractionGateAdmissionTests
{
    private readonly ITenantAccessReader _tenants = Substitute.For<ITenantAccessReader>();
    private readonly IAgentPartyDirectory _parties = Substitute.For<IAgentPartyDirectory>();
    private readonly IConversationAccessReader _conversations = Substitute.For<IConversationAccessReader>();
    private readonly IAgentInvocationReadinessReader _readiness = Substitute.For<IAgentInvocationReadinessReader>();
    private readonly IProviderCatalogReader _providers = Substitute.For<IProviderCatalogReader>();
    private readonly IApproverPolicyResolver _approvers = Substitute.For<IApproverPolicyResolver>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();
    private readonly List<string> _reads = [];
    private readonly Story54TimeProvider _clock = new();

    /// <summary>A bare tenant success is insufficient to address the caller's Party or any protected resource.</summary>
    [Fact]
    public async Task TenantSuccessWithoutCurrentActorStopsBeforePartyLookup()
    {
        Configure();
        _tenants.ReadAsync("tenant-a", "subject", "caller", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _reads.Add("Tenant");
                return new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, true);
            });

        (await Orchestrator().ExecuteAsync(Request(), CancellationToken.None)).Status.ShouldBe(AgentInteractionStatus.Denied);

        _reads.ShouldBe(["Tenant"]);
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Substituted, incomplete or noncurrent tenant-result evidence is rejected without protected reads.</summary>
    [Theory]
    [InlineData("tenant")]
    [InlineData("subject")]
    [InlineData("party")]
    [InlineData("actor")]
    [InlineData("version")]
    [InlineData("position")]
    [InlineData("future")]
    [InlineData("old")]
    [InlineData("expiry")]
    [InlineData("observation-offset")]
    [InlineData("expiry-offset")]
    public async Task InvalidTenantActorStopsBeforePartyLookup(string corruption)
    {
        Configure();
        CurrentHumanActorEvidence actor = Actor();
        actor = corruption switch
        {
            "tenant" => actor with { TenantId = "foreign-tenant" },
            "subject" => actor with { Subject = "foreign-subject" },
            "party" => actor with { PartyId = "foreign-party" },
            "actor" => actor with { ActorId = " " },
            "version" => actor with { BindingVersion = 0 },
            "position" => actor with { SourcePosition = 0 },
            "future" => actor with { ObservedAt = _clock.Now.AddTicks(1) },
            "old" => actor with { ObservedAt = _clock.Now.AddTicks(-1) },
            "expiry" => actor with { ValidUntil = _clock.Now },
            "observation-offset" => actor with { ObservedAt = _clock.Now.ToOffset(TimeSpan.FromHours(1)) },
            "expiry-offset" => actor with { ValidUntil = actor.ValidUntil.ToOffset(TimeSpan.FromHours(1)) },
            _ => throw new ArgumentOutOfRangeException(nameof(corruption)),
        };
        _tenants.ReadAsync("tenant-a", "subject", "caller", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _reads.Add("Tenant");
            return new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, true) { CurrentActor = actor };
        });

        var result = await Orchestrator().ExecuteAsync(Request(), CancellationToken.None);

        result.ShouldBe(new AgentInteractionGateOutcomeResult("interaction", AgentInteractionStatus.Denied));
        _reads.ShouldBe(["Tenant"]);
        _parties.ReceivedCalls().ShouldBeEmpty();
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
        System.Text.Json.JsonSerializer.Serialize(result).ShouldNotContain("foreign");
    }

    /// <summary>Expiry and backward clock movement during any await prevent subsequent protected work.</summary>
    [Theory]
    [InlineData("Tenant", false)]
    [InlineData("Caller", false)]
    [InlineData("Conversation", false)]
    [InlineData("Readiness", false)]
    [InlineData("AgentParty", false)]
    [InlineData("Provider", false)]
    [InlineData("Approver", false)]
    [InlineData("Tenant", true)]
    [InlineData("Caller", true)]
    [InlineData("Conversation", true)]
    [InlineData("Readiness", true)]
    [InlineData("AgentParty", true)]
    [InlineData("Provider", true)]
    [InlineData("Approver", true)]
    public async Task AuthorityIsRecheckedAcrossEveryAwait(string changedRead, bool rollback)
    {
        Configure(completed: read =>
        {
            if (read == changedRead)
            {
                _clock.Now = _clock.Now.AddTicks(rollback ? -1 : TimeSpan.TicksPerMinute);
            }
        });

        (await Orchestrator().ExecuteAsync(Request(), CancellationToken.None)).Status.ShouldBe(AgentInteractionStatus.Denied);

        string[] order = ["Tenant", "Caller", "Conversation", "Readiness", "AgentParty", "Provider", "Approver"];
        _reads.ShouldBe(order.Take(Array.IndexOf(order, changedRead) + 1));
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A rollback between reads also denies when the clock remains after the original interval start.</summary>
    [Fact]
    public async Task ClockRollbackAfterForwardProgressDeniesBeforeReadiness()
    {
        Configure(completed: read =>
        {
            if (read == "Caller")
            {
                _clock.Now = _clock.Now.AddSeconds(1);
            }
            if (read == "Conversation")
            {
                _clock.Now = _clock.Now.AddTicks(-1);
            }
        });

        (await Orchestrator().ExecuteAsync(Request(), CancellationToken.None)).Status.ShouldBe(AgentInteractionStatus.Denied);

        _reads.ShouldBe(["Tenant", "Caller", "Conversation"]);
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>The exact tenant-authorized actor reaches the new directory seam without a legacy fallback.</summary>
    [Fact]
    public async Task CurrentActorIsPassedToTheActorBearingCallerOverload()
    {
        Configure();

        await Orchestrator().ExecuteAsync(Request(), CancellationToken.None);

        await _parties.Received(1).ValidateCallerPartyAsync("tenant-a", "caller", Actor(), Arg.Any<CancellationToken>());
        await _parties.DidNotReceive().ValidateCallerPartyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Missing, revoked, uncertain and malformed caller evidence stops before Conversation and Agent reads.</summary>
    [Theory]
    [InlineData(PartyLinkValidationStatus.Unknown)]
    [InlineData(PartyLinkValidationStatus.Missing)]
    [InlineData(PartyLinkValidationStatus.Disabled)]
    [InlineData(PartyLinkValidationStatus.Ambiguous)]
    [InlineData(PartyLinkValidationStatus.Unavailable)]
    [InlineData(PartyLinkValidationStatus.Unauthorized)]
    [InlineData((PartyLinkValidationStatus)999)]
    public async Task CallerDenialStopsBeforeProtectedReadsOrDispatch(PartyLinkValidationStatus status)
    {
        Configure();
        _parties.ValidateCallerPartyAsync("tenant-a", "caller", Arg.Any<CurrentHumanActorEvidence>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _reads.Add("Caller");
                return new AgentPartyValidationResult(status, null);
            });

        AgentInteractionGateOutcomeResult result = await Orchestrator().ExecuteAsync(Request(), CancellationToken.None);

        result.Status.ShouldBe(AgentInteractionStatus.Denied);
        _reads.ShouldBe(["Tenant", "Caller"]);
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A Valid flag cannot substitute the exact caller Party or its current UTC deadline.</summary>
    [Theory]
    [InlineData("missing-party")]
    [InlineData("foreign-party")]
    [InlineData("missing-deadline")]
    [InlineData("expired-deadline")]
    [InlineData("offset-deadline")]
    public async Task MalformedSuccessfulCallerResultStopsBeforeConversationRead(string corruption)
    {
        Configure();
        DateTimeOffset? deadline = corruption switch
        {
            "missing-deadline" => null,
            "expired-deadline" => _clock.Now,
            "offset-deadline" => _clock.Now.AddSeconds(5).ToOffset(TimeSpan.FromHours(1)),
            _ => _clock.Now.AddSeconds(5),
        };
        string? partyId = corruption switch
        {
            "missing-party" => null,
            "foreign-party" => "foreign-party",
            _ => "caller",
        };
        _parties.ValidateCallerPartyAsync("tenant-a", "caller", Arg.Any<CurrentHumanActorEvidence>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _reads.Add("Caller");
                return new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, partyId) { CallerValidUntil = deadline };
            });

        AgentInteractionGateOutcomeResult result = await Orchestrator().ExecuteAsync(Request(), CancellationToken.None);

        result.ShouldBe(new AgentInteractionGateOutcomeResult("interaction", AgentInteractionStatus.Denied));
        _reads.ShouldBe(["Tenant", "Caller"]);
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
        System.Text.Json.JsonSerializer.Serialize(result).ShouldNotContain("foreign");
    }

    /// <summary>A shorter caller-binding or custody deadline stays exclusive through every later protected read.</summary>
    [Theory]
    [InlineData("Conversation")]
    [InlineData("Readiness")]
    [InlineData("AgentParty")]
    [InlineData("Provider")]
    [InlineData("Approver")]
    public async Task EarlierCallerDeadlineStopsBeforeSubsequentProtectedReads(string changedRead)
    {
        DateTimeOffset deadline = _clock.Now.AddSeconds(5);
        Configure(completed: read =>
        {
            if (read == changedRead)
            {
                _clock.Now = deadline;
            }
        });
        _parties.ValidateCallerPartyAsync("tenant-a", "caller", Arg.Any<CurrentHumanActorEvidence>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _reads.Add("Caller");
                return new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "caller") { CallerValidUntil = deadline };
            });

        (await Orchestrator().ExecuteAsync(Request(), CancellationToken.None)).Status.ShouldBe(AgentInteractionStatus.Denied);

        string[] order = ["Tenant", "Caller", "Conversation", "Readiness", "AgentParty", "Provider", "Approver"];
        _reads.ShouldBe(order.Take(Array.IndexOf(order, changedRead) + 1));
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

#if HEXALITH_PARTIES_SOURCE
    /// <summary>Real local reader, directory and gate compose for distinct subject/actor identities and all earlier deadlines.</summary>
    [Theory]
    [InlineData("current")]
    [InlineData("tenant")]
    [InlineData("binding")]
    [InlineData("custody")]
    public async Task RealReaderDirectoryAndGateComposeWithoutLosingAuthorityDeadlines(string earliest)
    {
        DateTimeOffset deadline = _clock.Now.AddSeconds(5);
        CurrentHumanActorEvidence actor = Story54PartyIdentityFixture.Actor(_clock) with { Subject = "subject", PartyId = "caller" };
        HumanActorBindingEvidence binding = Story54PartyIdentityFixture.Binding(_clock) with { PartyId = "caller" };
        if (earliest == "binding")
        {
            binding = binding with { ValidUntil = deadline };
        }
        if (earliest == "custody")
        {
            binding = binding with { ValidFrom = deadline.AddDays(-365), ValidUntil = deadline, Custody = binding.Custody with { ExpiresAt = deadline } };
        }
        var actors = Substitute.For<ICurrentHumanActorSource>();
        actors.ReadAsync("tenant-a", "subject", Arg.Any<CancellationToken>()).Returns(actor);
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        tenants.ReadAsync("tenant-a", false, Arg.Any<CancellationToken>()).Returns(new TenantAuthorityEvidence(
            "tenant-a", "tenants", 2, _clock.Now, earliest == "tenant" ? deadline : actor.ValidUntil,
            [new(1, string.Empty, null, "TenantActive", true), new(2, actor.ActorId, "caller", "User", true)],
            SourceAggregateId: "tenant-a"));
        var availability = Substitute.For<IAgentDependencyAvailability>();
        var client = Substitute.For<IPartiesIdentityClient>();
        client.ResolvePartyIdentityAsync("tenant-a", Arg.Any<ResolvePartyIdentity>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            ResolvePartyIdentity query = call.ArgAt<ResolvePartyIdentity>(1);
            return new PartyIdentityResult(PartyIdentityOutcome.Resolved, query.PartyId == "caller"
                ? Story54PartyIdentityFixture.Identity(_clock) with { PartyId = "caller", HumanBinding = binding }
                : new PartyIdentityEvidence(1, "tenant-a", "agent-party", PartyIdentityClassification.Organization,
                    true, false, false, 11, _clock.Now, "organization-observation", null));
        });
        _conversations.ReadAsync("tenant-a", "conversation", "caller", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (earliest != "current")
            {
                _clock.Now = deadline;
            }
            return new ConversationAccessReadResult(AgentInteractionGateOutcome.Satisfied, true);
        });
        _readiness.ReadAsync("tenant-a", "hexa", Arg.Any<CancellationToken>()).Returns(new AgentInvocationReadiness(
            true, AgentLifecycleStatus.Active, true, "agent-party", AgentResponseMode.Automatic, true, "provider", "model", null, true, true));
        _providers.GetEntryAsync("tenant-a", "provider", "model", Arg.Any<CancellationToken>()).Returns(
            new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success,
                new ProviderCatalogEntryView("provider", "model", "local model", ProviderModelStatus.Enabled,
                    true, 128_000, 16_000, new ProviderModelTimeoutPolicy(30_000, 3), ProviderModelCapabilityFlags.Streaming,
                    ProviderConfigurationState.Configured, "configuration-reference", true, 1,
                    new ProviderModelPricing("USD", 0.002m, 0.008m, 1))));
        var orchestrator = new AgentInteractionGateOrchestrator(new ProjectedTenantAccessReader(tenants, availability, _clock, actors),
            _conversations, _readiness, new PartiesAgentPartyDirectory(client, availability, _clock),
            _providers, _approvers, _dispatcher, _clock);

        AgentInteractionGateOutcomeResult result = await orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        actor.Subject.ShouldNotBe(actor.ActorId);
        await client.Received(1).ResolvePartyIdentityAsync("tenant-a", new ResolvePartyIdentity("tenant-a", "caller", actor.ActorId), Arg.Any<CancellationToken>());
        if (earliest == "current")
        {
            result.Status.ShouldBe(AgentInteractionStatus.Authorized);
            await client.Received(1).ResolvePartyIdentityAsync("tenant-a", new ResolvePartyIdentity("tenant-a", "agent-party"), Arg.Any<CancellationToken>());
            await _dispatcher.Received(1).DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>());
        }
        else
        {
            result.ShouldBe(new AgentInteractionGateOutcomeResult("interaction", AgentInteractionStatus.Denied));
            _readiness.ReceivedCalls().ShouldBeEmpty();
            _providers.ReceivedCalls().ShouldBeEmpty();
            _dispatcher.ReceivedCalls().ShouldBeEmpty();
            client.ReceivedCalls().Count().ShouldBe(1);
        }
    }
#endif

    /// <summary>Conversation denial or uncertainty stops before Agent, Provider and approver reads.</summary>
    [Theory]
    [InlineData(AgentInteractionGateOutcome.Unknown, true)]
    [InlineData(AgentInteractionGateOutcome.Missing, true)]
    [InlineData(AgentInteractionGateOutcome.Disabled, true)]
    [InlineData(AgentInteractionGateOutcome.Stale, true)]
    [InlineData(AgentInteractionGateOutcome.Unavailable, true)]
    [InlineData(AgentInteractionGateOutcome.Unauthorized, true)]
    [InlineData((AgentInteractionGateOutcome)999, true)]
    [InlineData(AgentInteractionGateOutcome.Satisfied, false)]
    public async Task ConversationDenialStopsBeforeProtectedReadsOrDispatch(AgentInteractionGateOutcome outcome, bool fresh)
    {
        Configure();
        _conversations.ReadAsync("tenant-a", "conversation", "caller", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _reads.Add("Conversation");
                return new ConversationAccessReadResult(outcome, fresh);
            });

        AgentInteractionGateOutcomeResult result = await Orchestrator().ExecuteAsync(Request(), CancellationToken.None);

        result.Status.ShouldBe(AgentInteractionStatus.Denied);
        _reads.ShouldBe(["Tenant", "Caller", "Conversation"]);
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A token cancelled before admission performs no dependency read.</summary>
    [Fact]
    public async Task CancellationBeforeAdmissionPerformsNoReadOrDispatch()
    {
        Configure();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => Orchestrator().ExecuteAsync(Request(), cancellation.Token));

        _reads.ShouldBeEmpty();
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A cancelled completed read or failed read performs no later read or command construction.</summary>
    [Theory]
    [InlineData("Tenant", false)]
    [InlineData("Caller", false)]
    [InlineData("Conversation", false)]
    [InlineData("Readiness", false)]
    [InlineData("AgentParty", false)]
    [InlineData("Provider", false)]
    [InlineData("Approver", false)]
    [InlineData("Tenant", true)]
    [InlineData("Caller", true)]
    [InlineData("Conversation", true)]
    [InlineData("Readiness", true)]
    [InlineData("AgentParty", true)]
    [InlineData("Provider", true)]
    [InlineData("Approver", true)]
    public async Task CancellationDuringDependencyReadStopsBeforeFurtherWork(string cancelledRead, bool throws)
    {
        using var cancellation = new CancellationTokenSource();
        Configure(cancelledRead, cancellation, throws);

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(
            () => Orchestrator().ExecuteAsync(Request(), cancellation.Token));

        exception.CancellationToken.ShouldBe(cancellation.Token);
        string[] order = ["Tenant", "Caller", "Conversation", "Readiness", "AgentParty", "Provider", "Approver"];
        _reads.ShouldBe(order.Take(Array.IndexOf(order, cancelledRead) + 1));
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    private AgentInteractionGateOrchestrator Orchestrator()
        => new(_tenants, _conversations, _readiness, _parties, _providers, _approvers, _dispatcher, _clock);

    private void Configure(string? cancelledRead = null, CancellationTokenSource? cancellation = null, bool throws = false, Action<string>? completed = null)
    {
        CurrentHumanActorEvidence actor = Actor();
        var source = new ApproverPolicySource(ApproverPolicySourceKind.TenantRole, null, "Approver");
        var policy = new AgentApproverPolicy([source], ApproverPolicyBasisDisclosure.OperatorOnly);
        _tenants.ReadAsync("tenant-a", "subject", "caller", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Tenant");
            return new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, true) { CurrentActor = actor };
        });
        _parties.ValidateCallerPartyAsync("tenant-a", "caller", Arg.Any<CurrentHumanActorEvidence>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Caller");
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "caller") { CallerValidUntil = actor.ValidUntil };
        });
        _parties.ValidateExistingPartyAsync("tenant-a", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            Complete("AgentParty");
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, call.ArgAt<string>(1));
        });
        _conversations.ReadAsync("tenant-a", "conversation", "caller", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Conversation");
            return new ConversationAccessReadResult(AgentInteractionGateOutcome.Satisfied, true);
        });
        _readiness.ReadAsync("tenant-a", "hexa", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Readiness");
            return new AgentInvocationReadiness(true, AgentLifecycleStatus.Active, true, "agent-party",
                AgentResponseMode.Confirmation, true, "provider", "model", policy, true, true);
        });
        _providers.GetEntryAsync("tenant-a", "provider", "model", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Provider");
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null);
        });
        _approvers.ResolveAsync("tenant-a", policy, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Approver");
            return new ApproverPolicyResolutionResult([new(source.Kind, ApproverSourceOutcome.Resolved) { Source = source }]);
        });

        void Complete(string read)
        {
            _reads.Add(read);
            completed?.Invoke(read);
            if (read == cancelledRead)
            {
                cancellation!.Cancel();
                if (throws)
                {
                    throw new InvalidOperationException("Unavailable source after cancellation.");
                }
            }
        }
    }

    private CurrentHumanActorEvidence Actor()
        => new("tenant-a", "subject", "stable-actor", "caller", 7, 11, _clock.Now, _clock.Now.AddMinutes(1));

    private static AgentInteractionGateRequest Request()
        => new("message", "correlation", "tenant-a", "interaction", "hexa", "subject", "caller", "conversation",
            "provider", "model", AgentResponseMode.Confirmation, null, null);
}
