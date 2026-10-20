using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Allowed output needs an independent mutation commit and loses cleanly to Closing.</summary>
public sealed class GeneratedProposalMutationLeaseTests
{
    [Fact]
    public async Task ClosingBeforeProposalCommitDiscardsGeneratedOutputAndSettlesTheProvider()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync(); await fixture.ReserveUntilAsync(ConversationEffectKind.ProposalMutation);
        await fixture.CloseAsync(); fixture.Restart(); await fixture.CompleteAsync();
        var interaction = fixture.Interaction.ShouldNotBeNull();
        interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBeFalse();
        interaction.AutomaticFacts[AutomaticInteractionStep.GenerationFailed].Outcome.ShouldBe(AutomaticInteractionOutcome.Discarded);
        interaction.AutomaticFacts.Values.ShouldAllBe(f => f.Output == null); fixture.Effects.Posts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProposalMutation).Status.ShouldBe(ConversationEffectLeaseStatus.CancelledBeforeCommit);
        fixture.Directory.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProviderInvocation).Status.ShouldBe(ConversationEffectLeaseStatus.Settled);
    }
}
