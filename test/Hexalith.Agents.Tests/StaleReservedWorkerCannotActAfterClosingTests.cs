using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;
using Hexalith.Agents.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>A stale reservation is never reusable authority after Closing.</summary>
public sealed class StaleReservedWorkerCannotActAfterClosingTests
{
    [Fact]
    public void ExactAcquireAndCommitRetriesAreRejectedAfterCancellation()
    {
        var s = ConversationDirectoryTestData.State(); var e = ConversationDirectoryTestData.Envelope(); var l = ConversationDirectoryTestData.Lease();
        ConversationDirectoryTestData.Apply(s, ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(l, s.Revision), s, e));
        ConversationDirectoryTestData.Apply(s, ConversationAgentStateAggregate.Handle(new EnterConversationDeletionClosing("tenant-a", "conversation-a", new string('d', 64), s.Revision), s, ConversationDirectoryTestData.DeletionEnvelope()));
        ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(l, s.Revision), s, e).IsRejection.ShouldBeTrue();
        ConversationAgentStateAggregate.Handle(new CommitConversationEffect(l.LeaseId, l.AgentInteractionId, l.WorkflowInstanceId, s.Revision), s, e).IsRejection.ShouldBeTrue();
        ConversationAgentStateAggregate.Handle(new RegisterInteractionPermit(ConversationDirectoryTestData.Permit(), s.Revision), s, e).IsRejection.ShouldBeTrue();
        s.Leases[l.LeaseId].CommitRevision.ShouldBe(0); s.Permits.Count.ShouldBe(1);
    }
    [Fact]
    public void ChangedLeasePayloadIsAConflictAndAnExactReservationIsNoOp()
    {
        var s = ConversationDirectoryTestData.State(); var e = ConversationDirectoryTestData.Envelope(); var l = ConversationDirectoryTestData.Lease();
        ConversationDirectoryTestData.Apply(s, ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(l, s.Revision), s, e));
        ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(l, 0), s, e).IsNoOp.ShouldBeTrue();
        ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(l with { OperationFamily = "changed" }, s.Revision), s, e)
            .Events.Single().ShouldBeOfType<ConversationDirectoryRejected>().Reason.ShouldBe(ConversationDirectoryRejectionReason.DivergentDuplicate);
    }
    [Theory]
    [InlineData(ConversationEffectLeaseStatus.CommittedToEffect)]
    [InlineData(ConversationEffectLeaseStatus.Settled)]
    [InlineData(ConversationEffectLeaseStatus.CancelledBeforeCommit)]
    [InlineData((ConversationEffectLeaseStatus)999)]
    public void AcquisitionRetriesMustKeepTheOriginalReservedShape(ConversationEffectLeaseStatus status)
    {
        var s = ConversationDirectoryTestData.State(); var e = ConversationDirectoryTestData.Envelope(); var l = ConversationDirectoryTestData.Lease();
        ConversationDirectoryTestData.Apply(s, ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(l, s.Revision), s, e));
        ConversationDirectoryTestData.Apply(s, ConversationAgentStateAggregate.Handle(new CommitConversationEffect(l.LeaseId, l.AgentInteractionId, l.WorkflowInstanceId, s.Revision), s, e));
        var before = s.Leases[l.LeaseId];
        ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(l with { Status = status }, s.Revision), s, e)
            .Events.Single().ShouldBeOfType<ConversationDirectoryRejected>().Reason.ShouldBe(ConversationDirectoryRejectionReason.InvalidLease);
        s.Leases[l.LeaseId].ShouldBe(before);
        ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(l, 0), s, e).IsNoOp.ShouldBeTrue();
    }

}
