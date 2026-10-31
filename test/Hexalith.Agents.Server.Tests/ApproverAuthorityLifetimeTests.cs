using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Retains locally confirmed approver lifetimes through safe basis and final command admission.</summary>
public sealed class ApproverAuthorityLifetimeTests
{
    private const string Tenant = "tenant-a";
    private const string Message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly ApproverPolicySource _source = new(ApproverPolicySourceKind.TenantRole, null, "Approver");
    private static readonly AgentApproverPolicy _policy = new([_source], ApproverPolicyBasisDisclosure.OperatorOnly);
    private readonly Story54TimeProvider _clock = new();

    /// <summary>Configuration and action verdicts cannot reuse evidence after its deadline or confirmed clock rollback.</summary>
    [Theory]
    [InlineData(false, 1, false)]
    [InlineData(false, 2, true)]
    [InlineData(false, 3, true)]
    [InlineData(false, 5, false)]
    [InlineData(true, 1, false)]
    [InlineData(true, 2, true)]
    [InlineData(true, 3, true)]
    [InlineData(true, 5, false)]
    public async Task ConfirmedApproverVerdictsRetainTheirExclusiveLifetime(bool action, int seconds, bool current)
    {
        DateTimeOffset started = _clock.Now;
        ProjectedApproverPolicyResolver resolver = Resolver(started);
        ApproverPolicyResolutionResult result = action
            ? await resolver.ResolveForActorAsync(Tenant, _policy, new("approver-actor", "approver-party", "conversation"), CancellationToken.None)
            : await resolver.ResolveAsync(Tenant, _policy, CancellationToken.None);
        _clock.Now = started.AddSeconds(seconds);

        ApproverPolicyValidationStatus verdict = action
            ? ApproverPolicyVerdict.EvaluateAction(_policy, result)
            : ApproverPolicyVerdict.Evaluate(_policy, result);

        verdict.ShouldBe(current ? ApproverPolicyValidationStatus.Valid : ApproverPolicyValidationStatus.Unavailable);
        if (!current)
        {
            _clock.Now = started.AddSeconds(3);
            (action ? ApproverPolicyVerdict.EvaluateAction(_policy, result) : ApproverPolicyVerdict.Evaluate(_policy, result))
                .ShouldBe(ApproverPolicyValidationStatus.Unavailable);
        }
    }

