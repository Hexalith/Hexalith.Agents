using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.UI.Services.Gateways;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// The live FrontComposer provider-catalog gateway (Story 5.3) over the public Agents client. Reads and writes are
/// thin translations of the public client's own typed outcomes.
/// </summary>
public sealed class AgentsClientProviderCatalogGatewayTests
{
    private readonly IAgentsClient _client = Substitute.For<IAgentsClient>();
    private readonly IProviderCatalogOperations _operations = Substitute.For<IProviderCatalogOperations>();

    public AgentsClientProviderCatalogGatewayTests() => _client.ProviderCatalog.Returns(_operations);

    [Theory]
    [InlineData("create", false)]
    [InlineData("create", true)]
    [InlineData("update", false)]
    [InlineData("update", true)]
    [InlineData("enable", false)]
    [InlineData("enable", true)]
    [InlineData("disable", false)]
    [InlineData("disable", true)]
    [InlineData("enablement", false)]
    [InlineData("enablement", true)]
    [InlineData("decision", false)]
    [InlineData("decision", true)]
    public async Task PreparedWritePassesCanonicalOptionsAndRefusesMismatchedAcceptance(string operation, bool mismatch)
    {
        var identity = new ProviderCatalogCommandAcceptance("openai", "gpt", "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW", AgentSetupTruthState.Unknown);
        AgentOperationResult<ProviderCatalogCommandAcceptance> Accept(AgentOperationOptions options)
        {
            options.IdempotencyKey.ShouldBe(identity.MessageId);
            options.CorrelationId.ShouldBe(identity.CorrelationId);
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Succeeded(new("openai", "gpt",
                mismatch ? "01ARZ3NDEKTSV4RRFFQ69G5FAX" : options.IdempotencyKey!, options.CorrelationId!, AgentSetupTruthState.Submitted));
        }
        _operations.CreateEntryAsync(Arg.Any<CreateProviderModelEntry>(), Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Accept(call.Arg<AgentOperationOptions>()));
        _operations.UpdateEntryAsync(Arg.Any<UpdateProviderModelEntry>(), Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Accept(call.Arg<AgentOperationOptions>()));
        _operations.EnableEntryAsync(Arg.Any<EnableProviderModelEntry>(), Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Accept(call.Arg<AgentOperationOptions>()));
        _operations.DisableEntryAsync(Arg.Any<DisableProviderModelEntry>(), Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Accept(call.Arg<AgentOperationOptions>()));
        _operations.SetTenantEnablementAsync(Arg.Any<SetTenantProviderModelEnablement>(), Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Accept(call.Arg<AgentOperationOptions>()));
        _operations.DecideDataHandlingAsync(Arg.Any<DecideProviderDataHandling>(), Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Accept(call.Arg<AgentOperationOptions>()));
        var pricing = new ProviderModelPricing("USD", 1m, 1m, 1);
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 1);
        ProviderCatalogWriteResult result = operation switch
        {
            "create" => await Gateway().CreateAsync(new("openai", "gpt", "Model", true, true, 1000, 100,
                new(30_000, 3), ProviderModelCapabilityFlags.None, null, pricing), identity, CancellationToken.None),
            "update" => await Gateway().UpdateAsync(new("openai", "gpt", "Model", true, 1000, 100,
                new(30_000, 3), ProviderModelCapabilityFlags.None, null, pricing, 1), identity, CancellationToken.None),
            "enable" => await Gateway().EnableAsync(new("openai", "gpt"), identity, CancellationToken.None),
            "disable" => await Gateway().DisableAsync(new("openai", "gpt"), identity, CancellationToken.None),
            "enablement" => await Gateway().SetTenantEnablementAsync(new("tenant-a", "openai", "gpt", false, 0, null), identity, CancellationToken.None),
            _ => await Gateway().DecideDataHandlingAsync(new("openai", "gpt", 1, true, "Reviewed", 0, terms, default), identity, CancellationToken.None),
        };
        result.Status.ShouldBe(mismatch ? AgentSetupWriteStatus.UnableToVerify : AgentSetupWriteStatus.Submitted);
        if (!mismatch)
        {
            result.Acceptance.ShouldNotBeNull().MessageId.ShouldBe(identity.MessageId);
            result.Acceptance.CorrelationId.ShouldBe(identity.CorrelationId);
        }
        _operations.ReceivedCalls().ShouldHaveSingleItem().GetArguments()[1].ShouldBeOfType<AgentOperationOptions>()
            .ShouldBe(new AgentOperationOptions(identity.CorrelationId, identity.MessageId));
    }

    [Fact]
    public async Task RecoveryPollingBindsTheOriginalCorrelation()
    {
        const string correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        _operations.GetCommandWriteOutcomeAsync("system", "message", Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(AgentOperationResult<AgentSetupWriteOutcome>.Succeeded(AgentSetupWriteOutcome.AlreadyApplied));
        (await Gateway().GetCommandOutcomeAsync("system", "message", correlation, CancellationToken.None))
            .ShouldBe(AgentSetupWriteStatus.AlreadyApplied);
        await _operations.Received(1).GetCommandWriteOutcomeAsync("system", "message",
            Arg.Is<AgentOperationOptions>(options => options.CorrelationId == correlation), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeferredPreparedBindingRefusesSafely()
    {
        IProviderCatalogGateway deferred = new DeferredProviderCatalogGateway();
        ProviderCatalogWriteResult result = await deferred.DisableAsync(new("openai", "gpt"),
            new("openai", "gpt", "01ARZ3NDEKTSV4RRFFQ69G5FAV", "01ARZ3NDEKTSV4RRFFQ69G5FAW", AgentSetupTruthState.Unknown),
            CancellationToken.None);
        result.Status.ShouldBe(AgentSetupWriteStatus.UnableToVerify);
    }

    [Theory]
    [InlineData(AgentOperationErrorCode.NotAuthorized, AgentSetupWriteStatus.NotAuthorized)]
    [InlineData(AgentOperationErrorCode.NotFound, AgentSetupWriteStatus.NotFound)]
    [InlineData(AgentOperationErrorCode.ValidationFailed, AgentSetupWriteStatus.ValidationFailed)]
    [InlineData(AgentOperationErrorCode.Conflict, AgentSetupWriteStatus.Conflict)]
    [InlineData(AgentOperationErrorCode.Stale, AgentSetupWriteStatus.Conflict)]
    [InlineData(AgentOperationErrorCode.Unavailable, AgentSetupWriteStatus.Unavailable)]
    [InlineData(AgentOperationErrorCode.UnableToVerify, AgentSetupWriteStatus.UnableToVerify)]
    [InlineData(AgentOperationErrorCode.Rejected, AgentSetupWriteStatus.Rejected)]
    public async Task A_failed_write_keeps_its_typed_status_and_carries_no_acceptance(
        AgentOperationErrorCode code,
        AgentSetupWriteStatus expected)
    {
        _operations
            .DisableEntryAsync(Arg.Any<DisableProviderModelEntry>(), Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>>(
                AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(code)));

        ProviderCatalogWriteResult result = await Gateway().DisableAsync(
            new DisableProviderModelEntry("openai", "gpt"),
            CancellationToken.None);

        result.Status.ShouldBe(expected);
        result.Acceptance.ShouldBeNull();
    }

    [Theory]
    [InlineData(AgentSetupWriteOutcome.Unknown)]
    [InlineData((AgentSetupWriteOutcome)999)]
    public async Task UnknownSuccessorOutcomeCannotBecomeSubmitted(AgentSetupWriteOutcome outcome)
    {
        _operations.GetCommandWriteOutcomeAsync("system", "message-1", Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentSetupWriteOutcome>>(
                AgentOperationResult<AgentSetupWriteOutcome>.Succeeded(outcome)));
        (await Gateway().GetCommandOutcomeAsync("system", "message-1", CancellationToken.None))
            .ShouldBe(AgentSetupWriteStatus.UnableToVerify);
    }

    [Fact]
    public async Task An_accepted_write_carries_the_servers_acceptance_and_the_submitted_stage()
    {
        var acceptance = new ProviderCatalogCommandAcceptance(
            "openai",
            "gpt",
            "message-1",
            "correlation-1",
            AgentSetupTruthState.Submitted);
        _operations
            .EnableEntryAsync(Arg.Any<EnableProviderModelEntry>(), Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>>(
                AgentOperationResult<ProviderCatalogCommandAcceptance>.Succeeded(acceptance)));

        ProviderCatalogWriteResult result = await Gateway().EnableAsync(
            new EnableProviderModelEntry("openai", "gpt"),
            CancellationToken.None);

        result.Status.ShouldBe(AgentSetupWriteStatus.Submitted);
        result.Acceptance.ShouldBe(acceptance);
    }

    [Fact]
    public async Task Command_outcome_passes_through_a_successful_client_result()
    {
        _operations.GetCommandWriteOutcomeAsync("system", "message-1", Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentSetupWriteOutcome>>(
                AgentOperationResult<AgentSetupWriteOutcome>.Succeeded(AgentSetupWriteOutcome.AwaitingProjection)));

        (await Gateway().GetCommandOutcomeAsync("system", "message-1", CancellationToken.None))
            .ShouldBe(AgentSetupWriteStatus.AwaitingProjection);
    }

    [Theory]
    [InlineData(AgentOperationErrorCode.Unavailable, AgentSetupWriteStatus.UnableToVerify)]
    [InlineData(AgentOperationErrorCode.NotAuthorized, AgentSetupWriteStatus.NotAuthorized)]
    public async Task Failed_command_outcome_read_preserves_pending_safety(
        AgentOperationErrorCode code, AgentSetupWriteStatus expected)
    {
        _operations.GetCommandWriteOutcomeAsync("system", "message-1", Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentSetupWriteOutcome>>(
                AgentOperationResult<AgentSetupWriteOutcome>.Failed(code)));

        (await Gateway().GetCommandOutcomeAsync("system", "message-1", CancellationToken.None))
            .ShouldBe(expected);
    }

    [Fact]
    public async Task Failed_tenant_reads_return_safe_empty_results()
    {
        _operations.ListTenantEntriesAsync(true, Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<TenantProviderCatalogInspectionResult>>(
                AgentOperationResult<TenantProviderCatalogInspectionResult>.Failed(AgentOperationErrorCode.Unavailable)));
        _operations.GetTenantEntryAsync("openai", "gpt", Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<TenantProviderCatalogInspectionResult>>(
                AgentOperationResult<TenantProviderCatalogInspectionResult>.Failed(AgentOperationErrorCode.NotAuthorized)));
        _operations.GetTenantEnablementAsync("tenant", "openai", "gpt", Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<TenantProviderEnablementInspectionResult>>(
                AgentOperationResult<TenantProviderEnablementInspectionResult>.Failed(AgentOperationErrorCode.NotAuthorized)));

        TenantProviderCatalogInspectionResult list = await Gateway().ListTenantEntriesAsync(true, CancellationToken.None);
        TenantProviderCatalogInspectionResult detail = await Gateway().GetTenantEntryAsync("openai", "gpt", CancellationToken.None);
        TenantProviderEnablementInspectionResult enablement = await Gateway().GetTenantEnablementAsync("tenant", "openai", "gpt", CancellationToken.None);

        list.Status.ShouldBe(ProviderCatalogInspectionStatus.Unavailable);
        list.Entries.ShouldBeEmpty();
        detail.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        detail.Entries.ShouldBeEmpty();
        enablement.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        enablement.Enabled.ShouldBeNull();
    }

    private AgentsClientProviderCatalogGateway Gateway() => new(_client);
}
