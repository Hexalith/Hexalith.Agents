using System.Security.Claims;
using System.Text.Json;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Queries;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Proves retained current authority bounds protected setup reads and final command admission locally.</summary>
public sealed class SetupAuthorityLifetimeTests
{
    private const string TenantId = "tenant-a";
    private const string AgentId = "hexa";
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private readonly Story54TimeProvider _clock = new();
    private readonly IReadModelStore _store = Substitute.For<IReadModelStore>();
    private readonly IApproverPolicyResolver _approvers = Substitute.For<IApproverPolicyResolver>();
    private readonly IProviderCatalogReader _catalog = Substitute.For<IProviderCatalogReader>();
    private readonly ICurrentHumanActorSource _actors = Substitute.For<ICurrentHumanActorSource>();
    private readonly ICurrentTenantAuthoritySource _tenants = Substitute.For<ICurrentTenantAuthoritySource>();
    private readonly IAgentDependencyAvailability _availability = Substitute.For<IAgentDependencyAvailability>();

    /// <summary>Gets matching setup API/query entry points and protected-read completion boundaries.</summary>
    public static TheoryData<bool, bool, string> ReadCases
    {
        get
        {
            var cases = new TheoryData<bool, bool, string>();
            foreach (bool direct in new[] { false, true })
            foreach (bool status in new[] { false, true })
            foreach (string boundary in new[] { "store-expiry", "store-rollback", "store-expiry-fault", "store-rollback-fault", "approver-expiry", "approver-rollback", "current" })
            {
                cases.Add(direct, status, boundary);
            }
            return cases;
        }
    }

