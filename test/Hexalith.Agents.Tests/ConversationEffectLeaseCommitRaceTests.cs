using System.Text.Json;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;
using Hexalith.Agents.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>Closing and commit compare the same revision, so only one can authorize the effect.</summary>
public sealed class ConversationEffectLeaseCommitRaceTests
{
    [Theory]
    [InlineData(ConversationEffectKind.WorkflowStart)]
    [InlineData(ConversationEffectKind.ApproverResolution)]
    [InlineData(ConversationEffectKind.ContextRead)]
    [InlineData(ConversationEffectKind.ConversationMembership)]
    [InlineData(ConversationEffectKind.ProviderInvocation)]
    [InlineData(ConversationEffectKind.ProposalMutation)]
    [InlineData(ConversationEffectKind.ConversationPosting)]
    public void ClosingWinsOrCommitWinsWithoutRevokingCommittedWork(ConversationEffectKind kind)
    {
        var e = ConversationDirectoryTestData.Envelope(); var s = ConversationDirectoryTestData.State(); var l = ConversationDirectoryTestData.Lease(kind);
        ConversationDirectoryTestData.Apply(s, ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(l, s.Revision), s, e));
        long expected = s.Revision;
        var closingWinner = JsonSerializer.Deserialize<ConversationAgentStateState>(JsonSerializer.Serialize(s)).ShouldNotBeNull();
        ConversationDirectoryTestData.Apply(closingWinner, ConversationAgentStateAggregate.Handle(new EnterConversationDeletionClosing("tenant-a", "conversation-a", new string('d', 64), expected), closingWinner, ConversationDirectoryTestData.DeletionEnvelope()));
        closingWinner.Leases[l.LeaseId].Status.ShouldBe(ConversationEffectLeaseStatus.CancelledBeforeCommit);
        ConversationAgentStateAggregate.Handle(new CommitConversationEffect(l.LeaseId, l.AgentInteractionId, l.WorkflowInstanceId, expected), closingWinner, e).IsRejection.ShouldBeTrue();
        ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(l, expected), closingWinner, e).IsRejection.ShouldBeTrue();
        ConversationDirectoryTestData.Apply(s, ConversationAgentStateAggregate.Handle(new CommitConversationEffect(l.LeaseId, l.AgentInteractionId, l.WorkflowInstanceId, expected), s, e));
        long committed = s.Leases[l.LeaseId].CommitRevision; committed.ShouldBeGreaterThan(0);
        ConversationDirectoryTestData.Apply(s, ConversationAgentStateAggregate.Handle(new EnterConversationDeletionClosing("tenant-a", "conversation-a", new string('d', 64), s.Revision), s, ConversationDirectoryTestData.DeletionEnvelope()));
        s.Leases[l.LeaseId].MustSettle.ShouldBeTrue(); s.Leases[l.LeaseId].CommitRevision.ShouldBe(committed);
        ConversationAgentStateAggregate.Handle(new CancelConversationEffectLease(l.LeaseId, l.AgentInteractionId, l.WorkflowInstanceId, s.Revision), s, e).IsRejection.ShouldBeTrue();
        ConversationDirectoryTestData.Apply(s, ConversationAgentStateAggregate.Handle(new SettleConversationEffectLease(l.LeaseId, l.AgentInteractionId, l.WorkflowInstanceId, committed, 7, s.Revision), s, e));
        s.Leases[l.LeaseId].Status.ShouldBe(ConversationEffectLeaseStatus.Settled); s.Leases[l.LeaseId].MustSettle.ShouldBeFalse();
    }
    [Fact]
    public void ReservationAndWrongCommitRevisionNeverSettleAnEffect()
    {
        var e = ConversationDirectoryTestData.Envelope(); var s = ConversationDirectoryTestData.State(); var l = ConversationDirectoryTestData.Lease();
        ConversationDirectoryTestData.Apply(s, ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(l, s.Revision), s, e));
        s.Leases[l.LeaseId].CommitRevision.ShouldBe(0);
        ConversationAgentStateAggregate.Handle(new SettleConversationEffectLease(l.LeaseId, l.AgentInteractionId, l.WorkflowInstanceId, 99, 1, s.Revision), s, e).IsRejection.ShouldBeTrue();
        ConversationAgentStateAggregate.Handle(new CommitConversationEffect(l.LeaseId, l.AgentInteractionId, "wrong-owner", s.Revision), s, e).IsRejection.ShouldBeTrue();
    }
}
