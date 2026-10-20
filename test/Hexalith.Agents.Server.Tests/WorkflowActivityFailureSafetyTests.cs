using Dapr.Workflow;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Server.Application.Activities;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>The activity boundary never sends a raw dependency error into Dapr failure history.</summary>
public sealed class WorkflowActivityFailureSafetyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawDependencyExceptionBecomesOnlyASafeRetryCode(bool cancellation)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        var reader = Substitute.For<IAgentInteractionStateReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AgentInteractionState?>(cancellation ? new OperationCanceledException("provider-payload-poison") : new InvalidOperationException("provider-payload-poison")));
        var orchestrator = new AutomaticInteractionStepOrchestrator(fixture, reader, fixture, fixture.Effects);
        var error = await Should.ThrowAsync<AutomaticInteractionCheckpointUnavailableException>(() =>
            new ExecuteAutomaticInteractionStepActivity(orchestrator).RunAsync(Substitute.For<WorkflowActivityContext>(), fixture.Input));
        error.Message.ShouldBe("InteractionCheckpointUnavailable"); error.InnerException.ShouldBeNull();
        error.ToString().Contains("provider-payload-poison", StringComparison.Ordinal).ShouldBeFalse(); fixture.Dispatches.ShouldBeEmpty();
    }
}
