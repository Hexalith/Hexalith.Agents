using Dapr.Workflow;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Application.Activities;
using Hexalith.Agents.Server.Application.Workflows;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>The real workflow invokes only content-free activity contracts in the authoritative order.</summary>
public sealed class AutomaticInteractionWorkflowTests
{
    [Theory]
    [InlineData(AgentResponseMode.Automatic)]
    [InlineData(AgentResponseMode.Confirmation)]
    public async Task SingleOwnerCallsActivitiesInFr8OrderAndConfirmationStopsAfterVersion(AgentResponseMode mode)
    {
        var fixture = new AutomaticInteractionReplayFixture { Mode = mode }; await fixture.StartAsync();
        WorkflowContext context = Substitute.For<WorkflowContext>(); context.InstanceId.Returns(fixture.InteractionId);
        context.CallActivityAsync<WorkflowExecutionState>(nameof(ExecuteAutomaticInteractionStepActivity), Arg.Any<WorkflowExecutionState>(), Arg.Any<WorkflowTaskOptions>())
            .Returns(call => fixture.Steps().ExecuteAsync((WorkflowExecutionState)call[1], CancellationToken.None));
        var result = await new AutomaticInteractionWorkflow().RunAsync(context, fixture.Input);
        result.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        var observed = fixture.Effects.Observed;
        observed.IndexOf(AutomaticInteractionStep.RateAdmissionRecorded).ShouldBeLessThan(observed.IndexOf(AutomaticInteractionStep.ContextReadRecorded));
        observed.IndexOf(AutomaticInteractionStep.ContextReadRecorded).ShouldBeLessThan(observed.IndexOf(AutomaticInteractionStep.BudgetReservationRecorded));
        observed.IndexOf(AutomaticInteractionStep.BudgetReservationRecorded).ShouldBeLessThan(observed.IndexOf(AutomaticInteractionStep.PromptSafetyRecorded));
        observed.IndexOf(AutomaticInteractionStep.PromptSafetyRecorded).ShouldBeLessThan(observed.IndexOf(AutomaticInteractionStep.ConversationMembershipRecorded));
        observed.Contains(AutomaticInteractionStep.ApproverResolutionRecorded).ShouldBe(mode == AgentResponseMode.Confirmation);
        if (mode == AgentResponseMode.Confirmation)
        {
            observed.IndexOf(AutomaticInteractionStep.ApproverResolutionRecorded).ShouldBeLessThan(observed.IndexOf(AutomaticInteractionStep.ContextReadRecorded));
            fixture.Effects.Posts.ShouldBeEmpty();
        }
        else { fixture.Effects.Posts.Count.ShouldBe(1); }
        fixture.Interaction.ShouldNotBeNull().AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.AgentCallAccepted);
    }
    [Fact]
    public async Task WorkflowInstanceMustExactlyMatchTheInteraction()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        WorkflowContext context = Substitute.For<WorkflowContext>(); context.InstanceId.Returns(new string('b', 64));
        await Should.ThrowAsync<ArgumentException>(() => new AutomaticInteractionWorkflow().RunAsync(context, fixture.Input));
        context.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "CallActivityAsync").ShouldBe(0);
    }
}
