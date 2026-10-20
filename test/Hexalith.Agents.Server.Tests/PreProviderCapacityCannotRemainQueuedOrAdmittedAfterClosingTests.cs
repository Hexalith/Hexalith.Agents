using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>The exact admitted capacity identity settles whether the generic Provider commit won or lost.</summary>
public sealed class PreProviderCapacityCannotRemainQueuedOrAdmittedAfterClosingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClosingSettlesExistingCapacityAndNeverInvokesProvider(bool commitFirst)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync(); await fixture.ReserveUntilAsync(ConversationEffectKind.ProviderInvocation);
        if (commitFirst) { await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None); }
        await fixture.CloseAsync(); fixture.Restart(); await fixture.CompleteAsync();
        fixture.Effects.Preparations.ShouldBeEmpty(); fixture.Effects.Attempts.ShouldBeEmpty();
        fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.PreProviderCapacityDispositionDecided].Outcome.ShouldBe(AutomaticInteractionOutcome.CancelNoInvocation);
        fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProviderInvocation).Status.ShouldBe(commitFirst ? ConversationEffectLeaseStatus.Settled : ConversationEffectLeaseStatus.CancelledBeforeCommit);
    }
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ClosingAroundCapacityAuthorizationCancelsTheAcceptedAttemptAndRecovers(bool capacityAuthorized, bool restart)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        while (!fixture.Interaction!.AutomaticFacts.ContainsKey(capacityAuthorized ? AutomaticInteractionStep.CapacityAdmissionAuthorized : AutomaticInteractionStep.AgentCallAccepted))
        { await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None); }
        fixture.Interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.CapacityAdmissionRecorded).ShouldBeFalse();
        await fixture.CloseAsync(); if (restart) { fixture.Restart(); }
        await fixture.CompleteAsync(); fixture.Restart(); await fixture.CompleteAsync();
        fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.PreProviderCapacityDispositionDecided].Outcome.ShouldBe(AutomaticInteractionOutcome.CancelNoInvocation);
        fixture.Interaction.AutomaticFacts[AutomaticInteractionStep.BudgetDispositionDecided].Outcome.ShouldBe(AutomaticInteractionOutcome.NotInvoked);
        fixture.Effects.Preparations.ShouldBeEmpty(); fixture.Effects.Attempts.ShouldBeEmpty(); fixture.Effects.Posts.ShouldBeEmpty();
        fixture.Effects.Observed.Contains(AutomaticInteractionStep.CapacityAdmissionRecorded).ShouldBeFalse();
        fixture.Effects.Observed.Contains(AutomaticInteractionStep.ProviderInvocationAuthorized).ShouldBeFalse();
        fixture.Directory!.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled || l.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit);
    }

    [Fact]
    public async Task ClosingCannotRecordCancellationWithoutTheAuthoritativeNoInvocationDecision()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        while (!fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.AgentCallAccepted))
        { await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None); }
        await fixture.CloseAsync(); fixture.Effects.FailAt = AutomaticInteractionStep.PreProviderCapacityDispositionDecided;
        var error = await Should.ThrowAsync<NotSupportedException>(() => fixture.CompleteAsync());
        error.Message.ShouldBe("AuthoritativeNoInvocationEvidenceRequired");
        fixture.Interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.PreProviderCapacityDispositionDecided).ShouldBeFalse();
    }

}
