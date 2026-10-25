using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Local setup-write cancellation boundaries; substituted ports establish no external availability.</summary>
public sealed class SetupWriteCancellationAdmissionTests
{
    private const string TenantId = "tenant-a";
    private const string AgentId = "hexa";
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private readonly IReadModelStore _store = Substitute.For<IReadModelStore>();
    private readonly IAgentAdministrationContextProvider _context = Substitute.For<IAgentAdministrationContextProvider>();
    private readonly IApproverPolicyResolver _approvers = Substitute.For<IApproverPolicyResolver>();
    private readonly IProviderCatalogReader _catalog = Substitute.For<IProviderCatalogReader>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();
    private readonly IAgentCommandStatusReader _status = Substitute.For<IAgentCommandStatusReader>();

    /// <summary>Gets setup writes and the boundaries that must not continue after cancellation.</summary>
    public static TheoryData<string, string> WriteCases
    {
        get
        {
            var cases = new TheoryData<string, string>();
            foreach (string operation in new[] { "create", "update", "response-mode", "disable", "activate" })
            {
                foreach (string boundary in new[] { "before", "authority", "denied-authority", "dispatch", "dispatch-failure", "status", "status-failure" })
                {
                    cases.Add(operation, boundary);
                }
            }

            return cases;
        }
    }

