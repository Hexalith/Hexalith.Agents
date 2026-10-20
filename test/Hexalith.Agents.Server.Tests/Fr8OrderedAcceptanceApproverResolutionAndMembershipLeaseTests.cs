using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>The real accepted-event revision comes after steps 5–9 and committed step-10 membership.</summary>
public sealed class Fr8OrderedAcceptanceApproverResolutionAndMembershipLeaseTests
{
    [Theory]
    [InlineData(AgentResponseMode.Automatic)]
    [InlineData(AgentResponseMode.Confirmation)]
    public async Task RecordedAcceptanceFollowsEveryOrderedPreparationAndMembershipResult(AgentResponseMode mode)
    {
        var fixture = new AutomaticInteractionReplayFixture { Mode = mode }; await fixture.StartAsync(); await fixture.CompleteAsync();
        var facts = fixture.Interaction!.AutomaticFacts;
        var sequence = new[] { AutomaticInteractionStep.RateAdmissionRecorded, AutomaticInteractionStep.OpenInteractionLeaseRecorded, AutomaticInteractionStep.ContextReadRecorded,
            AutomaticInteractionStep.BudgetReservationRecorded, AutomaticInteractionStep.PromptSafetyRecorded, AutomaticInteractionStep.ConversationMembershipRecorded, AutomaticInteractionStep.AgentCallAccepted };
        for (int i = 1; i < sequence.Length; i++) { facts[sequence[i]].Revision.ShouldBeGreaterThan(facts[sequence[i - 1]].Revision); }
        var membership = fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ConversationMembership);
        membership.CommitRevision.ShouldBeGreaterThan(0); facts[AutomaticInteractionStep.ConversationMembershipRecorded].LeaseCommitRevision.ShouldBe(membership.CommitRevision);
        membership.Status.ShouldBe(ConversationEffectLeaseStatus.Settled);
        facts.ContainsKey(AutomaticInteractionStep.ApproverResolutionRecorded).ShouldBe(mode == AgentResponseMode.Confirmation);
    }
}
