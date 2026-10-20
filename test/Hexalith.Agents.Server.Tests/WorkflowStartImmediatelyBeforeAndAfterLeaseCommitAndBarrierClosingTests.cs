using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Server.Ports;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Closing cancels an uncommitted workflow start; a committed start can only checkpoint and settle.</summary>
public sealed class WorkflowStartImmediatelyBeforeAndAfterLeaseCommitAndBarrierClosingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommitAndClosingHaveOneWinnerEvenWhenTheAppendAcknowledgementIsLost(bool commitFirst)
    {
        var fixture = new AutomaticInteractionReplayFixture { FaultAt = commitFirst ? 4 : 3, CrashAfterAppend = true };
        await Should.ThrowAsync<AutomaticInteractionInjectedCrash>(() => fixture.StartAsync()); fixture.Restart(); await fixture.CloseAsync();
        var result = await fixture.Recovery().RecoverAsync(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, CancellationToken.None);
        result.Status.ShouldBe(commitFirst ? AutomaticInteractionIntakeStatus.Scheduled : AutomaticInteractionIntakeStatus.StartSuppressedByDeletion);
        if (commitFirst) { await fixture.CompleteAsync(); }
        fixture.Scheduled.Count.ShouldBe(commitFirst ? 1 : 0); fixture.Effects.Attempts.ShouldBeEmpty(); fixture.Effects.Memberships.ShouldBeEmpty();
        fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.WorkflowStart).Status.ShouldBe(commitFirst ? ConversationEffectLeaseStatus.Settled : ConversationEffectLeaseStatus.CancelledBeforeCommit);
        fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.StartSuppressedByDeletion).ShouldBe(!commitFirst);
    }
}
