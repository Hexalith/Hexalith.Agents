namespace Hexalith.Agents.Server.Tests;

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Queries;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// Tests for the live Agent setup query handlers (Story 5.2 AC2, AC4). They cover the truth stage a read reports
/// and — just as importantly — what an unauthorized or cross-tenant caller is allowed to learn.
/// </summary>
public sealed class AgentSetupQueryHandlerTests
{
    private const string TenantId = "acme";
    private const string AgentId = "hexa";
    private const string StoreName = "statestore";
    private const string UserId = "admin-user";

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FakeReadModelStore _store = new();
    private readonly ITenantAccessReader _tenantAccess = Substitute.For<ITenantAccessReader>();
    private readonly IAgentAdministrationContextProvider _context = Substitute.For<IAgentAdministrationContextProvider>();

    public AgentSetupQueryHandlerTests()
    {
        _tenantAccess.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: true));
        _context.GetContext().Returns(new AgentAdministrationContext(TenantId, UserId, false));
        _context.GetContextAsync(false, Arg.Any<CancellationToken>())
            .Returns(new AgentAdministrationContext(TenantId, "stable-actor", true));
    }

    /// <summary>Gets administrator evidence failures that must deny both setup query entry points.</summary>
    public static TheoryData<bool, string> AdministratorDenialCases
    {
        get
        {
            var cases = new TheoryData<bool, string>();
            foreach (bool status in new[] { true, false })
            {
                foreach (string evidence in new[]
                {
                    "member", "global-admin", "wrong-subject", "subject-case", "missing-subject",
                    "wrong-identity-tenant", "identity-tenant-case", "missing-identity-tenant",
                    "wrong-authority-tenant", "authority-tenant-case", "missing-authority-tenant",
                    "missing-actor", "missing-provider", "legacy-provider",
                })
                {
                    cases.Add(status, evidence);
                }
            }

            return cases;
        }
    }

    /// <summary>Membership, infrastructure elevation and mismatched identities never grant setup disclosure.</summary>
    [Theory]
    [MemberData(nameof(AdministratorDenialCases))]
    public async Task CurrentScopedAdministratorEvidenceIsRequiredBeforeAnyRead(bool statusQuery, string evidence)
    {
        Seed(configurationVersion: 3, lastSequence: 5);
        var resolver = Substitute.For<IApproverPolicyResolver>();
        AgentAdministrationContext identity = new(TenantId, UserId, false);
        AgentAdministrationContext actor = new(TenantId, "stable-actor", true);
        switch (evidence)
        {
            case "member": actor = actor with { IsAgentsAdmin = false }; break;
            case "global-admin": actor = actor with { IsAgentsAdmin = false, IsPlatformOperator = true }; break;
            case "wrong-subject": identity = identity with { ActorUserId = "other-subject" }; break;
            case "subject-case": identity = identity with { ActorUserId = "Admin-user" }; break;
            case "missing-subject": identity = identity with { ActorUserId = "" }; break;
            case "wrong-identity-tenant": identity = identity with { TenantId = "foreign-tenant" }; break;
            case "identity-tenant-case": identity = identity with { TenantId = "Acme" }; break;
            case "missing-identity-tenant": identity = identity with { TenantId = "" }; break;
            case "wrong-authority-tenant": actor = actor with { TenantId = "foreign-tenant" }; break;
            case "authority-tenant-case": actor = actor with { TenantId = "Acme" }; break;
            case "missing-authority-tenant": actor = actor with { TenantId = "" }; break;
            case "missing-actor": actor = actor with { ActorUserId = " " }; break;
        }

        _context.GetContext().Returns(identity);
        _context.GetContextAsync(false, Arg.Any<CancellationToken>()).Returns(actor);
        IAgentAdministrationContextProvider? provider = evidence switch
        {
            "missing-provider" => null,
            "legacy-provider" => new LegacyRoleClaimAdministrationContextProvider(),
            _ => _context,
        };
        var options = Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName });
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(_store, options, _tenantAccess, resolver, provider)
            : new GetAgentConfigurationQueryHandler(_store, options, _tenantAccess, resolver, provider);
        QueryEnvelope query = Query(handler.QueryType) with { IsGlobalAdmin = evidence == "global-admin" };
        if (evidence == "legacy-provider")
        {
            query = query with { TenantId = "tenant-a", UserId = "authenticated-subject" };
        }

        QueryResult response = await handler.ExecuteAsync(query, CancellationToken.None);

        ReadSetupResult(response).ShouldBe(AgentSetupResult.NotAuthorized());
        _store.GetCount.ShouldBe(0);
        resolver.ReceivedCalls().ShouldBeEmpty();
        _tenantAccess.ReceivedCalls().ShouldBeEmpty();
        Encoding.UTF8.GetString(response.PayloadBytes.ShouldNotBeNull()).ShouldNotContain("hexa");
    }

    /// <summary>Current authority can resolve an authenticated subject to a different stable actor identity.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CurrentScopedAdministratorWithDistinctStableActorCanRead(bool statusQuery)
    {
        Seed(configurationVersion: 3, lastSequence: 5);
        var services = new ServiceCollection();
        services.AddSingleton<IReadModelStore>(_store);
        services.AddSingleton<IOptions<AgentSetupReadModelOptions>>(
            Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }));
        services.AddSingleton(_tenantAccess);
        services.AddSingleton(_context);
        services.AddTransient<GetAgentStatusQueryHandler>();
        services.AddTransient<GetAgentConfigurationQueryHandler>();
        using ServiceProvider provider = services.BuildServiceProvider();
        AgentSetupQueryHandlerBase handler = statusQuery
            ? provider.GetRequiredService<GetAgentStatusQueryHandler>()
            : provider.GetRequiredService<GetAgentConfigurationQueryHandler>();
        using var cancellation = new CancellationTokenSource();

        QueryResult response = await handler.ExecuteAsync(Query(handler.QueryType), cancellation.Token);

        ReadSetupResult(response).Setup.ShouldNotBeNull().ConfigurationVersion.ShouldBe(3);
        _store.GetCount.ShouldBe(1);
        await _context.Received(1).GetContextAsync(false, cancellation.Token);
        await _tenantAccess.Received(1).ReadAsync(TenantId, UserId, string.Empty, cancellation.Token);
    }

    /// <summary>Failure to obtain current authority denies disclosure without returning infrastructure details.</summary>
    [Theory]
    [InlineData(true, "identity")]
    [InlineData(false, "identity")]
    [InlineData(true, "current-sync")]
    [InlineData(false, "current-sync")]
    [InlineData(true, "current-async")]
    [InlineData(false, "current-async")]
    public async Task AuthorityReadFailureDeniesBeforeStoreAndEnrichment(bool statusQuery, string boundary)
    {
        Seed(configurationVersion: 3, lastSequence: 5);
        var resolver = Substitute.For<IApproverPolicyResolver>();
        var error = new InvalidOperationException("private-authority-details");
        if (boundary == "identity")
        {
            _context.GetContext().Returns<AgentAdministrationContext>(_ => throw error);
        }
        else
        {
            _context.GetContextAsync(false, Arg.Any<CancellationToken>())
                .Returns<ValueTask<AgentAdministrationContext>>(_ => boundary == "current-sync"
                    ? throw error
                    : new ValueTask<AgentAdministrationContext>(Task.FromException<AgentAdministrationContext>(error)));
        }

        var options = Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName });
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(_store, options, _tenantAccess, resolver, _context)
            : new GetAgentConfigurationQueryHandler(_store, options, _tenantAccess, resolver, _context);

        QueryResult response = await handler.ExecuteAsync(Query(handler.QueryType), CancellationToken.None);

        ReadSetupResult(response).ShouldBe(AgentSetupResult.NotAuthorized());
        _store.GetCount.ShouldBe(0);
        resolver.ReceivedCalls().ShouldBeEmpty();
        _tenantAccess.ReceivedCalls().ShouldBeEmpty();
        Encoding.UTF8.GetString(response.PayloadBytes.ShouldNotBeNull()).ShouldNotContain("private-authority-details");
    }

    /// <summary>Verifies that a misrouted model cannot disclose another scope or cause a foreign approver lookup.</summary>
    [Theory]
    [InlineData(true, "foreign-tenant", AgentId)]
    [InlineData(false, "foreign-tenant", AgentId)]
    [InlineData(true, TenantId, "foreign-agent")]
    [InlineData(false, TenantId, "foreign-agent")]
    [InlineData(true, "foreign-tenant", "foreign-agent")]
    [InlineData(false, "foreign-tenant", "foreign-agent")]
    [InlineData(true, "", AgentId)]
    [InlineData(false, "", AgentId)]
    [InlineData(true, TenantId, "")]
    [InlineData(false, TenantId, "")]
    [InlineData(true, "Acme", AgentId)]
    [InlineData(false, "Acme", AgentId)]
    [InlineData(true, TenantId, "HEXA")]
    [InlineData(false, TenantId, "HEXA")]
    public async Task MisroutedSetupIsUnavailableBeforeDisclosureOrApproverLookup(bool statusQuery, string modelTenant, string modelAgent)
    {
        var resolver = Substitute.For<IApproverPolicyResolver>();
        _store.Seed(StoreName, AgentSetupReadModelAddresses.Detail(TenantId, AgentId), new AgentSetupReadModel
        {
            IsCreated = true,
            AgentId = modelAgent,
            TenantId = modelTenant,
            DisplayName = "foreign-display-name",
            Description = "foreign-description",
            ApproverPolicySources = [new(ApproverPolicySourceKind.PredefinedParty, "foreign-party", null)],
            ApproverPolicyDisclosure = ApproverPolicyBasisDisclosure.OperatorOnly,
            ResponseMode = AgentResponseMode.Confirmation,
            ConfigurationVersion = 99,
        });
        var options = Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName });
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(_store, options, _tenantAccess, resolver, _context)
            : new GetAgentConfigurationQueryHandler(_store, options, _tenantAccess, resolver, _context);

        QueryResult response = await handler.ExecuteAsync(
            Query(statusQuery ? GetAgentStatusQuery.QueryType : GetAgentConfigurationQuery.QueryType), CancellationToken.None);
        AgentSetupResult result = ReadSetupResult(response);

        result.Status.ShouldBe(AgentInspectionStatus.Unavailable);
        result.Setup.ShouldBeNull();
        Encoding.UTF8.GetString(response.PayloadBytes.ShouldNotBeNull()).ShouldNotContain("foreign-");
        resolver.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Verifies that same kind substituted party cannot confirm the safe query basis.</summary>
    [Fact]
    public async Task SameKindSubstitutedPartyCannotConfirmTheSafeQueryBasis()
    {
        var configured = new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, "configured-party", null);
        var resolver = Substitute.For<IApproverPolicyResolver>();
        resolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>()).Returns(
            new ApproverPolicyResolutionResult([new ApproverSourceResolution(ApproverPolicySourceKind.PredefinedParty, ApproverSourceOutcome.Resolved)
            {
                Source = new(ApproverPolicySourceKind.PredefinedParty, "substituted-party", null),
            }]));
        _store.Seed(StoreName, AgentSetupReadModelAddresses.Detail(TenantId, AgentId), new AgentSetupReadModel
        {
            IsCreated = true,
            AgentId = AgentId,
            TenantId = TenantId,
            DisplayName = "hexa",
            ApproverPolicySources = [configured],
            ApproverPolicyDisclosure = ApproverPolicyBasisDisclosure.OperatorOnly,
            ResponseMode = AgentResponseMode.Confirmation,
            ConfigurationVersion = 1,
        });
        var handler = new GetAgentStatusQueryHandler(_store,
            Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }), _tenantAccess, resolver, _context);
        var result = await ExecuteAsync(handler, Query(GetAgentStatusQuery.QueryType));
        result.Setup.ShouldNotBeNull().Agent.ApproverPolicyBasis.Single().Status.ShouldBe(ApproverPolicyValidationStatus.Unavailable);
    }

    [Fact]
    public async Task Both_setup_queries_answer_with_the_same_authoritative_truth()
    {
        Seed(configurationVersion: 3, lastSequence: 5);

        AgentSetupResult status = await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType));
        AgentSetupResult configuration = await ExecuteAsync(ConfigurationHandler(), Query(GetAgentConfigurationQuery.QueryType));

        status.ShouldBeEquivalentTo(configuration);
        status.Setup.ShouldNotBeNull().ConfigurationVersion.ShouldBe(3);
        status.Setup.ProjectionVersion.ShouldBe("5");
    }

    [Fact]
    public async Task A_read_without_an_expectation_is_projection_confirmed_and_current()
    {
        Seed(configurationVersion: 3, lastSequence: 5);

        AgentSetupView setup = (await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType))).Setup.ShouldNotBeNull();

        setup.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        setup.Freshness.ShouldBe(AgentSetupFreshness.Current);
    }

    [Fact]
    public async Task A_read_waiting_for_a_newer_version_is_authoritative_pending_and_stale()
    {
        Seed(configurationVersion: 3, lastSequence: 5);

        AgentSetupView setup = (await ExecuteAsync(
            StatusHandler(),
            Query(GetAgentStatusQuery.QueryType, new GetAgentStatusQuery(ExpectedConfigurationVersion: 4)))).Setup.ShouldNotBeNull();

        setup.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        setup.Freshness.ShouldBe(AgentSetupFreshness.Stale);

        // The pending answer still reports what the projection actually holds, never the version being awaited.
        setup.ConfigurationVersion.ShouldBe(3);
    }

    [Fact]
    public async Task A_read_whose_expectation_has_landed_is_projection_confirmed()
    {
        Seed(configurationVersion: 4, lastSequence: 6);

        AgentSetupView setup = (await ExecuteAsync(
            StatusHandler(),
            Query(GetAgentStatusQuery.QueryType, new GetAgentStatusQuery(ExpectedConfigurationVersion: 4)))).Setup.ShouldNotBeNull();

        setup.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        setup.Freshness.ShouldBe(AgentSetupFreshness.Current);
    }

    [Fact]
    public async Task A_later_projected_version_confirms_the_original_command_derived_target()
    {
        Seed(configurationVersion: 5, lastSequence: 8);

        AgentSetupView setup = (await ExecuteAsync(
            StatusHandler(),
            Query(GetAgentStatusQuery.QueryType, new GetAgentStatusQuery(ExpectedConfigurationVersion: 4)))).Setup.ShouldNotBeNull();

        // Ordered projection cannot reach version 5 without applying the original command-derived version 4.
        setup.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        setup.ConfigurationVersion.ShouldBe(5);
        setup.Freshness.ShouldBe(AgentSetupFreshness.Current);
    }

    [Fact]
    public async Task Lifecycle_active_is_reported_as_a_lifecycle_flag_and_never_as_callability()
    {
        Seed(configurationVersion: 3, lastSequence: 5, lifecycle: AgentLifecycleStatus.Active);

        AgentSetupView setup = (await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType))).Setup.ShouldNotBeNull();

        setup.Agent.Lifecycle.ShouldBe(AgentLifecycleStatus.Active);

        // An Agent with no provider selection, no approver policy, and no launch readiness is emphatically not
        // callable; the read model reports the blockers rather than inferring readiness from the lifecycle flag.
        setup.Agent.ActivationBlockers.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task An_unauthorized_caller_is_denied_before_the_read_model_is_addressed()
    {
        Seed(configurationVersion: 3, lastSequence: 5);
        _tenantAccess
            .ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TenantAccessReadResult.Unavailable);

        AgentSetupResult result = await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType));

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        result.Setup.ShouldBeNull();
    }

    [Fact]
    public async Task A_cross_tenant_caller_cannot_learn_the_agent_exists()
    {
        Seed(configurationVersion: 3, lastSequence: 5);

        // The authenticated administrator's tenant must match the requested scope before any read occurs.
        AgentSetupResult result = await ExecuteAsync(
            StatusHandler(),
            Query(GetAgentStatusQuery.QueryType, tenantId: "other-tenant"));

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        result.Setup.ShouldBeNull();
        _store.GetCount.ShouldBe(0);
        JsonSerializer.Serialize(result, _json).ShouldNotContain(TenantId);
    }

    [Fact]
    public async Task A_stale_tenant_access_read_is_denied_rather_than_trusted()
    {
        Seed(configurationVersion: 3, lastSequence: 5);
        _tenantAccess
            .ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: false));

        AgentSetupResult result = await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType));

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
    }

    [Fact]
    public async Task A_denied_read_never_consults_the_read_model()
    {
        Seed(configurationVersion: 3, lastSequence: 5);
        _tenantAccess
            .ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TenantAccessReadResult.Unavailable);

        AgentSetupResult denied = await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType));
        AgentSetupResult missing = await ExecuteAsync(
            StatusHandler(),
            Query(GetAgentStatusQuery.QueryType, tenantId: "empty-tenant"));

        // A caller cannot tell "no permission" from "no such Agent": both answers are shaped alike apart from the
        // status, and neither carries versions, blockers, or any other trace of the seeded Agent.
        denied.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        missing.Setup.ShouldBeNull();
        denied.Setup.ShouldBeNull();
    }

    /// <summary>Both query entry points translate backing-store faults without disclosing details or resolving approvers.</summary>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task StoreFailureReturnsUnavailable(bool statusQuery, bool synchronousFailure)
    {
        var store = Substitute.For<IReadModelStore>();
        var resolver = Substitute.For<IApproverPolicyResolver>();
        store.GetAsync<AgentSetupReadModel>(StoreName, AgentSetupReadModelAddresses.Detail(TenantId, AgentId),
            Arg.Any<CancellationToken>()).Returns<Task<ReadModelEntry<AgentSetupReadModel>>>(_ =>
            synchronousFailure
                ? throw new InvalidOperationException("private-store-details")
                : Task.FromException<ReadModelEntry<AgentSetupReadModel>>(new InvalidOperationException("private-store-details")));
        var options = Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName });
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(store, options, _tenantAccess, resolver, _context)
            : new GetAgentConfigurationQueryHandler(store, options, _tenantAccess, resolver, _context);

        QueryResult response = await handler.ExecuteAsync(
            Query(statusQuery ? GetAgentStatusQuery.QueryType : GetAgentConfigurationQuery.QueryType), CancellationToken.None);
        AgentSetupResult result = ReadSetupResult(response);

        result.ShouldBe(AgentSetupResult.Unavailable());
        Encoding.UTF8.GetString(response.PayloadBytes.ShouldNotBeNull()).ShouldNotContain("private-store-details");
        resolver.ReceivedCalls().ShouldBeEmpty();
        await store.Received(1).GetAsync<AgentSetupReadModel>(StoreName,
            AgentSetupReadModelAddresses.Detail(TenantId, AgentId), Arg.Any<CancellationToken>());
    }

    /// <summary>Authorization denial still prevents any attempt to access a failing store.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeniedQueryNeverTouchesFailingStore(bool statusQuery)
    {
        var store = Substitute.For<IReadModelStore>();
        store.GetAsync<AgentSetupReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<ReadModelEntry<AgentSetupReadModel>>>(_ => throw new InvalidOperationException("private-store-details"));
        _tenantAccess.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TenantAccessReadResult.Unavailable);
        var options = Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName });
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(store, options, _tenantAccess, contextProvider: _context)
            : new GetAgentConfigurationQueryHandler(store, options, _tenantAccess, contextProvider: _context);

        AgentSetupResult result = await ExecuteAsync(handler,
            Query(statusQuery ? GetAgentStatusQuery.QueryType : GetAgentConfigurationQuery.QueryType));

        result.ShouldBe(AgentSetupResult.NotAuthorized());
        store.ReceivedCalls().ShouldBeEmpty();
    }

    private GetAgentStatusQueryHandler StatusHandler()
        => new(_store, Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }), _tenantAccess, contextProvider: _context);

    private GetAgentConfigurationQueryHandler ConfigurationHandler()
        => new(_store, Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }), _tenantAccess, contextProvider: _context);

    private static async Task<AgentSetupResult> ExecuteAsync(AgentSetupQueryHandlerBase handler, QueryEnvelope query)
        => ReadSetupResult(await handler.ExecuteAsync(query, CancellationToken.None));

    private static AgentSetupResult ReadSetupResult(QueryResult result)
    {
        result.Success.ShouldBeTrue();
        return JsonSerializer.Deserialize<AgentSetupResult>(result.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
    }

    private void Seed(
        int configurationVersion,
        long lastSequence,
        AgentLifecycleStatus lifecycle = AgentLifecycleStatus.Draft)
        => _store.Seed(
            StoreName,
            AgentSetupReadModelAddresses.Detail(TenantId, AgentId),
            new AgentSetupReadModel
            {
                IsCreated = true,
                AgentId = AgentId,
                TenantId = TenantId,
                DisplayName = "hexa",
                HasInstructions = true,
                InstructionsValid = true,
                InstructionsVersion = 1,
                Lifecycle = lifecycle,
                ConfigurationVersion = configurationVersion,
                ResponseMode = AgentResponseMode.Automatic,
                LastSequenceNumber = lastSequence,
                ProjectedAt = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero),
                ProjectionVersion = lastSequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });

    private static QueryEnvelope Query(
        string queryType,
        GetAgentStatusQuery? payload = null,
        string tenantId = TenantId,
        string userId = UserId)
        => new(
            tenantId,
            AgentSetupReadModelAddresses.Domain,
            AgentId,
            queryType,
            payload is null ? [] : JsonSerializer.SerializeToUtf8Bytes(payload, _json),
            "corr-1",
            userId);
}
