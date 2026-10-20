using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Confirmation Approver resolution is a committed phase and no losing reservation can read the roster.</summary>
public sealed class ApproverResolutionImmediatelyBeforeAndAfterLeaseCommitAndBarrierClosingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClosingCancelsTheLoserAndTheWinningCommitStillSettles(bool commitFirst)
    {
        var fixture = new AutomaticInteractionReplayFixture { Mode = AgentResponseMode.Confirmation }; await fixture.StartAsync(); await fixture.ReserveUntilAsync(ConversationEffectKind.ApproverResolution);
        if (commitFirst) { await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None); }
        await fixture.CloseAsync(); await fixture.CompleteAsync();
        fixture.Effects.Observed.Contains(AutomaticInteractionStep.ApproverResolutionRecorded).ShouldBe(commitFirst);
        fixture.Effects.Memberships.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ApproverResolution).Status.ShouldBe(commitFirst ? ConversationEffectLeaseStatus.Settled : ConversationEffectLeaseStatus.CancelledBeforeCommit);
    }
}
