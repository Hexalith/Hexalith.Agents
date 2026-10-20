using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>The exact mutation commit chooses whether an already allowed output may become a durable version.</summary>
public sealed class GeneratedOutputDiscardedWhenProposalMutationCommitLosesTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnlyTheWinningMutationCommitCanPersistOutputAfterClosing(bool commitFirst)
    {
        var fixture = new AutomaticInteractionReplayFixture { Mode = AgentResponseMode.Confirmation }; await fixture.StartAsync(); await fixture.ReserveUntilAsync(ConversationEffectKind.ProposalMutation);
        if (commitFirst) { await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None); }
        await fixture.CloseAsync(); await fixture.CompleteAsync();
        fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(commitFirst);
        fixture.Effects.Posts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProposalMutation).Status.ShouldBe(commitFirst ? ConversationEffectLeaseStatus.Settled : ConversationEffectLeaseStatus.CancelledBeforeCommit);
    }
}
