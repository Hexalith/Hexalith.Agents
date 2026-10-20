using System.Reflection;
using Dapr.Workflow;
using Hexalith.Agents.Server.Ports;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Scheduling preserves the exact runtime owner and never hides an owner that requires recovery.</summary>
public sealed class DaprInteractionWorkflowSchedulerTests
{
    [Theory]
    [InlineData(WorkflowRuntimeStatus.Pending)]
    [InlineData(WorkflowRuntimeStatus.Running)]
    [InlineData(WorkflowRuntimeStatus.Suspended)]
    [InlineData(WorkflowRuntimeStatus.Completed)]
    public async Task ExistingLiveOrCompletedOwnerIsRetained(WorkflowRuntimeStatus status)
    {
        var fixture = new AutomaticInteractionReplayFixture(); var client = Substitute.For<IDaprWorkflowClient>();
        client.GetWorkflowStateAsync(fixture.InteractionId, false, Arg.Any<CancellationToken>()).Returns(State(status));
        await new DaprInteractionWorkflowScheduler(client).EnsureScheduledAsync(fixture.Input, CancellationToken.None);
        await client.DidNotReceive().ScheduleNewWorkflowAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(WorkflowRuntimeStatus.Failed)]
    [InlineData(WorkflowRuntimeStatus.Terminated)]
    [InlineData(WorkflowRuntimeStatus.Canceled)]
    public async Task FailedOwnerRequiresExplicitRecoveryWithoutStartingAnotherOwner(WorkflowRuntimeStatus status)
    {
        var fixture = new AutomaticInteractionReplayFixture(); var client = Substitute.For<IDaprWorkflowClient>();
        client.GetWorkflowStateAsync(fixture.InteractionId, false, Arg.Any<CancellationToken>()).Returns(State(status));
        var scheduler = new DaprInteractionWorkflowScheduler(client);
        for (int n = 0; n < 2; n++)
        {
            var error = await Should.ThrowAsync<InvalidOperationException>(() => scheduler.EnsureScheduledAsync(fixture.Input, CancellationToken.None));
            error.Message.ShouldBe("InteractionWorkflowRecoveryRequired");
        }
        await client.DidNotReceive().ScheduleNewWorkflowAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(WorkflowRuntimeStatus.Running)]
    [InlineData(WorkflowRuntimeStatus.Completed)]
    [InlineData(WorkflowRuntimeStatus.Failed)]
    [InlineData(WorkflowRuntimeStatus.Terminated)]
    public async Task LostAcknowledgementRechecksTheSameOwnerAndDoesNotCreateASecondInstance(WorkflowRuntimeStatus status)
    {
        var fixture = new AutomaticInteractionReplayFixture(); var client = Substitute.For<IDaprWorkflowClient>();
        bool exists = false; var owners = new HashSet<string>();
        client.GetWorkflowStateAsync(fixture.InteractionId, false, Arg.Any<CancellationToken>()).Returns(_ => State(exists ? status : null));
        client.ScheduleNewWorkflowAsync(Arg.Any<string>(), fixture.InteractionId, Arg.Any<object>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(_ => { exists = true; owners.Add(fixture.InteractionId); return Task.FromException<string>(new InvalidOperationException("runtime-error-poison")); });
        var scheduler = new DaprInteractionWorkflowScheduler(client);
        for (int n = 0; n < 2; n++)
        {
            if (status is WorkflowRuntimeStatus.Failed or WorkflowRuntimeStatus.Terminated)
            {
                var error = await Should.ThrowAsync<InvalidOperationException>(() => scheduler.EnsureScheduledAsync(fixture.Input, CancellationToken.None));
                error.Message.ShouldBe("InteractionWorkflowRecoveryRequired"); error.InnerException.ShouldBeNull();
            }
            else { await scheduler.EnsureScheduledAsync(fixture.Input, CancellationToken.None); }
        }
        owners.ShouldBe([fixture.InteractionId]);
        await client.Received(1).ScheduleNewWorkflowAsync(Arg.Any<string>(), fixture.InteractionId, Arg.Any<object>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>());
    }

    private static WorkflowState State(WorkflowRuntimeStatus? status)
    {
        // Dapr exposes immutable snapshots through a non-public wrapper constructor.
        Type metadataType = typeof(WorkflowState).Assembly.GetType("Dapr.Workflow.Client.WorkflowMetadata", true)!;
        object? metadata = status is null ? null : Activator.CreateInstance(metadataType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, ["instance", "AutomaticInteractionWorkflow", status.Value, DateTime.UnixEpoch, DateTime.UnixEpoch, null], null);
        return (WorkflowState)Activator.CreateInstance(typeof(WorkflowState), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [metadata], null)!;
    }
}