    /// <summary>The shared API/query basis withholds a Valid flag when the confirmed approver interval has ended.</summary>
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(5, false)]
    public async Task SetupBasisChecksTheConfirmedApproverLifetime(int seconds, bool current)
    {
        DateTimeOffset started = _clock.Now;
        ApproverPolicyResolutionResult resolution = await Resolver(started).ResolveAsync(Tenant, _policy, CancellationToken.None);
        var resolver = Substitute.For<IApproverPolicyResolver>();
        resolver.ResolveAsync(Tenant, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _clock.Now = started.AddSeconds(seconds);
            return resolution;
        });
        var model = new AgentSetupReadModel
        {
            IsCreated = true, TenantId = Tenant, AgentId = "hexa", DisplayName = "hexa", ConfigurationVersion = 1,
            ResponseMode = AgentResponseMode.Confirmation, ApproverPolicySources = _policy.Sources,
            ApproverPolicyDisclosure = _policy.DisclosureCategory,
        };

        AgentSetupResult result = await AgentSetupBasisEnricher.EnrichAsync(
            AgentSetupViewFactory.Create(model, Tenant, "hexa", null, true), model, resolver, CancellationToken.None);

        result.Setup.ShouldNotBeNull().Agent.ApproverPolicyBasis.Single().Status
            .ShouldBe(current ? ApproverPolicyValidationStatus.Valid : ApproverPolicyValidationStatus.Unavailable);
    }

    /// <summary>A shorter approver deadline must survive metadata preparation and the host admission await.</summary>
    [Theory]
    [InlineData(false, false, 3, true)]
    [InlineData(false, false, 5, false)]
    [InlineData(false, true, 3, true)]
    [InlineData(false, true, 5, false)]
    [InlineData(true, false, 3, true)]
    [InlineData(true, false, 5, false)]
    [InlineData(true, true, 3, true)]
    [InlineData(true, true, 5, false)]
    public async Task ActivationRetainsApproverDeadlineBeforeTargetConstruction(bool duringAdmission, bool administratorBound, int seconds, bool current)
    {
        DateTimeOffset started = _clock.Now;
        var gateway = Gateway();
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        admission.AdmitAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (duringAdmission)
            {
                _clock.Now = started.AddSeconds(seconds);
            }
            return call.Arg<AgentCommandIntent>();
        });
        var extensions = Substitute.For<IReadOnlyDictionary<string, string>>();
        extensions.GetEnumerator().Returns(_ =>
        {
            if (!duringAdmission)
            {
                _clock.Now = started.AddSeconds(seconds);
            }
            return new Dictionary<string, string>().GetEnumerator();
        });
        var request = new AgentActivationRevalidationRequest(Message, Correlation, Tenant, "hexa", "administrator", true, 1,
            null, null, AgentResponseMode.Confirmation, _policy, extensions)
        {
            Authority = administratorBound ? new(_clock, started, started.AddMinutes(1)) : null,
        };
        var orchestrator = new AgentActivationProviderRevalidation(Substitute.For<IProviderCatalogReader>(), Resolver(started),
            new EventStoreAgentCommandDispatcher(gateway, admission));

        if (current)
        {
            (await orchestrator.ExecuteAsync(request, CancellationToken.None)).Dispatched.ShouldBeTrue();
            await gateway.Received(1).SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>());
        }
        else
        {
            await Should.ThrowAsync<AgentCommandAuthorityExpiredException>(() => orchestrator.ExecuteAsync(request, CancellationToken.None));
            gateway.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>Composition preserves an earlier administrator deadline and checked clock without retracting an accepted write.</summary>
    [Theory]
    [InlineData(3, 2, true)]
    [InlineData(3, 3, false)]
    [InlineData(60, 1, false)]
    public async Task ActivationRetainsAdministratorDeadlineAndAdmissionClock(int administratorSeconds, int admissionSeconds, bool current)
    {
        DateTimeOffset started = _clock.Now;
        var gateway = Gateway();
        gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _clock.Now = started.AddMinutes(2);
            return new SubmitCommandResponse(CorrelationId: Correlation, MessageId: Message);
        });
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        admission.AdmitAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _clock.Now = started.AddSeconds(admissionSeconds);
            return call.Arg<AgentCommandIntent>();
        });
        var request = new AgentActivationRevalidationRequest(Message, Correlation, Tenant, "hexa", "administrator", true, 1,
            null, null, AgentResponseMode.Confirmation, _policy)
        {
            Authority = new(_clock, started, started.AddSeconds(administratorSeconds)),
        };
        var orchestrator = new AgentActivationProviderRevalidation(Substitute.For<IProviderCatalogReader>(), Resolver(started),
            new EventStoreAgentCommandDispatcher(gateway, admission));

        if (current)
        {
            (await orchestrator.ExecuteAsync(request, CancellationToken.None)).Dispatched.ShouldBeTrue();
            await gateway.Received(1).SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>());
        }
        else
        {
            await Should.ThrowAsync<AgentCommandAuthorityExpiredException>(() => orchestrator.ExecuteAsync(request, CancellationToken.None));
            gateway.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>The interaction gate retains the approver deadline alongside its longer caller/tenant interval.</summary>
    [Theory]
    [InlineData(false, 3, true)]
    [InlineData(false, 5, false)]
    [InlineData(true, 3, true)]
    [InlineData(true, 5, false)]
    public async Task GateRetainsApproverDeadlineThroughPreparationAndAdmission(bool duringAdmission, int seconds, bool current)
    {
        DateTimeOffset started = _clock.Now;
        var tenant = Substitute.For<ITenantAccessReader>();
        var actor = new CurrentHumanActorEvidence(Tenant, "subject", "caller-actor", "caller", 1, 1, started, started.AddMinutes(1));
        tenant.ReadAsync(Tenant, "subject", "caller", Arg.Any<CancellationToken>()).Returns(
            new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, true) { CurrentActor = actor, Authority = new(_clock, started, actor.ValidUntil) });
        var parties = Substitute.For<IAgentPartyDirectory>();
        parties.ValidateCallerPartyAsync(Tenant, "caller", actor, Arg.Any<CancellationToken>()).Returns(
            new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "caller") { CallerValidUntil = actor.ValidUntil });
        parties.ValidateExistingPartyAsync(Tenant, "agent-party", Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "agent-party"));
        var conversations = Substitute.For<IConversationAccessReader>();
        conversations.ReadAsync(Tenant, "conversation", "caller", Arg.Any<CancellationToken>())
            .Returns(new ConversationAccessReadResult(AgentInteractionGateOutcome.Satisfied, true));
        var readiness = Substitute.For<IAgentInvocationReadinessReader>();
        readiness.ReadAsync(Tenant, "hexa", Arg.Any<CancellationToken>()).Returns(new AgentInvocationReadiness(
            true, AgentLifecycleStatus.Active, true, "agent-party", AgentResponseMode.Confirmation, true, "provider", "model", _policy, true, true));
        var providers = Substitute.For<IProviderCatalogReader>();
        providers.GetEntryAsync(Tenant, "provider", "model", Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null));
        var gateway = Gateway();
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        admission.AdmitAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (duringAdmission)
            {
                _clock.Now = started.AddSeconds(seconds);
            }
            return call.Arg<AgentCommandIntent>();
        });
        var extensions = Substitute.For<IReadOnlyDictionary<string, string>>();
        extensions.GetEnumerator().Returns(_ =>
        {
            if (!duringAdmission)
            {
                _clock.Now = started.AddSeconds(seconds);
            }
            return new Dictionary<string, string>().GetEnumerator();
        });
        var orchestrator = new AgentInteractionGateOrchestrator(tenant, conversations, readiness, parties, providers, Resolver(started),
            new EventStoreAgentCommandDispatcher(gateway, admission), _clock);

        AgentInteractionGateOutcomeResult result = await orchestrator.ExecuteAsync(new(Message, Correlation, Tenant, "interaction", "hexa",
            "subject", "caller", "conversation", "provider", "model", AgentResponseMode.Confirmation,
            ClientSuppliedExtensions: extensions), CancellationToken.None);

        result.Status.ShouldBe(current ? AgentInteractionStatus.Blocked : AgentInteractionStatus.Denied);
        if (current)
        {
            await gateway.Received(1).SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>());
        }
        else
        {
            gateway.ReceivedCalls().ShouldBeEmpty();
        }
    }

    private ProjectedApproverPolicyResolver Resolver(DateTimeOffset started)
    {
        var source = Substitute.For<ICurrentApproverEvidenceSource>();
        source.ReadAsync(Tenant, _source, Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(call => new[]
        {
            new CurrentApproverEvidence(_source, new(Tenant, "approver-subject", "approver-actor", "approver-party", 1, 1,
                _clock.Now, started.AddSeconds(5)), call.ArgAt<string?>(2), true),
            new CurrentApproverEvidence(_source, new(Tenant, "other-subject", "other-actor", "other-party", 1, 1,
                _clock.Now, started.AddSeconds(15)), call.ArgAt<string?>(2), true),
        });
        source.ConfirmAsync(Tenant, Arg.Any<IReadOnlyList<CurrentApproverEvidence>>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _clock.Now = started.AddSeconds(2);
            return true;
        });
        return new(source, Substitute.For<IAgentDependencyAvailability>(), _clock);
    }

    private static IEventStoreGatewayClient Gateway()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse(CorrelationId: Correlation, MessageId: Message));
        return gateway;
    }
}
