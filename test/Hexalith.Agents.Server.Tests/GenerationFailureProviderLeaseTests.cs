using System.Text.Json;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Failed generation has only safe failure evidence, no version/content, and a settled Provider lease.</summary>
public sealed class GenerationFailureProviderLeaseTests
{
    [Fact]
    public async Task FailureDoesNotInventAVersionOrRetainIncompleteOutput()
    {
        var fixture = new AutomaticInteractionReplayFixture(); fixture.Effects.FailAt = AutomaticInteractionStep.GeneratedVersionRecorded;
        await fixture.StartAsync(); await fixture.CompleteAsync(); fixture.Restart();
        var state = fixture.Interaction.ShouldNotBeNull(); state.AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.GenerationFailed);
        state.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBeFalse(); state.GeneratedVersions.ShouldBeNull();
        state.AutomaticFacts.Values.ShouldAllBe(f => f.Output == null); JsonSerializer.Serialize(state).Contains("allowed-output", StringComparison.Ordinal).ShouldBeFalse();
        fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProviderInvocation).Status.ShouldBe(ConversationEffectLeaseStatus.Settled);
        fixture.Directory.Leases.Values.Any(l => l.Kind == ConversationEffectKind.ProposalMutation).ShouldBeFalse(); fixture.Effects.Posts.ShouldBeEmpty();
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AFailedLookupAfterMutationCommitRecordsItsExactNegativeResultAndSettlesBothLeases(bool restart)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        await fixture.ReserveUntilAsync(ConversationEffectKind.ProposalMutation);
        fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBeGreaterThan(0);
        await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None);
        var mutation = fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProposalMutation);
        mutation.Status.ShouldBe(ConversationEffectLeaseStatus.CommittedToEffect);
        fixture.Effects.FailAt = AutomaticInteractionStep.GeneratedVersionRecorded;
        if (restart) { fixture.Restart(); }
        await fixture.CompleteAsync(); fixture.Restart(); await fixture.CompleteAsync();
        var failure = fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.GenerationFailed];
        failure.LeaseId.ShouldBe(mutation.LeaseId); failure.LeaseCommitRevision.ShouldBe(mutation.CommitRevision);
        fixture.Interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBeFalse();
        fixture.Interaction.AutomaticFacts.Values.ShouldAllBe(f => f.Output == null);
        fixture.Directory!.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled || l.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit);
        fixture.Effects.Preparations.ShouldBeEmpty(); fixture.Effects.Posts.ShouldBeEmpty();
    }

}
