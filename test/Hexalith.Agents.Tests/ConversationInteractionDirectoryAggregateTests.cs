using System.Text.Json;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.ConversationAgentState;
using Hexalith.EventStore.Contracts.Commands;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>Directory permits are durable independently of projections and exact retry preserves the original intent.</summary>
public sealed class ConversationInteractionDirectoryAggregateTests
{
    [Fact]
    public void ExactRetryIsNoOpAndChangedProtectedIntentConflicts()
    {
        var p = ConversationDirectoryTestData.Permit(); var state = ConversationDirectoryTestData.State();
        ConversationAgentStateAggregate.Handle(new RegisterInteractionPermit(p, 0), state, ConversationDirectoryTestData.Envelope()).IsNoOp.ShouldBeTrue();
        var result = ConversationAgentStateAggregate.Handle(new RegisterInteractionPermit(p with { RequestDigest = new string('b', 64) }, state.Revision), state, ConversationDirectoryTestData.Envelope());
        result.IsRejection.ShouldBeTrue(); result.Events.Single().ShouldBeOfType<ConversationDirectoryRejected>().Reason.ShouldBe(ConversationDirectoryRejectionReason.DivergentDuplicate);
        var restored = JsonSerializer.Deserialize<ConversationAgentStateState>(JsonSerializer.Serialize(state)).ShouldNotBeNull();
        restored.Permits.ShouldContainKey(p.AgentInteractionId); restored.PermitHighWater.ShouldBe(1); restored.Acknowledgements.ShouldBeEmpty();
    }
    [Fact]
    public void DirectoryRequiresAQualifiedReadinessEpochAndExactExpectedRevision()
    {
        var p = ConversationDirectoryTestData.Permit();
        ConversationAgentStateAggregate.Handle(new RegisterInteractionPermit(p with { DirectoryEpoch = null! }, 0), null, ConversationDirectoryTestData.Envelope())
            .Events.Single().ShouldBeOfType<ConversationDirectoryRejected>().Reason.ShouldBe(ConversationDirectoryRejectionReason.DirectoryNotReady);
        ConversationAgentStateAggregate.Handle(new RegisterInteractionPermit(p, 1), null, ConversationDirectoryTestData.Envelope())
            .Events.Single().ShouldBeOfType<ConversationDirectoryRejected>().Reason.ShouldBe(ConversationDirectoryRejectionReason.StaleRevision);
    }
    [Fact]
    public void AcknowledgementCannotRetargetThePermitOrChangeCreationRevision()
    {
        var p = ConversationDirectoryTestData.Permit(); var s = ConversationDirectoryTestData.State();
        var c = new AcknowledgeInteractionCreation(p.AgentInteractionId, p.PermitId, p.CreationOutboxId, 1, s.Revision);
        ConversationDirectoryTestData.Apply(s, ConversationAgentStateAggregate.Handle(c, s, ConversationDirectoryTestData.Envelope()));
        ConversationAgentStateAggregate.Handle(c, s, ConversationDirectoryTestData.Envelope()).IsNoOp.ShouldBeTrue();
        ConversationAgentStateAggregate.Handle(c with { InteractionCreationRevision = 2 }, s, ConversationDirectoryTestData.Envelope()).IsRejection.ShouldBeTrue();
        s.Permits.Count.ShouldBe(1); s.Acknowledgements.Count.ShouldBe(1);
    }
    [Fact]
    public async Task TwoWorkflowInstancesContendingForOneDirectoryRetainIndependentLeases()
    {
        var first = ConversationDirectoryTestData.Permit();
        var second = ConversationDirectoryTestData.Permit("client-b");
        var directory = new ConversationAgentStateState();
        var aggregate = new ConversationAgentStateAggregate();

        static CommandEnvelope Envelope<T>(T command, string messageId) where T : notnull
            => ConversationDirectoryTestData.Envelope() with
            {
                MessageId = messageId,
                CommandType = typeof(T).Name,
                Payload = JsonSerializer.SerializeToUtf8Bytes(command),
            };

        async Task Append(CommandEnvelope command)
        {
            var result = await aggregate.ProcessAsync(command, directory);
            result.IsSuccess.ShouldBeTrue();
            ConversationDirectoryTestData.Apply(directory, result);
        }

        await Append(Envelope(new RegisterInteractionPermit(first, directory.Revision), "permit-a"));
        await Append(Envelope(new RegisterInteractionPermit(second, directory.Revision), "permit-b"));

        // Both workflows read the same directory revision before either reservation arrives.
        long observed = directory.Revision;
        var firstLease = ConversationDirectoryTestData.Lease(permit: first);
        var secondLease = ConversationDirectoryTestData.Lease(permit: second);
        var contender = new AcquireConversationEffectLease(secondLease, observed);
        await Append(Envelope(new AcquireConversationEffectLease(firstLease, observed), "acquire-a"));
        var stale = await aggregate.ProcessAsync(Envelope(contender, "acquire-b"), directory);
        stale.Events.Single().ShouldBeOfType<ConversationDirectoryRejected>().Reason
            .ShouldBe(ConversationDirectoryRejectionReason.StaleRevision);
        ConversationDirectoryTestData.Apply(directory, stale);
        directory.Leases.Keys.ShouldBe([firstLease.LeaseId]);
        directory.Revision.ShouldBe(observed + 1);

        var refreshed = Envelope(contender with { ExpectedRevision = directory.Revision }, "acquire-b-refreshed");
        await Append(refreshed);
        (await aggregate.ProcessAsync(refreshed, directory)).IsNoOp.ShouldBeTrue();

        var commit = new CommitConversationEffect(secondLease.LeaseId, second.AgentInteractionId, second.AgentInteractionId, directory.Revision);
        await Append(Envelope(commit, "commit-b"));
        directory.PermitHighWater.ShouldBe(2);
        directory.Leases.Count.ShouldBe(2);
        directory.Leases[firstLease.LeaseId].ShouldBe(firstLease);
        directory.Leases[secondLease.LeaseId].ShouldBe(secondLease with
        {
            Status = ConversationEffectLeaseStatus.CommittedToEffect,
            CommitRevision = observed + 3,
        });
    }

