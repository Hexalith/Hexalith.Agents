using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Exactly one phase-pinned branch precedes Provider work; Open never chooses a local status.</summary>
public sealed class ProviderOutputSafetyDecisionPhasePinTests
{
    [Fact]
    public async Task MissingAuthorityRecordsNotInvokedDispositionsAndSettlesProviderWithoutCallingIt()
    {
        var fixture = new AutomaticInteractionReplayFixture(); fixture.Effects.AuthorityAvailable = false;
        await fixture.StartAsync(); await fixture.CompleteAsync();
        var interaction = fixture.Interaction.ShouldNotBeNull(); interaction.AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.ProviderInvocationNotAuthorized);
        interaction.AutomaticFacts[AutomaticInteractionStep.BudgetDispositionDecided].Outcome.ShouldBe(AutomaticInteractionOutcome.NotInvoked);
        interaction.AutomaticFacts[AutomaticInteractionStep.PreProviderCapacityDispositionDecided].Outcome.ShouldBe(AutomaticInteractionOutcome.CancelNoInvocation);
        interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ProviderInvocationAuthorized).ShouldBeFalse();
        fixture.Effects.Attempts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Directory.ShouldNotBeNull().Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProviderInvocation).Status.ShouldBe(ConversationEffectLeaseStatus.Settled);
    }
    [Fact]
    public async Task RecordedAuthorityIsNeverReReadWhenItsSuccessorChanges()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        for (int i = 0; i < 100 && !fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ProviderInvocationAuthorized); i++) { await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None); }
        var pinned = fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationAuthorized].OutputAuthority.ShouldNotBeNull();
        fixture.Effects.AuthorityAvailable = false; fixture.Restart(); await fixture.CompleteAsync();
        fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationAuthorized].OutputAuthority.ShouldBe(pinned);
        fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.ProviderInvocationAuthorized).ShouldBe(1); fixture.Effects.Attempts.Count.ShouldBe(1);
    }
}
