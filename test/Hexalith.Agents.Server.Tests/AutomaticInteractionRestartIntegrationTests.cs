using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Server.Application.Workflows;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Level-2 failure injection against real Handle/Apply event folds; no Level-4/5 or recovery-time claim.</summary>
public sealed class AutomaticInteractionRestartIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CrashBeforeOrAfterEveryCommandCheckpointConvergesToTheIdenticalPersistedEndState(bool afterAppend)
    {
        var baseline = new AutomaticInteractionReplayFixture(); await baseline.StartAsync();
        (await baseline.CompleteAsync()).Status.ShouldBe(WorkflowExecutionStatus.Completed);
        string expected = baseline.Snapshot(); int checkpoints = baseline.Dispatches.Count;
        checkpoints.ShouldBeGreaterThan(20);
        for (int fault = 0; fault < checkpoints; fault++)
        {
            var run = new AutomaticInteractionReplayFixture { FaultAt = fault, CrashAfterAppend = afterAppend };
            try { await run.StartAsync(); await run.CompleteAsync(); }
            catch (AutomaticInteractionInjectedCrash) { run.Restart(); }
            await run.StartAsync();
            (await run.CompleteAsync()).Status.ShouldBe(WorkflowExecutionStatus.Completed);
            run.Snapshot().ShouldBe(expected, $"Checkpoint {fault}, afterAppend={afterAppend}");
            run.AcceptedCommandIds.Distinct(StringComparer.Ordinal).Count().ShouldBe(run.AcceptedCommandIds.Count);
            run.Scheduled.Count.ShouldBe(1); run.Effects.Attempts.Count.ShouldBe(1); run.Effects.Posts.Count.ShouldBe(1);
            run.Effects.Preparations.ShouldBeEmpty();
            run.Directory.ShouldNotBeNull().Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled);
        }
    }
}
