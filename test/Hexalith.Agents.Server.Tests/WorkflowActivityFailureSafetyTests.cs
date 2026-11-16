using Dapr.Workflow;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Server.Application.Activities;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Application.Workflows;
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
        var logger = new AutomaticInteractionActivityTestLogger();
        var orchestrator = new AutomaticInteractionStepOrchestrator(fixture, reader, fixture, fixture.Effects);
        var error = await Should.ThrowAsync<AutomaticInteractionCheckpointUnavailableException>(() =>
            new ExecuteAutomaticInteractionStepActivity(orchestrator, logger).RunAsync(Substitute.For<WorkflowActivityContext>(), fixture.Input));
        error.Message.ShouldBe("InteractionCheckpointUnavailable"); error.InnerException.ShouldBeNull();
        error.ToString().Contains("provider-payload-poison", StringComparison.Ordinal).ShouldBeFalse(); fixture.Dispatches.ShouldBeEmpty();
        AssertSafeLog(logger, fixture.InteractionId, cancellation ? nameof(OperationCanceledException) : nameof(InvalidOperationException));
    }

    [Theory]
    [InlineData("provider-payload-poison")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData(null)]
    public async Task InvalidInputsLogOnlyAPlaceholder(string? interactionId)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        var logger = new AutomaticInteractionActivityTestLogger();
        WorkflowExecutionState input = interactionId is null ? null! : fixture.Input with { AgentInteractionId = interactionId };
        var error = await Should.ThrowAsync<AutomaticInteractionCheckpointUnavailableException>(() =>
            new ExecuteAutomaticInteractionStepActivity(fixture.Steps(), logger).RunAsync(Substitute.For<WorkflowActivityContext>(), input));
        error.Message.ShouldBe("InteractionCheckpointUnavailable"); error.InnerException.ShouldBeNull();
        AssertSafeLog(logger, "invalid-interaction-id", interactionId is null ? nameof(ArgumentNullException) : nameof(ArgumentException));
        fixture.Dispatches.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DiagnosticSinkFailuresAlsoBecomeOnlyASafeRetryCode(bool whileEnabling)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        var logger = new AutomaticInteractionActivityTestLogger { ThrowWhileEnabling = whileEnabling, ThrowWhileLogging = !whileEnabling };
        var error = await Should.ThrowAsync<AutomaticInteractionCheckpointUnavailableException>(() =>
            new ExecuteAutomaticInteractionStepActivity(fixture.Steps(), logger).RunAsync(Substitute.For<WorkflowActivityContext>(), fixture.Input));
        error.Message.ShouldBe("InteractionCheckpointUnavailable"); error.InnerException.ShouldBeNull();
        error.ToString().ShouldNotContain("logger-payload-poison"); fixture.Dispatches.ShouldBeEmpty();
    }

    private static void AssertSafeLog(AutomaticInteractionActivityTestLogger logger, string interactionId, string exceptionType)
    {
        logger.Count.ShouldBe(1); logger.Failure.ShouldBeNull();
        var values = logger.Fields.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        values.ShouldContainKey("ExceptionType"); values["ExceptionType"].ShouldBe(exceptionType); values["AgentInteractionId"].ShouldBe(interactionId);
        values.Count.ShouldBe(3); // Only the two safe values and the generated format template.
        logger.Message.ShouldNotBeNull().ShouldNotContain("provider-payload-poison");
    }
}
