using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>An empty-directory Closing is a durable barrier before any interaction can be registered.</summary>
public sealed class ConversationDeletionBarrierRejectsBeforePermitInteractionWorkflowAndProviderTests
{
    [Fact]
    public void EmptyDirectoryClosingPreventsTheFirstPermit()
    {
        var e = ConversationDirectoryTestData.Envelope(); var state = new ConversationAgentStateState();
        ConversationDirectoryTestData.Apply(state, ConversationAgentStateAggregate.Handle(new EnterConversationDeletionClosing("tenant-a", "conversation-a", new string('d', 64), 0), null, ConversationDirectoryTestData.DeletionEnvelope()));
        state.Permits.ShouldBeEmpty(); state.Leases.ShouldBeEmpty();
        ConversationAgentStateAggregate.Handle(new RegisterInteractionPermit(ConversationDirectoryTestData.Permit(), state.Revision), state, e).IsRejection.ShouldBeTrue();
        state.Permits.ShouldBeEmpty(); state.PermitHighWater.ShouldBe(0);
    }
}
