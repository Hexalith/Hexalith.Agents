using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.ConversationAgentState;
using Hexalith.EventStore.Contracts.Results;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>Every public automatic handler requires its own admitted actor authority and preserves accepted replay state on refusal.</summary>
public sealed class InteractionCommandAuthorityTests
{
    [Theory]
    [InlineData(nameof(RegisterInteractionPermit))]
    [InlineData(nameof(AcknowledgeInteractionCreation))]
    [InlineData(nameof(AcquireConversationEffectLease))]
    [InlineData(nameof(CommitConversationEffect))]
    [InlineData(nameof(SettleConversationEffectLease))]
    [InlineData(nameof(CancelConversationEffectLease))]
    [InlineData(nameof(EnterConversationDeletionClosing))]
    [InlineData(nameof(CreatePermittedAgentInteraction))]
    [InlineData(nameof(RecordAutomaticInteractionStep))]
    public void CorrectAuthorityAcceptsWhileMissingAndOtherActorKeysRejectWithoutBusinessEffects(string commandType)
    {
        foreach (string authority in new[] { "correct", "missing", "foreign" })
        {
            var p = ConversationDirectoryTestData.Permit();
            bool interaction = commandType is nameof(CreatePermittedAgentInteraction) or nameof(RecordAutomaticInteractionStep);
            var envelope = interaction ? ConversationDirectoryTestData.InteractionEnvelope() : ConversationDirectoryTestData.Envelope();
            var directory = commandType == nameof(RegisterInteractionPermit) ? new ConversationAgentStateState() : ConversationDirectoryTestData.State();
            var phase = ConversationDirectoryTestData.PhasePrefix(AutomaticInteractionStep.WorkflowStartCheckpoint);
            var state = commandType == nameof(CreatePermittedAgentInteraction) ? new AgentInteractionState() : phase.State;
            var lease = ConversationDirectoryTestData.Lease();
            if (commandType is nameof(CommitConversationEffect) or nameof(SettleConversationEffectLease) or nameof(CancelConversationEffectLease))
            {
                var acquired = ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(lease, directory.Revision), directory, envelope);
                acquired.IsRejection.ShouldBeFalse(); ConversationDirectoryTestData.Apply(directory, acquired);
                if (commandType == nameof(SettleConversationEffectLease))
                {
                    var committed = ConversationAgentStateAggregate.Handle(new CommitConversationEffect(lease.LeaseId, p.AgentInteractionId, p.AgentInteractionId, directory.Revision), directory, envelope);
                    committed.IsRejection.ShouldBeFalse(); ConversationDirectoryTestData.Apply(directory, committed);
                    lease = directory.Leases[lease.LeaseId];
                }
            }
            object command = commandType switch
            {
                nameof(RegisterInteractionPermit) => new RegisterInteractionPermit(p, directory.Revision),
                nameof(AcknowledgeInteractionCreation) => new AcknowledgeInteractionCreation(p.AgentInteractionId, p.PermitId, p.CreationOutboxId, 1, directory.Revision),
                nameof(AcquireConversationEffectLease) => new AcquireConversationEffectLease(lease, directory.Revision),
                nameof(CommitConversationEffect) => new CommitConversationEffect(lease.LeaseId, p.AgentInteractionId, p.AgentInteractionId, directory.Revision),
                nameof(SettleConversationEffectLease) => new SettleConversationEffectLease(lease.LeaseId, p.AgentInteractionId, p.AgentInteractionId, lease.CommitRevision, 2, directory.Revision),
                nameof(CancelConversationEffectLease) => new CancelConversationEffectLease(lease.LeaseId, p.AgentInteractionId, p.AgentInteractionId, directory.Revision),
                nameof(EnterConversationDeletionClosing) => new EnterConversationDeletionClosing(p.TenantId, p.ConversationId, new string('e', 64), directory.Revision),
                nameof(CreatePermittedAgentInteraction) => new CreatePermittedAgentInteraction(p, 1, 0),
                _ => new RecordAutomaticInteractionStep(p.TenantId, p.AgentInteractionId, phase.Next, state.AutomaticRevision),
            };
            string correct = commandType == nameof(EnterConversationDeletionClosing) ? ConversationAgentStateIdentity.DeletionAuthority : ConversationAgentStateIdentity.WorkflowAuthority;
            string foreign = correct == ConversationAgentStateIdentity.WorkflowAuthority ? ConversationAgentStateIdentity.DeletionAuthority : ConversationAgentStateIdentity.WorkflowAuthority;
            envelope = envelope with { Extensions = authority == "missing" ? null : new Dictionary<string, string>(StringComparer.Ordinal) { [authority == "correct" ? correct : foreign] = "true" } };
            string before = interaction ? JsonSerializer.Serialize(state) : JsonSerializer.Serialize(directory);
            DomainResult result = interaction ? AgentInteractionAggregate.Handle((dynamic)command, state, envelope) : ConversationAgentStateAggregate.Handle((dynamic)command, directory, envelope);
            if (authority == "correct")
            {
                result.IsRejection.ShouldBeFalse(); result.IsNoOp.ShouldBeFalse(); result.Events.ShouldNotBeEmpty();
            }
            else
            {
                result.IsRejection.ShouldBeTrue(); result.Events.Count.ShouldBe(1);
                if (interaction) { result.Events.Single().ShouldBeOfType<AutomaticInteractionRejected>().Reason.ShouldBe(AutomaticInteractionRejectionReason.InvalidTarget); }
                else
                {
                    var reason = commandType is nameof(CommitConversationEffect) or nameof(SettleConversationEffectLease) or nameof(CancelConversationEffectLease)
                        ? ConversationDirectoryRejectionReason.LeaseNotFound : ConversationDirectoryRejectionReason.InvalidTarget;
                    result.Events.Single().ShouldBeOfType<ConversationDirectoryRejected>().Reason.ShouldBe(reason);
                }
            }
            foreach (var item in result.Events)
            {
                if (interaction) { state.Apply((dynamic)item); }
                else { directory.Apply((dynamic)item); }
            }
            string after = interaction ? JsonSerializer.Serialize(state) : JsonSerializer.Serialize(directory);
            if (authority == "correct") { after.ShouldNotBe(before); }
            else { after.ShouldBe(before); }
            if (interaction)
            {
                var replayed = JsonSerializer.Deserialize<AgentInteractionState>(after).ShouldNotBeNull();
                JsonSerializer.Serialize(replayed).ShouldBe(after);
            }
            else
            {
                var replayed = JsonSerializer.Deserialize<ConversationAgentStateState>(after).ShouldNotBeNull();
                JsonSerializer.Serialize(replayed).ShouldBe(after);
            }
        }
    }
}