    /// <summary>Cancelled setup writes neither start later protected work nor return a processed receipt.</summary>
    [Theory]
    [MemberData(nameof(WriteCases))]
    public async Task CancelledWriteStopsBeforeLaterReadsDispatchOrRendering(string operation, string boundary)
    {
        using var cancellation = new CancellationTokenSource();
        Configure(boundary, cancellation);

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(
            () => WriteAsync(operation, cancellation.Token));

        error.CancellationToken.ShouldBe(cancellation.Token);
        if (boundary == "before")
        {
            _context.ReceivedCalls().ShouldBeEmpty();
        }

        if (boundary is "before" or "authority" or "denied-authority")
        {
            _store.ReceivedCalls().ShouldBeEmpty();
            _catalog.ReceivedCalls().ShouldBeEmpty();
            _approvers.ReceivedCalls().ShouldBeEmpty();
            _dispatcher.ReceivedCalls().ShouldBeEmpty();
        }

        if (boundary is not ("status" or "status-failure"))
        {
            _status.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>Both exact-version activation and retained replay stop after a cancelled setup read.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledActivationSetupReadCannotStartDependencyReadsOrReplay(bool retainedReplay)
    {
        using var cancellation = new CancellationTokenSource();
        Configure(retainedReplay ? "store-replay" : "store", cancellation);

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(
            () => WriteAsync("activate", cancellation.Token));

        error.CancellationToken.ShouldBe(cancellation.Token);
        _catalog.ReceivedCalls().ShouldBeEmpty();
        _approvers.ReceivedCalls().ShouldBeEmpty();
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
        _status.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Direct activation stops at each await before later dependencies or command construction.</summary>
    [Theory]
    [InlineData("before")]
    [InlineData("catalog")]
    [InlineData("approver")]
    [InlineData("dispatch")]
    public async Task CancelledDirectActivationCannotContinueToTheNextBoundary(string boundary)
    {
        using var cancellation = new CancellationTokenSource();
        Configure(boundary, cancellation);

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(
            () => Activation().ExecuteAsync(ActivationRequest(), cancellation.Token));

        error.CancellationToken.ShouldBe(cancellation.Token);
        if (boundary == "before")
        {
            _catalog.ReceivedCalls().ShouldBeEmpty();
        }

        if (boundary is "before" or "catalog")
        {
            _approvers.ReceivedCalls().ShouldBeEmpty();
        }

        if (boundary != "dispatch")
        {
            _dispatcher.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>Pre-cancelled retained activation cannot construct or dispatch a target intent.</summary>
    [Fact]
    public async Task CancelledRetainedActivationStopsBeforeDispatch()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(
            () => Activation().AttemptReplayAsync(ActivationRequest(), cancellation.Token));

        error.CancellationToken.ShouldBe(cancellation.Token);
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    private void Configure(string boundary, CancellationTokenSource cancellation)
    {
        _context.GetContextAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            CancelAt("authority", "denied-authority");
            return new AgentAdministrationContext(TenantId, "actor", boundary != "denied-authority");
        });
        _store.GetAsync<AgentSetupReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                CancelAt("store", "store-replay");
                return new ReadModelEntry<AgentSetupReadModel>(new AgentSetupReadModel
                {
                    IsCreated = true,
                    TenantId = TenantId,
                    AgentId = AgentId,
                    ConfigurationVersion = boundary == "store-replay" ? 2 : 1,
                    ProviderId = "provider",
                    ModelId = "model",
                    ResponseMode = AgentResponseMode.Confirmation,
                    ApproverPolicySources = Policy().Sources,
                    ApproverPolicyDisclosure = ApproverPolicyBasisDisclosure.OperatorOnly,
                }, "etag");
            });
        _catalog.GetEntryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                CancelAt("catalog");
                return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Unavailable, null);
            });
        _approvers.ResolveAsync(Arg.Any<string>(), Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                CancelAt("approver");
                return new ApproverPolicyResolutionResult([]);
            });
        _dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            CancelAt("dispatch", "dispatch-failure");
            return boundary == "dispatch-failure"
                ? Task.FromException<SubmitCommandResponse>(new InvalidOperationException("dispatch-fixture"))
                : Task.FromResult(new SubmitCommandResponse(CorrelationId, MessageId: MessageId));
        });
        _status.WasRejectedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            CancelAt("status", "status-failure");
            return boundary == "status-failure"
                ? Task.FromException<bool?>(new InvalidOperationException("status-fixture"))
                : Task.FromResult<bool?>(true);
        });
        CancelAt("before");

        void CancelAt(params string[] boundaries)
        {
            if (boundaries.Contains(boundary, StringComparer.Ordinal))
            {
                cancellation.Cancel();
            }
        }
    }

    private async Task WriteAsync(string operation, CancellationToken cancellationToken)
    {
        EventStoreAgentAdministrationOperations operations = Operations();
        var options = new AgentOperationOptions(CorrelationId, MessageId) { ExpectedConfigurationVersion = 1 };
        _ = operation switch
        {
            "create" => await operations.CreateAsync(AgentId, new CreateAgent(TenantId, "hexa", "description", "instructions"), options, cancellationToken),
            "update" => await operations.UpdateConfigurationAsync(AgentId, new UpdateAgentConfiguration("hexa", "description", "instructions"), options, cancellationToken),
            "response-mode" => await operations.ConfigureResponseModeAsync(AgentId, new ConfigureAgentResponseMode(AgentResponseMode.Confirmation), options, cancellationToken),
            "disable" => await operations.DisableAsync(AgentId, new DisableAgent(), options, cancellationToken),
            "activate" => await operations.ActivateAsync(AgentId, new ActivateAgent(), options, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
    }

    private EventStoreAgentAdministrationOperations Operations()
        => new(_context, new AgentAdministrationOrchestrator(_dispatcher), new AgentResponseModeOrchestrator(_dispatcher),
            Activation(), _store, Options.Create(new AgentSetupReadModelOptions()), new AgentCommandIdentityFactory(), _status);

    private AgentActivationProviderRevalidation Activation() => new(_catalog, _approvers, _dispatcher);

    private static AgentActivationRevalidationRequest ActivationRequest()
        => new(MessageId, CorrelationId, TenantId, AgentId, "actor", true, 1, "provider", "model",
            AgentResponseMode.Confirmation, Policy());

    private static AgentApproverPolicy Policy()
        => new([new(ApproverPolicySourceKind.PredefinedParty, "approver-party", null)], ApproverPolicyBasisDisclosure.OperatorOnly);
}
