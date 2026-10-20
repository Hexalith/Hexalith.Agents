using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Closing before Provider commit releases only the exact already-admitted identity under no-invocation evidence.</summary>
public sealed class PreProviderCapacityDeletionDispositionTests
{
    [Fact]
    public async Task ClosingRecordsCancelNoInvocationAndSettlesExistingCapacityWithoutProviderWork()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync(); await fixture.ReserveUntilAsync(ConversationEffectKind.ProviderInvocation);
        await fixture.CloseAsync(); await fixture.CompleteAsync();
        fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.PreProviderCapacityDispositionDecided].Outcome.ShouldBe(AutomaticInteractionOutcome.CancelNoInvocation);
        fixture.Interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ProviderInvocationAuthorized).ShouldBeFalse();
        fixture.Effects.Attempts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty(); fixture.Effects.Settlements.ShouldNotBeEmpty();
    }
    [Fact]
    public async Task CapacityFailureRequiresExactNoInvocationEvidenceAndABudgetDecisionBeforeSettlement()
    {
        var fixture = new AutomaticInteractionReplayFixture(); fixture.Effects.FailAt = AutomaticInteractionStep.CapacityAdmissionRecorded;
        await fixture.StartAsync(); await fixture.CompleteAsync();
        fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.BudgetDispositionDecided].Outcome.ShouldBe(AutomaticInteractionOutcome.NotInvoked);
        fixture.Interaction.AutomaticFacts[AutomaticInteractionStep.PreProviderCapacityDispositionDecided].Outcome.ShouldBe(AutomaticInteractionOutcome.CancelNoInvocation);
        fixture.Effects.Preparations.ShouldBeEmpty(); fixture.Effects.Attempts.ShouldBeEmpty();
    }
}
