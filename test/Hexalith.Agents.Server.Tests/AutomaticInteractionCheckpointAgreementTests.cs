using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Application.Workflows;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Public dispatch/Handle/Apply agreement: the checkpoint outcome matches actual owner truth with at most one command.</summary>
public sealed class AutomaticInteractionCheckpointAgreementTests
{
    [Fact]
    public async Task EveryCheckpointReturnsTheRecordedOutcomeAndDispatchesAtMostOneCommand()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        var input = fixture.Input;
        for (int n = 0; n < 128; n++)
        {
            int before = fixture.Dispatches.Count;
            input = await fixture.Steps().ExecuteAsync(input, CancellationToken.None);
            (fixture.Dispatches.Count - before).ShouldBeLessThanOrEqualTo(1);
            var latest = fixture.Interaction!.AutomaticFacts.Values.MaxBy(f => f.Revision);
            input.Outcome.ShouldBe(latest?.Outcome ?? AutomaticInteractionOutcome.Unknown);
            input.EventRevision.ShouldBe(fixture.Interaction.AutomaticRevision);
            if (input.Status != WorkflowExecutionStatus.Running) { input.Status.ShouldBe(WorkflowExecutionStatus.Completed); return; }
        }
        throw new InvalidOperationException("FixtureDidNotConverge");
    }
}
