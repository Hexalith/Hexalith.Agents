using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Server.Application.Workflows;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Every pre-acceptance dependency failure preserves the permit and settles all preparations without membership.</summary>
public sealed class Fr8RateApproverContextBudgetAndSafetyFailuresCreateNoMembershipEffectTests
{
    [Theory]
    [InlineData(AutomaticInteractionStep.RateAdmissionAuthorized)]
    [InlineData(AutomaticInteractionStep.RateAdmissionRecorded)]
    [InlineData(AutomaticInteractionStep.OpenInteractionLeaseRecorded)]
    [InlineData(AutomaticInteractionStep.ApproverResolutionRecorded)]
    [InlineData(AutomaticInteractionStep.ContextReadRecorded)]
    [InlineData(AutomaticInteractionStep.BudgetReservationRecorded)]
    [InlineData(AutomaticInteractionStep.PromptSafetyRecorded)]
    public async Task TypedFailureSettlesAndNeverAcquiresMembership(AutomaticInteractionStep failure)
    {
        var fixture = new AutomaticInteractionReplayFixture { Mode = AgentResponseMode.Confirmation }; fixture.Effects.FailAt = failure;
        await fixture.StartAsync(); (await fixture.CompleteAsync()).Status.ShouldBe(WorkflowExecutionStatus.Failed);
        fixture.Effects.Memberships.ShouldBeEmpty(); fixture.Effects.Attempts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Directory.ShouldNotBeNull().Permits.Count.ShouldBe(1);
        fixture.Directory.Leases.Values.ShouldAllBe(l => l.Kind != ConversationEffectKind.ConversationMembership);
        fixture.Directory.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled || l.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit);
        fixture.Interaction.ShouldNotBeNull().AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.InteractionPreAcceptanceFailed);
        fixture.Interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.AgentCallAccepted).ShouldBeFalse();
    }
}
