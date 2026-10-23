using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Queries;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Local setup cancellation boundaries; substituted ports establish no external availability.</summary>
public sealed class SetupReadCancellationAdmissionTests
{
    private const string TenantId = "tenant-a";
    private const string AgentId = "hexa";
    private readonly IReadModelStore _store = Substitute.For<IReadModelStore>();
    private readonly ITenantAccessReader _tenantAccess = Substitute.For<ITenantAccessReader>();
    private readonly IAgentAdministrationContextProvider _context = Substitute.For<IAgentAdministrationContextProvider>();
    private readonly IApproverPolicyResolver _approvers = Substitute.For<IApproverPolicyResolver>();

    /// <summary>Gets every setup API/query and cancellation boundary shared by both entry points.</summary>
    public static TheoryData<bool, string> CancellationCases
    {
        get
        {
            var cases = new TheoryData<bool, string>();
            foreach (bool status in new[] { true, false })
            {
                foreach (string boundary in new[] { "before", "authority", "denied-authority", "store", "store-no-policy", "approver", "approver-failure" })
                {
                    cases.Add(status, boundary);
                }
            }

            return cases;
        }
    }

    /// <summary>Cancelled setup queries stop before protected downstream reads or rendering.</summary>
    [Theory]
    [MemberData(nameof(CancellationCases))]
    public async Task QueryCancellationStopsBeforeFurtherReadsOrRendering(bool statusQuery, string boundary)
    {
        using var cancellation = new CancellationTokenSource();
        Configure(boundary, cancellation);
        var options = Options.Create(new AgentSetupReadModelOptions());
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(_store, options, _tenantAccess, _approvers, _context)
            : new GetAgentConfigurationQueryHandler(_store, options, _tenantAccess, _approvers, _context);
        var query = new QueryEnvelope(TenantId, AgentSetupReadModelAddresses.Domain, AgentId,
            statusQuery ? GetAgentStatusQuery.QueryType : GetAgentConfigurationQuery.QueryType, [], "correlation", "subject");

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(
            () => handler.ExecuteAsync(query, cancellation.Token));

        error.CancellationToken.ShouldBe(cancellation.Token);
        AssertNoLaterReads(boundary);
        if (boundary == "before")
        {
            _tenantAccess.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>Cancellation during authenticated identity extraction stops before authority lookup.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task QueryIdentityCancellationStopsBeforeAuthorityLookup(bool statusQuery)
    {
        using var cancellation = new CancellationTokenSource();
        Configure("none", cancellation);
        _context.GetContext().Returns(_ =>
        {
            cancellation.Cancel();
            return new AgentAdministrationContext(TenantId, "subject", false);
        });
        var options = Options.Create(new AgentSetupReadModelOptions());
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(_store, options, _tenantAccess, _approvers, _context)
            : new GetAgentConfigurationQueryHandler(_store, options, _tenantAccess, _approvers, _context);
        var query = new QueryEnvelope(TenantId, AgentSetupReadModelAddresses.Domain, AgentId,
            handler.QueryType, [], "correlation", "subject");

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(
            () => handler.ExecuteAsync(query, cancellation.Token));

        error.CancellationToken.ShouldBe(cancellation.Token);
        await _context.DidNotReceive().GetContextAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>());
        _tenantAccess.ReceivedCalls().ShouldBeEmpty();
        _store.ReceivedCalls().ShouldBeEmpty();
        _approvers.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Current-authority provider cancellation propagates even if the caller remains active.</summary>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task CurrentAuthorityProviderCancellationIsNeverTranslatedIntoDenial(bool statusQuery, bool synchronousFailure)
    {
        using var cancellation = new CancellationTokenSource();
        Configure("none", cancellation);
        var error = new OperationCanceledException("provider-cancelled");
        _context.GetContextAsync(false, Arg.Any<CancellationToken>())
            .Returns<ValueTask<AgentAdministrationContext>>(_ => synchronousFailure
                ? throw error
                : new ValueTask<AgentAdministrationContext>(Task.FromException<AgentAdministrationContext>(error)));
        var options = Options.Create(new AgentSetupReadModelOptions());
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(_store, options, _tenantAccess, _approvers, _context)
            : new GetAgentConfigurationQueryHandler(_store, options, _tenantAccess, _approvers, _context);
        var query = new QueryEnvelope(TenantId, AgentSetupReadModelAddresses.Domain, AgentId,
            handler.QueryType, [], "correlation", "subject");

        Task<QueryResult> execution = handler.ExecuteAsync(query, cancellation.Token);
        await Should.ThrowAsync<OperationCanceledException>(() => execution);

        execution.IsCanceled.ShouldBeTrue();
        cancellation.IsCancellationRequested.ShouldBeFalse();
        _tenantAccess.ReceivedCalls().ShouldBeEmpty();
        _store.ReceivedCalls().ShouldBeEmpty();
        _approvers.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Cancellation while awaiting authority wins over late grants, denials and infrastructure faults.</summary>
    [Theory]
    [InlineData(true, "grant")]
    [InlineData(false, "grant")]
    [InlineData(true, "deny")]
    [InlineData(false, "deny")]
    [InlineData(true, "failure")]
    [InlineData(false, "failure")]
    public async Task CancellationDuringPendingAuthorityStopsBeforeProtectedReads(bool statusQuery, string completion)
    {
        using var cancellation = new CancellationTokenSource();
        Configure("none", cancellation);
        TaskCompletionSource<AgentAdministrationContext> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _context.GetContextAsync(false, Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<AgentAdministrationContext>(pending.Task));
        var options = Options.Create(new AgentSetupReadModelOptions());
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(_store, options, _tenantAccess, _approvers, _context)
            : new GetAgentConfigurationQueryHandler(_store, options, _tenantAccess, _approvers, _context);
        var query = new QueryEnvelope(TenantId, AgentSetupReadModelAddresses.Domain, AgentId,
            handler.QueryType, [], "correlation", "subject");

        Task<QueryResult> execution = handler.ExecuteAsync(query, cancellation.Token);
        execution.IsCompleted.ShouldBeFalse();
        cancellation.Cancel();
        if (completion == "failure")
        {
            pending.SetException(new InvalidOperationException("late-authority-details"));
        }
        else
        {
            pending.SetResult(new AgentAdministrationContext(TenantId, "stable-actor", completion == "grant"));
        }

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(() => execution);

        error.CancellationToken.ShouldBe(cancellation.Token);
        _tenantAccess.ReceivedCalls().ShouldBeEmpty();
        _store.ReceivedCalls().ShouldBeEmpty();
        _approvers.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Cancelled direct setup reads stop before protected downstream reads or rendering.</summary>
    [Theory]
    [MemberData(nameof(CancellationCases))]
    public async Task DirectCancellationStopsBeforeFurtherReadsOrRendering(bool statusRead, string boundary)
    {
        using var cancellation = new CancellationTokenSource();
        Configure(boundary, cancellation);
        EventStoreAgentAdministrationOperations operations = Operations();

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            if (statusRead)
            {
                await operations.GetStatusAsync(AgentId, cancellationToken: cancellation.Token);
            }
            else
            {
                await operations.GetConfigurationAsync(AgentId, cancellationToken: cancellation.Token);
            }
        });

        error.CancellationToken.ShouldBe(cancellation.Token);
        AssertNoLaterReads(boundary);
        if (boundary == "before")
        {
            _context.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>A provider cancellation remains cancellation even when the caller has not cancelled.</summary>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task StoreCancellationIsNeverTranslatedIntoUnavailable(bool statusQuery, bool synchronousFailure)
    {
        using var cancellation = new CancellationTokenSource();
        Configure("none", cancellation);
        var error = new OperationCanceledException("provider-cancelled");
        _store.GetAsync<AgentSetupReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<ReadModelEntry<AgentSetupReadModel>>>(_ => synchronousFailure
                ? throw error
                : Task.FromException<ReadModelEntry<AgentSetupReadModel>>(error));
        var options = Options.Create(new AgentSetupReadModelOptions());
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(_store, options, _tenantAccess, _approvers, _context)
            : new GetAgentConfigurationQueryHandler(_store, options, _tenantAccess, _approvers, _context);
        var query = new QueryEnvelope(TenantId, AgentSetupReadModelAddresses.Domain, AgentId,
            statusQuery ? GetAgentStatusQuery.QueryType : GetAgentConfigurationQuery.QueryType, [], "correlation", "subject");

        Task<QueryResult> execution = handler.ExecuteAsync(query, cancellation.Token);
        await Should.ThrowAsync<OperationCanceledException>(() => execution);

        execution.IsCanceled.ShouldBeTrue();
        cancellation.IsCancellationRequested.ShouldBeFalse();
        _approvers.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Cancellation while awaiting a store read wins over its later infrastructure fault.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancellationDuringPendingReadWinsOverLaterStoreFault(bool statusQuery)
    {
        using var cancellation = new CancellationTokenSource();
        Configure("none", cancellation);
        TaskCompletionSource<ReadModelEntry<AgentSetupReadModel>> pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _store.GetAsync<AgentSetupReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(pendingRead.Task);
        var options = Options.Create(new AgentSetupReadModelOptions());
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(_store, options, _tenantAccess, _approvers, _context)
            : new GetAgentConfigurationQueryHandler(_store, options, _tenantAccess, _approvers, _context);
        var query = new QueryEnvelope(TenantId, AgentSetupReadModelAddresses.Domain, AgentId,
            statusQuery ? GetAgentStatusQuery.QueryType : GetAgentConfigurationQuery.QueryType, [], "correlation", "subject");

        Task<QueryResult> execution = handler.ExecuteAsync(query, cancellation.Token);
        execution.IsCompleted.ShouldBeFalse();
        cancellation.Cancel();
        pendingRead.SetException(new InvalidOperationException("late-store-details"));

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(() => execution);

        error.CancellationToken.ShouldBe(cancellation.Token);
        _approvers.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Read failure translation must not turn cancellation into a returned API result.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task QueryReadFailureCannotSwallowCancellation(bool statusQuery)
    {
        using var cancellation = new CancellationTokenSource();
        Configure("store-failure", cancellation);
        var options = Options.Create(new AgentSetupReadModelOptions());
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(_store, options, _tenantAccess, _approvers, _context)
            : new GetAgentConfigurationQueryHandler(_store, options, _tenantAccess, _approvers, _context);
        var query = new QueryEnvelope(TenantId, AgentSetupReadModelAddresses.Domain, AgentId,
            statusQuery ? GetAgentStatusQuery.QueryType : GetAgentConfigurationQuery.QueryType, [], "correlation", "subject");

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(
            () => handler.ExecuteAsync(query, cancellation.Token));

        error.CancellationToken.ShouldBe(cancellation.Token);
        _approvers.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Read failure translation must not turn cancellation into a returned API result.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DirectReadFailureCannotSwallowCancellation(bool statusRead)
    {
        using var cancellation = new CancellationTokenSource();
        Configure("store-failure", cancellation);
        EventStoreAgentAdministrationOperations operations = Operations();

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            if (statusRead)
            {
                await operations.GetStatusAsync(AgentId, cancellationToken: cancellation.Token);
            }
            else
            {
                await operations.GetConfigurationAsync(AgentId, cancellationToken: cancellation.Token);
            }
        });

        error.CancellationToken.ShouldBe(cancellation.Token);
        _approvers.ReceivedCalls().ShouldBeEmpty();
    }

    private void Configure(string boundary, CancellationTokenSource cancellation)
    {
        var configured = new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, "approver-party", null);
        _tenantAccess.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (boundary is "authority" or "denied-authority")
                {
                    cancellation.Cancel();
                }

                return new TenantAccessReadResult(boundary == "denied-authority"
                    ? AgentInteractionGateOutcome.Unauthorized : AgentInteractionGateOutcome.Satisfied, true);
            });
        _context.GetContext().Returns(new AgentAdministrationContext(TenantId, "subject", false));
        _context.GetContextAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (boundary is "authority" or "denied-authority")
            {
                cancellation.Cancel();
            }

            return new AgentAdministrationContext(TenantId, "actor", boundary != "denied-authority");
        });
        _store.GetAsync<AgentSetupReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (boundary is "store" or "store-no-policy" or "store-failure")
                {
                    cancellation.Cancel();
                }

                return boundary == "store-failure"
                    ? Task.FromException<ReadModelEntry<AgentSetupReadModel>>(new InvalidOperationException("unavailable-read-fixture"))
                    : Task.FromResult(new ReadModelEntry<AgentSetupReadModel>(new AgentSetupReadModel
                    {
                        IsCreated = true,
                        TenantId = TenantId,
                        AgentId = AgentId,
                        DisplayName = "protected-setup-label",
                        ConfigurationVersion = 1,
                        ApproverPolicySources = boundary == "store-no-policy" ? [] : [configured],
                        ApproverPolicyDisclosure = ApproverPolicyBasisDisclosure.OperatorOnly,
                    }, "etag"));
            });
        _approvers.ResolveAsync(Arg.Any<string>(), Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (boundary is "approver" or "approver-failure")
                {
                    cancellation.Cancel();
                }

                return boundary == "approver-failure"
                    ? Task.FromException<ApproverPolicyResolutionResult>(new InvalidOperationException("unavailable-approver-fixture"))
                    : Task.FromResult(new ApproverPolicyResolutionResult([new(configured.Kind, ApproverSourceOutcome.Resolved)
                    {
                        Source = configured,
                    }]));
            });
        if (boundary == "before")
        {
            cancellation.Cancel();
        }
    }

    private void AssertNoLaterReads(string boundary)
    {
        if (boundary is "before" or "authority" or "denied-authority")
        {
            _store.ReceivedCalls().ShouldBeEmpty();
        }

        if (boundary is not ("approver" or "approver-failure"))
        {
            _approvers.ReceivedCalls().ShouldBeEmpty();
        }
    }

    private EventStoreAgentAdministrationOperations Operations()
    {
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        return new(_context, new AgentAdministrationOrchestrator(dispatcher), new AgentResponseModeOrchestrator(dispatcher),
            new AgentActivationProviderRevalidation(Substitute.For<IProviderCatalogReader>(), _approvers, dispatcher),
            _store, Options.Create(new AgentSetupReadModelOptions()), new AgentCommandIdentityFactory(),
            Substitute.For<IAgentCommandStatusReader>(), _approvers);
    }
}