    /// <summary>Both API/query setup reads withhold protected output after expiry or intermediate clock rollback.</summary>
    [Theory]
    [MemberData(nameof(ReadCases))]
    public async Task SetupReadsRetainAuthorityThroughStoreAndApproverAwaits(bool direct, bool status, string boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        DateTimeOffset started = _clock.Now;
        ConfigureAuthority();
        ApproverPolicySource configured = new(ApproverPolicySourceKind.PredefinedParty, "approver-party", null);
        _store.GetAsync<AgentSetupReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _clock.Now = boundary switch
            {
                "store-expiry" or "store-expiry-fault" => started.AddSeconds(5),
                "store-rollback" or "store-rollback-fault" => started.AddTicks(-1),
                _ => started.AddSeconds(2),
            };
            if (boundary.EndsWith("-fault", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("private-store-failure");
            }
            return new ReadModelEntry<AgentSetupReadModel>(Model(configured), "etag");
        });
        _approvers.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _clock.Now = boundary switch
            {
                "approver-expiry" => started.AddSeconds(5),
                "approver-rollback" => started.AddSeconds(1),
                _ => started.AddSeconds(3),
            };
            return new ApproverPolicyResolutionResult([new(configured.Kind, ApproverSourceOutcome.Resolved) { Source = configured }]);
        });

        AgentSetupResult result;
        if (direct)
        {
            EventStoreAgentAdministrationOperations operations = Operations(Substitute.For<IAgentCommandDispatcher>());
            result = (status ? await operations.GetStatusAsync(AgentId) : await operations.GetConfigurationAsync(AgentId)).Value.ShouldNotBeNull();
        }
        else
        {
            var options = Options.Create(new AgentSetupReadModelOptions());
            var tenantReader = new ProjectedTenantAccessReader(_tenants, _availability, _clock, _actors);
            AgentSetupQueryHandlerBase handler = status
                ? new GetAgentStatusQueryHandler(_store, options, tenantReader, _approvers, ContextProvider())
                : new GetAgentConfigurationQueryHandler(_store, options, tenantReader, _approvers, ContextProvider());
            QueryResult response = await handler.ExecuteAsync(new QueryEnvelope(TenantId, "agent", AgentId,
                handler.QueryType, [], CorrelationId, "subject"), CancellationToken.None);
            result = JsonSerializer.Deserialize<AgentSetupResult>(response.PayloadBytes.ShouldNotBeNull(),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
                }).ShouldNotBeNull();
        }

        result.Status.ShouldBe(boundary == "current" ? AgentInspectionStatus.Success : AgentInspectionStatus.NotAuthorized);
        if (boundary == "current")
        {
            result.Setup.ShouldNotBeNull().Agent.DisplayName.ShouldBe("protected-setup-name");
        }
        else
        {
            result.ShouldBe(AgentSetupResult.NotAuthorized());
            JsonSerializer.Serialize(result).ShouldNotContain("protected-");
        }
        if (boundary.StartsWith("store-", StringComparison.Ordinal))
        {
            _approvers.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>Current actor and role evidence both constrain the retained lifetime for either principal.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task EarliestActorOrRoleDeadlineIsExclusiveAndCannotReopen(bool platform, bool actorFirst)
    {
        DateTimeOffset started = _clock.Now;
        ConfigureAuthority(actorFirst ? 5 : 10, actorFirst ? 10 : 5);
        AgentAdministrationContext context = await ContextProvider().GetContextAsync(platform, CancellationToken.None);
        context.Authority.ShouldNotBeNull().IsCurrent.ShouldBeTrue();
        _clock.Now = started.AddSeconds(5);
        context.HasCurrentAuthority.ShouldBeFalse();
        _clock.Now = started.AddSeconds(4);
        context.HasCurrentAuthority.ShouldBeFalse();
    }

    /// <summary>All public setup writes retain authority through final admission and preserve completed outcome truth.</summary>
    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("response-mode")]
    [InlineData("disable")]
    [InlineData("activate")]
    public async Task SetupWritesRetainTheirBoundThroughFinalAdmission(string operation)
    {
        DateTimeOffset started = _clock.Now;
        ConfigureAuthority();
        _store.GetAsync<AgentSetupReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<AgentSetupReadModel>(Model(), "etag"));
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        admission.AdmitAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            AgentCommandIntent intent = call.Arg<AgentCommandIntent>();
            intent.Authority.ShouldNotBeNull();
            _clock.Now = started.AddSeconds(5);
            return intent;
        });
        EventStoreAgentAdministrationOperations operations = Operations(new EventStoreAgentCommandDispatcher(gateway, admission));

        AgentOperationResult<AgentCommandAcceptance> result = await WriteAsync(operations, operation);

        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.NotAuthorized);
        gateway.ReceivedCalls().ShouldBeEmpty();
        await admission.Received(1).AdmitAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Expiry at any activation protected read stops subsequent reads and intent construction.</summary>
    [Theory]
    [InlineData("store")]
    [InlineData("catalog")]
    [InlineData("approver")]
    public async Task ActivationDoesNotContinueAfterItsAuthorityExpires(string boundary)
    {
        DateTimeOffset started = _clock.Now;
        ConfigureAuthority();
        ApproverPolicySource configured = new(ApproverPolicySourceKind.PredefinedParty, "approver-party", null);
        _store.GetAsync<AgentSetupReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (boundary == "store")
            {
                _clock.Now = started.AddSeconds(5);
            }
            AgentSetupReadModel model = Model(configured);
            model.ProviderId = "provider";
            model.ModelId = "model";
            return new ReadModelEntry<AgentSetupReadModel>(model, "etag");
        });
        _catalog.GetEntryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (boundary == "catalog")
            {
                _clock.Now = started.AddSeconds(5);
            }
            return new ProviderCatalogEntryReadResult(Hexalith.Agents.Contracts.ProviderCatalog.ProviderCatalogInspectionStatus.Unavailable, null);
        });
        _approvers.ResolveAsync(Arg.Any<string>(), Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (boundary == "approver")
            {
                _clock.Now = started.AddSeconds(5);
            }
            return new ApproverPolicyResolutionResult([]);
        });
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();

        AgentOperationResult<AgentCommandAcceptance> result = await WriteAsync(Operations(dispatcher), "activate");

        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.NotAuthorized);
        dispatcher.ReceivedCalls().ShouldBeEmpty();
        if (boundary == "store")
        {
            _catalog.ReceivedCalls().ShouldBeEmpty();
        }
        if (boundary != "approver")
        {
            _approvers.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>Admission may add trust metadata but cannot remove or replace a current authorization bound.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdmissionCannotDiscardOrReplaceTheAuthorityLifetime(bool replace)
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        var intent = Intent(new(_clock, _clock.Now, _clock.Now.AddSeconds(5)));
        admission.AdmitAsync(intent, Arg.Any<CancellationToken>()).Returns(Intent(replace
            ? new(_clock, _clock.Now, _clock.Now.AddSeconds(10)) : null));

        AgentDependencyNotAvailableException error = await Should.ThrowAsync<AgentDependencyNotAvailableException>(
            () => new EventStoreAgentCommandDispatcher(gateway, admission).DispatchAsync(intent, CancellationToken.None));

        error.RecordId.ShouldBe("EXT-HOST-1");
        gateway.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A delayed gateway acknowledgement keeps the outcome of an already admitted command.</summary>
    [Fact]
    public async Task ExpiryAfterGatewaySubmissionDoesNotContradictItsAcceptedOutcome()
    {
        DateTimeOffset started = _clock.Now;
        ConfigureAuthority();
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        admission.AdmitAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<AgentCommandIntent>());
        gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _clock.Now = started.AddSeconds(5);
            return new SubmitCommandResponse(CorrelationId,
                JsonSerializer.SerializeToElement(new { effect = "Applied", configurationVersion = 2 }), MessageId);
        });

        AgentOperationResult<AgentCommandAcceptance> result = await WriteAsync(
            Operations(new EventStoreAgentCommandDispatcher(gateway, admission)), "disable");

        result.Value.ShouldNotBeNull().MessageId.ShouldBe(MessageId);
        await gateway.Received(1).SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Payload-less receipts cannot start or disclose status work after the original interval fails.</summary>
    [Theory]
    [InlineData("before-status", false)]
    [InlineData("status-expiry", false)]
    [InlineData("status-rollback", false)]
    [InlineData("current", true)]
    public async Task PayloadlessReceiptRetainsAuthorityAcrossStatusRead(string boundary, bool current)
    {
        DateTimeOffset started = _clock.Now;
        ConfigureAuthority();
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        admission.AdmitAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<AgentCommandIntent>());
        gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _clock.Now = started.AddSeconds(boundary == "before-status" ? 5 : 1);
            return new SubmitCommandResponse(CorrelationId, MessageId: MessageId);
        });
        var status = Substitute.For<IAgentCommandStatusReader>();
        status.WasRejectedAsync(TenantId, "agent", MessageId, CorrelationId, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _clock.Now = started.AddSeconds(boundary == "status-rollback" ? 0 : current ? 2 : 5);
            return true;
        });

        AgentOperationResult<AgentCommandAcceptance> result = await WriteAsync(
            Operations(new EventStoreAgentCommandDispatcher(gateway, admission), status), "disable");

        result.Error.ShouldNotBeNull().Code.ShouldBe(current ? AgentOperationErrorCode.Rejected : AgentOperationErrorCode.UnableToVerify);
        if (boundary == "before-status")
        {
            status.ReceivedCalls().ShouldBeEmpty();
        }
        else
        {
            await status.Received(1).WasRejectedAsync(TenantId, "agent", MessageId, CorrelationId, Arg.Any<CancellationToken>());
        }
    }

    /// <summary>Final admission enforces the checked interval before target construction and permits valid controls.</summary>
    [Theory]
    [InlineData("already-expired", false)]
    [InlineData("admission-expiry", false)]
    [InlineData("admission-rollback", false)]
    [InlineData("current", true)]
    public async Task DispatcherRetainsTheOriginalDeadlineAndClockOrdering(string boundary, bool accepted)
    {
        DateTimeOffset started = _clock.Now;
        var bound = new AgentAuthorityValidity(_clock, started, started.AddSeconds(5));
        var intent = Intent(bound);
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse(CorrelationId, MessageId: MessageId));
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        admission.AdmitAsync(intent, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _clock.Now = started.AddSeconds(2);
            bound.IsCurrent.ShouldBeTrue();
            _clock.Now = started.AddSeconds(boundary == "admission-rollback" ? 1 : boundary == "current" ? 4 : 5);
            return new AgentCommandIntent(intent.MessageId, intent.TenantId, intent.Domain, intent.AggregateId,
                intent.CommandType, intent.Payload, intent.CorrelationId, intent.CausationId, intent.UserId,
                new Dictionary<string, string> { ["trust:deliveryNonce"] = "independent-nonce" }) { Authority = intent.Authority };
        });
        if (boundary == "already-expired")
        {
            _clock.Now = started.AddSeconds(5);
        }
        var dispatcher = new EventStoreAgentCommandDispatcher(gateway, admission);

        if (accepted)
        {
            (await dispatcher.DispatchAsync(intent, CancellationToken.None)).MessageId.ShouldBe(MessageId);
            await gateway.Received(1).SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>());
        }
        else
        {
            AgentCommandAuthorityExpiredException error = await Should.ThrowAsync<AgentCommandAuthorityExpiredException>(
                () => dispatcher.DispatchAsync(intent, CancellationToken.None));
            error.Message.ShouldBe("CurrentAuthorityUnavailable");
            gateway.ReceivedCalls().ShouldBeEmpty();
            if (boundary == "already-expired")
            {
                admission.ReceivedCalls().ShouldBeEmpty();
            }
        }
    }

    private void ConfigureAuthority(int actorSeconds = 5, int roleSeconds = 5)
    {
        DateTimeOffset started = _clock.Now;
        _actors.ReadAsync(TenantId, "subject", Arg.Any<CancellationToken>()).Returns(_ =>
            new CurrentHumanActorEvidence(TenantId, "subject", "stable-actor", "human-party", 1, 1, _clock.Now, started.AddSeconds(actorSeconds)));
        _tenants.ReadAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<bool>()
            ? new TenantAuthorityEvidence("system", "global-administrators", 1, _clock.Now, started.AddSeconds(roleSeconds),
                [new(1, "stable-actor", null, "Agents.PlatformOperator", true)], SourceAggregateId: "global-administrators")
            : new TenantAuthorityEvidence(TenantId, "tenants", 2, _clock.Now, started.AddSeconds(roleSeconds),
                [new(1, "", null, "TenantActive", true), new(2, "stable-actor", null, "Agents.Administrator", true)], SourceAggregateId: TenantId));
    }

    private HttpAgentAdministrationContextProvider ContextProvider() => new(new HttpContextAccessor
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenantId", TenantId), new Claim("sub", "subject")], "test")),
        },
    }, _tenants, _actors, _availability, _clock);

    private EventStoreAgentAdministrationOperations Operations(IAgentCommandDispatcher dispatcher, IAgentCommandStatusReader? status = null) => new(ContextProvider(),
        new AgentAdministrationOrchestrator(dispatcher), new AgentResponseModeOrchestrator(dispatcher),
        new AgentActivationProviderRevalidation(_catalog, _approvers, dispatcher), _store,
        Options.Create(new AgentSetupReadModelOptions()), new AgentCommandIdentityFactory(),
        status ?? Substitute.For<IAgentCommandStatusReader>(), _approvers);

    private static AgentSetupReadModel Model(ApproverPolicySource? configured = null) => new()
    {
        IsCreated = true, TenantId = TenantId, AgentId = AgentId, DisplayName = "protected-setup-name", ConfigurationVersion = 1,
        ResponseMode = AgentResponseMode.Confirmation, ApproverPolicySources = configured is null ? [] : [configured],
        ApproverPolicyDisclosure = ApproverPolicyBasisDisclosure.OperatorOnly,
    };

    private static AgentCommandIntent Intent(AgentAuthorityValidity? authority) => new(MessageId, TenantId, "agent", AgentId,
        nameof(DisableAgent), "{}"u8.ToArray(), CorrelationId, null, "stable-actor", null) { Authority = authority };

    private static async Task<AgentOperationResult<AgentCommandAcceptance>> WriteAsync(EventStoreAgentAdministrationOperations operations, string operation)
    {
        var options = new AgentOperationOptions(IdempotencyKey: MessageId, CorrelationId: CorrelationId) { ExpectedConfigurationVersion = 1 };
        return operation switch
        {
            "create" => await operations.CreateAsync(AgentId, new CreateAgent(TenantId, "hexa", null, "instructions"), options),
            "update" => await operations.UpdateConfigurationAsync(AgentId, new UpdateAgentConfiguration("hexa", null, "instructions"), options),
            "response-mode" => await operations.ConfigureResponseModeAsync(AgentId, new ConfigureAgentResponseMode(AgentResponseMode.Automatic), options),
            "activate" => await operations.ActivateAsync(AgentId, new ActivateAgent(), options),
            "disable" => await operations.DisableAsync(AgentId, new DisableAgent(), options),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
    }
}
