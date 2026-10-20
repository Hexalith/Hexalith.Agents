using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;
using Hexalith.Agents.Contracts.Security;
using Hexalith.EventStore.Contracts.Commands;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>Post suppression is a terminal no-effect decision tied to Closing, never a posting capability.</summary>
public sealed class PostingSuppressionDecisionTests
{
    [Fact]
    public void ClosingBoundSuppressionIsIdempotentAndRejectsAnyLaterPostingResult()
    {
        var (state, envelope) = AcceptedGenerated();
        var generated = JsonSerializer.Serialize(state.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded]);
        var fact = Suppression(state);
        var command = new RecordAutomaticInteractionStep(envelope.TenantId, state.AgentInteractionId, fact, state.AutomaticRevision);
        var result = AgentInteractionAggregate.Handle(command, state, envelope);
        state.Apply(result.Events.Single().ShouldBeOfType<ConversationPostingSuppressedByDeletion>());
        JsonSerializer.Serialize(state.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded]).ShouldBe(generated);
        state.Status.ShouldBe(AgentInteractionStatus.Generated);
        AgentInteractionAggregate.Handle(command, state, envelope).IsNoOp.ShouldBeTrue();
        AgentInteractionAggregate.Handle(command with { Fact = fact with { DeletionRequestId = new string('e', 64) } }, state, envelope)
            .Events.Single().ShouldBeOfType<AutomaticInteractionRejected>().Reason.ShouldBe(AutomaticInteractionRejectionReason.DivergentDuplicate);
        var posting = fact with { Step = AutomaticInteractionStep.ConversationPostingRecorded,
            OperationId = AgentsSecurityIdentity.Derive("interaction-step", envelope.TenantId, state.AgentInteractionId, AutomaticInteractionStep.ConversationPostingRecorded.ToString()),
            Outcome = AutomaticInteractionOutcome.Posted, Revision = state.AutomaticRevision + 1, DeletionRequestId = null, DirectoryClosingRevision = 0 };
        AgentInteractionAggregate.Handle(new RecordAutomaticInteractionStep(envelope.TenantId, state.AgentInteractionId, posting, state.AutomaticRevision), state, envelope)
            .Events.Single().ShouldBeOfType<AutomaticInteractionRejected>().Reason.ShouldBe(AutomaticInteractionRejectionReason.Terminal);
    }

    [Theory]
    [InlineData("missing-request")]
    [InlineData("missing-revision")]
    [InlineData("committed-post")]
    [InlineData("wrong-lease")]
    [InlineData("content")]
    public void SuppressionRequiresClosingEvidenceAndCannotCarryEffectAuthorityOrOutput(string invalid)
    {
        var (state, envelope) = AcceptedGenerated();
        var fact = Suppression(state);
        fact = invalid switch
        {
            "missing-request" => fact with { DeletionRequestId = null },
            "missing-revision" => fact with { DirectoryClosingRevision = 0 },
            "committed-post" => fact with { LeaseCommitRevision = 7 },
            "wrong-lease" => fact with { LeaseId = new string('f', 64) },
            _ => fact with { Output = state.PermittedCreation!.Permit.Request },
        };
        AgentInteractionAggregate.Handle(new RecordAutomaticInteractionStep(envelope.TenantId, state.AgentInteractionId, fact, state.AutomaticRevision), state, envelope).IsRejection.ShouldBeTrue();
        state.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ConversationPostingSuppressedByDeletion).ShouldBeFalse();
    }

    private static AutomaticInteractionFact Suppression(AgentInteractionState state) => new(AutomaticInteractionStep.ConversationPostingSuppressedByDeletion,
        AgentsSecurityIdentity.Derive("interaction-step", state.PermittedCreation!.Permit.TenantId, state.AgentInteractionId, AutomaticInteractionStep.ConversationPostingSuppressedByDeletion.ToString()),
        AutomaticInteractionOutcome.Suppressed, state.AutomaticRevision + 1, DeletionRequestId: new string('d', 64), DirectoryClosingRevision: 17);

    private static (AgentInteractionState State, CommandEnvelope Envelope) AcceptedGenerated()
    {
        var p = ConversationDirectoryTestData.Permit();
        var envelope = ConversationDirectoryTestData.Envelope() with { Domain = "agent-interaction", AggregateId = p.AgentInteractionId };
        var created = AgentInteractionAggregate.Handle(new CreatePermittedAgentInteraction(p, 1, 0), null, envelope).Events.Single().ShouldBeOfType<PermittedAgentInteractionCreated>();
        var state = new AgentInteractionState(); state.Apply(created);
        state.Apply(new AgentCallAccepted(new(AutomaticInteractionStep.AgentCallAccepted, new string('a', 64), AutomaticInteractionOutcome.Allowed, 2)));
        state.Apply(new GeneratedVersionRecorded(new(AutomaticInteractionStep.GeneratedVersionRecorded, new string('b', 64), AutomaticInteractionOutcome.Allowed, 3,
            Output: p.Request, ContentDigest: new string('c', 64), DigestKeyVersion: p.DigestKeyVersion)));
        return (state, envelope);
    }
}
