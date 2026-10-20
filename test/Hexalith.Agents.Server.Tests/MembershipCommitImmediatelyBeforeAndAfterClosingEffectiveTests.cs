using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Only the Closing race is in scope; this suite makes no deletion Effective claim.</summary>
public sealed class MembershipCommitImmediatelyBeforeAndAfterClosingEffectiveTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MembershipReservationLosesOrCommittedMembershipSettlesWithoutLateAcceptance(bool commitFirst)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync(); await fixture.ReserveUntilAsync(ConversationEffectKind.ConversationMembership);
        if (commitFirst) { await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None); }
        await fixture.CloseAsync(); await fixture.CompleteAsync();
        fixture.Effects.Memberships.Count.ShouldBe(commitFirst ? 1 : 0);
        fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.AgentCallAccepted).ShouldBeFalse(); fixture.Effects.Attempts.ShouldBeEmpty();
        fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ConversationMembership).Status.ShouldBe(commitFirst ? ConversationEffectLeaseStatus.Settled : ConversationEffectLeaseStatus.CancelledBeforeCommit);
    }
}
