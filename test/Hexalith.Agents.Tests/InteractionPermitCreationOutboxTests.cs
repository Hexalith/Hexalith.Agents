using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.Security;
using Hexalith.EventStore.Contracts.Commands;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>The exact protected creation outbox is consumed once; legacy streams and divergent retries remain separate.</summary>
public sealed class InteractionPermitCreationOutboxTests
{
    [Fact]
    public void PermitCreatedFirstEventContainsTheSoleReciprocalOutboxesAndAd29Identities()
    {
        var permit = ConversationDirectoryTestData.Permit();
        var command = new CreatePermittedAgentInteraction(permit, 1, 0);
        var envelope = ConversationDirectoryTestData.Envelope() with { Domain = "agent-interaction", AggregateId = permit.AgentInteractionId };
        var created = AgentInteractionAggregate.Handle(command, null, envelope).Events.Single().ShouldBeOfType<PermittedAgentInteractionCreated>();
        created.WorkflowStartOutboxId.ShouldBe(ConversationAgentStateIdentity.WorkflowStartOutbox(permit.TenantId, permit.AgentInteractionId));
        created.DirectoryAcknowledgementOutboxId.ShouldBe(ConversationAgentStateIdentity.DirectoryAckOutbox(permit.TenantId, permit.AgentInteractionId));
        created.AttemptId.ShouldBe(AgentsSecurityIdentity.Derive("attempt", permit.TenantId, permit.AgentInteractionId, "1"));
        created.ReservationId.ShouldBe(AgentsSecurityIdentity.Derive("reservation", created.AttemptId));
        created.AdmissionId.ShouldBe(AgentsSecurityIdentity.Derive("admission", created.AttemptId));
        created.ProposalVersionId.ShouldBe(AgentsSecurityIdentity.Derive("version", permit.AgentInteractionId, "1", "Generated"));
        created.MessageId.ShouldBe(AgentsSecurityIdentity.Derive("message-id", permit.AgentInteractionId, created.ProposalVersionId));
        var state = new AgentInteractionState(); state.Apply(created);
        state.Prompt.ShouldBeEmpty(); state.GeneratedVersions.ShouldBeNull();
        var restored = JsonSerializer.Deserialize<AgentInteractionState>(JsonSerializer.Serialize(state)).ShouldNotBeNull();
        AgentInteractionAggregate.Handle(command, restored, envelope).IsNoOp.ShouldBeTrue();
        AgentInteractionAggregate.Handle(command with { Permit = permit with { RequestDigest = new string('b', 64) } }, restored, envelope).IsRejection.ShouldBeTrue();
    }
    [Fact]
    public void AnyLegacyStreamCannotBeUpgradedToThePermittedPath()
    {
        var p = ConversationDirectoryTestData.Permit();
        var envelope = ConversationDirectoryTestData.Envelope() with { Domain = "agent-interaction", AggregateId = p.AgentInteractionId };
        AgentInteractionAggregate.Handle(new CreatePermittedAgentInteraction(p, 1, 0), new AgentInteractionState { IsRequested = true }, envelope).IsRejection.ShouldBeTrue();
        AgentInteractionAggregate.Handle(new CreatePermittedAgentInteraction(p, 1, 0), new AgentInteractionState(), envelope).IsRejection.ShouldBeTrue();
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyHandlersCannotAdvancePermittedGeneratedOrClosingSuppressedStreams(bool closingSuppressed)
    {
        var p = ConversationDirectoryTestData.Permit(); var envelope = ConversationDirectoryTestData.Envelope() with { Domain = "agent-interaction", AggregateId = p.AgentInteractionId };
        var state = new AgentInteractionState();
        state.Apply(AgentInteractionAggregate.Handle(new CreatePermittedAgentInteraction(p, 1, 0), null, envelope).Events.Single().ShouldBeOfType<PermittedAgentInteractionCreated>());
        state.Apply(new AgentCallAccepted(new(AutomaticInteractionStep.AgentCallAccepted, new string('a', 64), AutomaticInteractionOutcome.Allowed, 2)));
        state.Apply(new GeneratedVersionRecorded(new(AutomaticInteractionStep.GeneratedVersionRecorded, new string('b', 64), AutomaticInteractionOutcome.Allowed, 3,
            Output: p.Request, ContentDigest: new string('c', 64), DigestKeyVersion: p.DigestKeyVersion)));
        if (closingSuppressed)
        { state.Apply(new ConversationPostingSuppressedByDeletion(new(AutomaticInteractionStep.ConversationPostingSuppressedByDeletion, new string('d', 64), AutomaticInteractionOutcome.Suppressed, 4, DeletionRequestId: new string('e', 64), DirectoryClosingRevision: 17))); }
        string before = JsonSerializer.Serialize(state);
        object[] commands =
        [
            new RequestAgentInteraction(p.AgentId, p.ConversationId, p.CallerPartyId, "legacy-plaintext", p.ClientIdempotencyKey, p.Snapshot),
            new EvaluateAgentInteractionGate(p.AgentInteractionId, []), new BuildAgentInteractionContext(p.AgentInteractionId, null!),
            new GenerateAgentOutput(p.AgentInteractionId, null!),
            new PostAgentResponse(p.AgentInteractionId, new(AgentResponsePostingOutcome.Posted, "message", p.ConversationId, "agent-party", "version")),
            new CreateProposedAgentReply(p.AgentInteractionId, null!),
            new EditProposedAgentReply(p.AgentInteractionId, null!), new RegenerateProposedAgentReply(p.AgentInteractionId, null!),
            new ApproveProposedAgentReply(p.AgentInteractionId, null!), new RejectProposedAgentReply(p.AgentInteractionId, null!),
            new AbandonProposedAgentReply(p.AgentInteractionId, null!), new ExpireProposedAgentReply(p.AgentInteractionId, null!),
        ];
        foreach (object command in commands)
        {
            Hexalith.EventStore.Contracts.Results.DomainResult result = AgentInteractionAggregate.Handle((dynamic)command, state, envelope);
            var rejection = result.Events.Single().ShouldBeOfType<AutomaticInteractionRejected>(); rejection.Reason.ShouldBe(AutomaticInteractionRejectionReason.LegacyStream);
            state.Apply(rejection); JsonSerializer.Serialize(state).ShouldBe(before); state.Status.ShouldBe(AgentInteractionStatus.Generated);
        }
    }

}
