using System.Text.Json;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.ConversationAgentState;
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