    [Theory]
    [InlineData("unknown-mode")]
    [InlineData("undefined-mode")]
    [InlineData("agent")]
    [InlineData("party")]
    [InlineData("key")]
    public void UnconsumablePermitsAreRejectedBeforeCreatingOutboxes(string invalid)
    {
        var p = ConversationDirectoryTestData.Permit();
        p = invalid switch
        {
            "unknown-mode" => p with { Snapshot = p.Snapshot with { ResponseMode = Hexalith.Agents.Contracts.Agent.AgentResponseMode.Unknown } },
            "undefined-mode" => p with { Snapshot = p.Snapshot with { ResponseMode = (Hexalith.Agents.Contracts.Agent.AgentResponseMode)999 } },
            "agent" => p with { AgentId = " " },
            "party" => p with { CallerPartyId = " " },
            _ => p with { ClientIdempotencyKey = " " },
        };
        string id = Hexalith.Agents.Contracts.Security.ConversationAgentStateIdentity.Interaction(p.TenantId, p.AgentId, p.ConversationId, p.CallerPartyId, p.ClientIdempotencyKey);
        p = p with { AgentInteractionId = id, PermitId = Hexalith.Agents.Contracts.Security.ConversationAgentStateIdentity.Permit(p.TenantId, id),
            CreationOutboxId = Hexalith.Agents.Contracts.Security.ConversationAgentStateIdentity.CreationOutbox(p.TenantId, id), Request = p.Request with { Target = p.Request.Target with { AgentInteractionId = id } } };
        var result = ConversationAgentStateAggregate.Handle(new RegisterInteractionPermit(p, 0), null, ConversationDirectoryTestData.Envelope());
        result.IsRejection.ShouldBeTrue(); result.Events.ShouldAllBe(e => e is ConversationDirectoryRejected);
    }

}
