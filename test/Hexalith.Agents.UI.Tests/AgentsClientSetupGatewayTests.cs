using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.UI.Services.Gateways;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// The live FrontComposer setup gateway (Story 5.2 AC2, AC4). It is a thin, honest translation of the public client:
/// it forwards the version the page is waiting for, keeps each failure's own meaning instead of collapsing everything
/// into a denial, and never fabricates Agent state on a failed read.
/// </summary>
public sealed class AgentsClientSetupGatewayTests
{
    private const string AgentId = "hexa";

    private readonly IAgentsClient _client = Substitute.For<IAgentsClient>();
    private readonly IAgentAdministrationOperations _administration = Substitute.For<IAgentAdministrationOperations>();

    public AgentsClientSetupGatewayTests() => _client.AgentAdministration.Returns(_administration);

    [Fact]
    public async Task The_expected_configuration_version_is_forwarded_to_the_public_read()
    {
        GivenRead(AgentOperationResult<AgentSetupResult>.Succeeded(AgentUiTestData.SetupResult()));

        _ = await Gateway().GetSetupAsync(expectedConfigurationVersion: 7, CancellationToken.None);

        // Without the version the server cannot tell "projection confirmed" from "still catching up".
        await _administration.Received(1).GetConfigurationAsync(AgentId, 7, Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_successful_read_passes_the_servers_setup_truth_through_untouched()
    {
        AgentSetupResult served = AgentUiTestData.SetupResult(AgentUiTestData.Setup(configurationVersion: 4));
        GivenRead(AgentOperationResult<AgentSetupResult>.Succeeded(served));

        AgentSetupResult result = await Gateway().GetSetupAsync(null, CancellationToken.None);

        result.ShouldBeSameAs(served);
    }

    [Fact]
    public async Task A_not_found_operation_reads_as_not_found_rather_than_denied()
    {
        GivenRead(AgentOperationResult<AgentSetupResult>.Failed(AgentOperationErrorCode.NotFound));

        AgentSetupResult result = await Gateway().GetSetupAsync(null, CancellationToken.None);

        result.Status.ShouldBe(AgentInspectionStatus.AgentNotFound);
        result.Setup.ShouldBeNull();
    }

    [Fact]
    public async Task A_not_authorized_operation_reads_as_denied()
    {
        GivenRead(AgentOperationResult<AgentSetupResult>.Failed(AgentOperationErrorCode.NotAuthorized));

        AgentSetupResult result = await Gateway().GetSetupAsync(null, CancellationToken.None);

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        result.Setup.ShouldBeNull();
    }

    [Theory]
    [InlineData(AgentOperationErrorCode.Unavailable)]
    [InlineData(AgentOperationErrorCode.ValidationFailed)]
    [InlineData(AgentOperationErrorCode.Conflict)]
    [InlineData(AgentOperationErrorCode.Blocked)]
    public async Task Any_other_failure_reads_as_unavailable_and_carries_no_agent_state(AgentOperationErrorCode code)
    {
        GivenRead(AgentOperationResult<AgentSetupResult>.Failed(code));

        AgentSetupResult result = await Gateway().GetSetupAsync(null, CancellationToken.None);

        // A transport or processing failure is neither a denial nor evidence that the Agent is missing.
        result.Status.ShouldBe(AgentInspectionStatus.Unavailable);
        result.Setup.ShouldBeNull();
    }

    [Fact]
    public async Task A_host_with_no_configured_agent_reads_as_unavailable_without_calling_the_client()
    {
        AgentSetupResult result = await Gateway(agentId: string.Empty).GetSetupAsync(null, CancellationToken.None);

        result.Status.ShouldBe(AgentInspectionStatus.Unavailable);
        await _administration.DidNotReceiveWithAnyArgs().GetConfigurationAsync(default!, default, default, default);
    }

    [Fact]
    public async Task An_accepted_write_carries_the_servers_acceptance_and_the_submitted_stage()
    {
        var acceptance = new AgentCommandAcceptance(AgentId, "message-1", "correlation-1", AgentSetupTruthState.Submitted);
        _administration
            .ActivateAsync(AgentId, Arg.Any<ActivateAgent>(), Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentCommandAcceptance>>(
                AgentOperationResult<AgentCommandAcceptance>.Succeeded(acceptance)));

        AgentSetupWriteResult result = await Gateway().ActivateAsync(CancellationToken.None);

        result.Status.ShouldBe(AgentSetupWriteStatus.Submitted);
        result.Acceptance.ShouldBe(acceptance);
        result.TruthState.ShouldBe(AgentSetupTruthState.Submitted);
    }

    [Theory]
    [InlineData(AgentOperationErrorCode.NotAuthorized, AgentSetupWriteStatus.NotAuthorized)]
    [InlineData(AgentOperationErrorCode.NotFound, AgentSetupWriteStatus.NotFound)]
    [InlineData(AgentOperationErrorCode.ValidationFailed, AgentSetupWriteStatus.ValidationFailed)]
    [InlineData(AgentOperationErrorCode.Conflict, AgentSetupWriteStatus.Conflict)]
    [InlineData(AgentOperationErrorCode.Unavailable, AgentSetupWriteStatus.Unavailable)]
    public async Task A_failed_write_keeps_its_typed_status_and_carries_no_acceptance(
        AgentOperationErrorCode code,
        AgentSetupWriteStatus expected)
    {
        _administration
            .UpdateConfigurationAsync(AgentId, Arg.Any<UpdateAgentConfiguration>(), Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentCommandAcceptance>>(
                AgentOperationResult<AgentCommandAcceptance>.Failed(code)));

        AgentSetupWriteResult result = await Gateway().UpdateConfigurationAsync(
            new UpdateAgentConfiguration("hexa", null, "instructions long enough to be valid"),
            CancellationToken.None);

        result.Status.ShouldBe(expected);
        result.Acceptance.ShouldBeNull();
    }

    [Fact]
    public async Task A_host_with_no_configured_agent_never_attempts_a_write()
    {
        AgentSetupWriteResult result = await Gateway(agentId: string.Empty).DisableAsync(CancellationToken.None);

        result.Status.ShouldBe(AgentSetupWriteStatus.Unavailable);
        await _administration.DidNotReceiveWithAnyArgs().DisableAsync(default!, default!, default, default);
    }

    private void GivenRead(AgentOperationResult<AgentSetupResult> result)
        => _administration
            .GetConfigurationAsync(AgentId, Arg.Any<int?>(), Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentSetupResult>>(result));

    private AgentsClientSetupGateway Gateway(string agentId = AgentId)
        => new(_client, Options.Create(new AgentSetupTargetOptions { AgentId = agentId }));
}
