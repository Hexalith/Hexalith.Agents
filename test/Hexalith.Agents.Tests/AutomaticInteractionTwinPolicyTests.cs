using System.Text.Json;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>The domain twin-policy truth table pins ordered atomic branch/disposition events and the returned safe decision.</summary>
public sealed class AutomaticInteractionTwinPolicyTests
{
    [Theory]
    [InlineData(AutomaticInteractionStep.ContextReadRecorded, AutomaticInteractionOutcome.Allowed, 1)]
    [InlineData(AutomaticInteractionStep.ContextReadRecorded, AutomaticInteractionOutcome.Blocked, 1)]
    [InlineData(AutomaticInteractionStep.ContextReadRecorded, AutomaticInteractionOutcome.DependencyNotAvailable, 1)]
    [InlineData(AutomaticInteractionStep.ProviderInvocationAuthorized, AutomaticInteractionOutcome.Authorized, 2)]
    [InlineData(AutomaticInteractionStep.ProviderInvocationNotAuthorized, AutomaticInteractionOutcome.Blocked, 2)]
    [InlineData(AutomaticInteractionStep.ProviderInvocationNotAuthorized, AutomaticInteractionOutcome.DependencyNotAvailable, 2)]
    [InlineData(AutomaticInteractionStep.InteractionPreAcceptanceFailed, AutomaticInteractionOutcome.Blocked, 2)]
    [InlineData(AutomaticInteractionStep.InteractionPreAcceptanceFailed, AutomaticInteractionOutcome.Suppressed, 2)]
    [InlineData(AutomaticInteractionStep.PreProviderCapacityDispositionDecided, AutomaticInteractionOutcome.CancelNoInvocation, 2)]
    [InlineData(AutomaticInteractionStep.GenerationFailed, AutomaticInteractionOutcome.Failed, 1)]
    [InlineData(AutomaticInteractionStep.GenerationFailed, AutomaticInteractionOutcome.Discarded, 1)]
    [InlineData(AutomaticInteractionStep.ConversationPostingSuppressedByDeletion, AutomaticInteractionOutcome.Suppressed, 1)]
    public void EvaluateAndDecideShareTheSameDecisionAndOrderedDispositionSequence(AutomaticInteractionStep step, AutomaticInteractionOutcome outcome, int count)
    {
        var fact = new AutomaticInteractionFact(step, new string('a', 64), outcome, 10);
        var events = AutomaticInteractionPolicy.Evaluate(fact, budgetDispositionRecorded: false);
        AutomaticInteractionPolicy.Decide(fact).ShouldBe(outcome); events.Count.ShouldBe(count); events[0].GetType().Name.ShouldBe(step.ToString());
        if (count >= 2)
        {
            var budget = events[1].ShouldBeOfType<BudgetDispositionDecided>(); budget.Fact.Revision.ShouldBe(11);
            budget.Fact.Outcome.ShouldBe(step == AutomaticInteractionStep.ProviderInvocationAuthorized ? AutomaticInteractionOutcome.InvocationSettlement : AutomaticInteractionOutcome.NotInvoked);
        }
    }

    [Fact]
    public void CapacityContinuationDoesNotEmitASecondBudgetDisposition()
    {
        var fact = new AutomaticInteractionFact(
            AutomaticInteractionStep.PreProviderCapacityDispositionDecided, new string('a', 64),
            AutomaticInteractionOutcome.CancelNoInvocation, 12, DependencyRecordId: "exact-allocator-record");

        AutomaticInteractionPolicy.Evaluate(fact, budgetDispositionRecorded: true).ShouldHaveSingleItem()
            .ShouldBe(new PreProviderCapacityDispositionDecided(fact));
    }

    [Fact]
    public void HistoricalUntypedRefusalAndCapacityDispositionStillReplay()
    {
        const string refusalJson = """{"Fact":{"Step":"ProviderInvocationNotAuthorized","OperationId":"legacy","Outcome":"Blocked","Revision":10}}""";
        const string budgetJson = """{"Fact":{"Step":"BudgetDispositionDecided","OperationId":"legacy","Outcome":"NotInvoked","Revision":11}}""";
        const string capacityJson = """{"Fact":{"Step":"PreProviderCapacityDispositionDecided","OperationId":"legacy","Outcome":"CancelNoInvocation","Revision":12}}""";
        var refusal = JsonSerializer.Deserialize<ProviderInvocationNotAuthorized>(refusalJson).ShouldNotBeNull();
        var state = new AgentInteractionState();
        state.Apply(refusal);
        state.Apply(JsonSerializer.Deserialize<BudgetDispositionDecided>(budgetJson).ShouldNotBeNull());
        state.Apply(JsonSerializer.Deserialize<PreProviderCapacityDispositionDecided>(capacityJson).ShouldNotBeNull());

        state.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationNotAuthorized]
            .OutputAuthorityRefusal.ShouldBeNull();
        JsonSerializer.Serialize(refusal).ShouldNotContain(nameof(AutomaticInteractionFact.OutputAuthorityRefusal));
        state.Status.ShouldBe(AgentInteractionStatus.Blocked);
        state.AutomaticRevision.ShouldBe(12);
    }
}
