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
        _operations.GetCommandOutcomeAsync("system", "message-1", Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentSetupWriteStatus>>(
                AgentOperationResult<AgentSetupWriteStatus>.Succeeded(AgentSetupWriteStatus.AwaitingProjection)));

        (await Gateway().GetCommandOutcomeAsync("system", "message-1", CancellationToken.None))
            .ShouldBe(AgentSetupWriteStatus.AwaitingProjection);
    }

    [Theory]
    [InlineData(AgentOperationErrorCode.Unavailable, AgentSetupWriteStatus.UnableToVerify)]
    [InlineData(AgentOperationErrorCode.NotAuthorized, AgentSetupWriteStatus.NotAuthorized)]
    public async Task Failed_command_outcome_read_preserves_pending_safety(
        AgentOperationErrorCode code, AgentSetupWriteStatus expected)
    {
        _operations.GetCommandOutcomeAsync("system", "message-1", Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentSetupWriteStatus>>(
                AgentOperationResult<AgentSetupWriteStatus>.Failed(code)));

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
