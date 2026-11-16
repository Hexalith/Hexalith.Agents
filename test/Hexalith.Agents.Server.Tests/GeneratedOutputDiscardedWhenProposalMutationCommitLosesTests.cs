using System.Text.Json;
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
        var fixture = new AutomaticInteractionReplayFixture { Mode = AgentResponseMode.Confirmation }; await fixture.StartAsync();
        await fixture.PauseGenerationAsync(afterCommit: commitFirst);
        fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBeFalse();
        fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProposalMutation).Status.ShouldBe(
            commitFirst ? ConversationEffectLeaseStatus.CommittedToEffect : ConversationEffectLeaseStatus.Reserved);
        var mutation = fixture.Directory.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProposalMutation);
        await fixture.CloseAsync(); fixture.Restart(); await fixture.CompleteAsync();
        string? recorded = commitFirst ? JsonSerializer.Serialize(fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded]) : null;
        fixture.Restart();
        if (commitFirst)
        {
            var generated = fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded];
            generated.LeaseId.ShouldBe(mutation.LeaseId); generated.LeaseCommitRevision.ShouldBe(mutation.CommitRevision);
            JsonSerializer.Serialize(generated).ShouldBe(recorded);
        }
        fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(commitFirst);
        fixture.Effects.Posts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProposalMutation).Status.ShouldBe(commitFirst ? ConversationEffectLeaseStatus.Settled : ConversationEffectLeaseStatus.CancelledBeforeCommit);
    }
}
