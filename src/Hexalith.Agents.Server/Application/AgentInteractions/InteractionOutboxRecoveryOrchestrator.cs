using System.Text.Json;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Application.Workflows;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>Platform-callable recovery from EventStore outboxes; never a worker, timer or alternate durable owner.</summary>
public sealed class InteractionOutboxRecoveryOrchestrator(IConversationAgentStateReader directoryReader, IAgentInteractionStateReader interactionReader,
    IAgentCommandDispatcher dispatcher, IInteractionWorkflowScheduler scheduler)
{
    /// <summary>Re-drives creation, acknowledgement and WorkflowStart using exact owner reads after every command.</summary>
    public Task<AutomaticInteractionIntakeResult> RecoverAsync(string tenantId, string conversationId, string interactionId, CancellationToken ct)
        => RecoverAsync(tenantId, conversationId, interactionId, ct, expectedPermit: null);

    /// <summary>Intake-only recovery also verifies the exact submitted protected intent before consuming its outboxes.</summary>
    internal async Task<AutomaticInteractionIntakeResult> RecoverAsync(string tenantId, string conversationId, string interactionId, CancellationToken ct, InteractionPermit? expectedPermit, long submittedPermitRevision = 0)
    {
        ct.ThrowIfCancellationRequested();
        if (!WorkflowExecutionStateValidation.IsOpaqueId(tenantId) || !WorkflowExecutionStateValidation.IsOpaqueId(conversationId)) { return new(AutomaticInteractionIntakeStatus.NotFound); }
        string? lastObservation = null;
        for (int checkpoint = 0; checkpoint < 64; checkpoint++)
        {
            ct.ThrowIfCancellationRequested();
            var directory = await directoryReader.ReadAsync(tenantId, conversationId, ct).ConfigureAwait(false);
            if (directory is not null && directory.Revision != 0 && (directory.TenantId != tenantId || directory.ConversationId != conversationId)) { return new(AutomaticInteractionIntakeStatus.NotFound); }
            if (directory is null || !directory.Permits.TryGetValue(interactionId, out var permit))
            {
                if (expectedPermit is null) { return new(AutomaticInteractionIntakeStatus.NotFound); }
                if (directory?.DeletionRequestId is not null) { return new(AutomaticInteractionIntakeStatus.ConversationClosing); }
                // A newer owner revision needs a fresh submission identity; unchanged visibility only needs observation.
                if (directory is not null && directory.Revision > submittedPermitRevision)
                {
                    submittedPermitRevision = directory.Revision;
                    await InteractionCommandDispatch.SendAsync(dispatcher, tenantId, ConversationAgentStateIdentity.Domain,
                        ConversationAgentStateIdentity.Directory(tenantId, conversationId), expectedPermit.PermitId,
                        new RegisterInteractionPermit(expectedPermit, submittedPermitRevision), ct).ConfigureAwait(false);
                    await ObserveAsync().ConfigureAwait(false);
                    continue;
                }
                await ObserveAsync().ConfigureAwait(false); continue;
            }
            // Accepted gateway receipts can precede a rejection or append. Only this exact recorded intent may succeed.
            if (expectedPermit is not null && JsonSerializer.Serialize(permit.Permit) != JsonSerializer.Serialize(expectedPermit)) { return new(AutomaticInteractionIntakeStatus.Conflict, interactionId); }
            var interaction = await interactionReader.ReadAsync(tenantId, interactionId, ct).ConfigureAwait(false);
            string observation = $"{directory.Revision}:{interaction?.AutomaticRevision ?? 0}";
            if (observation == lastObservation) { await ObserveAsync().ConfigureAwait(false); continue; }
            lastObservation = observation;
            string directoryId = ConversationAgentStateIdentity.Directory(tenantId, conversationId);
            if (interaction?.PermittedCreation is null)
            {
                if (directory.DeletionRequestId is not null) { return new(AutomaticInteractionIntakeStatus.StartSuppressedByDeletion, interactionId); }
                await InteractionCommandDispatch.SendAsync(dispatcher, tenantId, "agent-interaction", interactionId, permit.OutboxId,
                    new CreatePermittedAgentInteraction(permit.Permit, permit.Revision, 0), ct).ConfigureAwait(false);
                continue;
            }
            var creation = interaction.PermittedCreation;
            if (JsonSerializer.Serialize(creation.Permit) != JsonSerializer.Serialize(permit.Permit) || creation.PermitRevision != permit.Revision) { return new(AutomaticInteractionIntakeStatus.Conflict, interactionId); }
            if (!directory.Acknowledgements.ContainsKey(interactionId))
            {
                await InteractionCommandDispatch.SendAsync(dispatcher, tenantId, ConversationAgentStateIdentity.Domain, directoryId, creation.DirectoryAcknowledgementOutboxId,
                    new AcknowledgeInteractionCreation(interactionId, permit.Permit.PermitId, permit.OutboxId, creation.Revision, directory.Revision), ct).ConfigureAwait(false);
                continue;
            }
            string operationId = AgentsSecurityIdentity.Derive("interaction-step", tenantId, interactionId, AutomaticInteractionStep.WorkflowStartCheckpoint.ToString());
            string leaseId = ConversationAgentStateIdentity.Lease(tenantId, conversationId, interactionId, ConversationEffectKind.WorkflowStart, AutomaticInteractionStep.WorkflowStartCheckpoint.ToString(), operationId);
            directory.Leases.TryGetValue(leaseId, out var lease);
            if (lease?.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit || directory.DeletionRequestId is not null && lease?.CommitRevision is not > 0)
            {
                if (!interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.StartSuppressedByDeletion))
                {
                    var fact = new AutomaticInteractionFact(AutomaticInteractionStep.StartSuppressedByDeletion,
                        AgentsSecurityIdentity.Derive("interaction-step", tenantId, interactionId, AutomaticInteractionStep.StartSuppressedByDeletion.ToString()), AutomaticInteractionOutcome.Suppressed, interaction.AutomaticRevision + 1);
                    await InteractionCommandDispatch.SendAsync(dispatcher, tenantId, "agent-interaction", interactionId, fact.OperationId, new RecordAutomaticInteractionStep(tenantId, interactionId, fact, interaction.AutomaticRevision), ct).ConfigureAwait(false);
                    continue;
                }
                return new(AutomaticInteractionIntakeStatus.StartSuppressedByDeletion, interactionId);
            }
            if (lease is null)
            {
                lease = new(leaseId, interactionId, ConversationEffectKind.WorkflowStart, AutomaticInteractionStep.WorkflowStartCheckpoint.ToString(), operationId, interactionId,
                    permit.Permit.DirectoryEpoch.EpochId, ConversationEffectLeaseStatus.Reserved, 0, 0, false);
                await InteractionCommandDispatch.SendAsync(dispatcher, tenantId, ConversationAgentStateIdentity.Domain, directoryId, leaseId,
                    new AcquireConversationEffectLease(lease, directory.Revision), ct).ConfigureAwait(false); continue;
            }
            if (lease.Status == ConversationEffectLeaseStatus.Reserved)
            {
                await InteractionCommandDispatch.SendAsync(dispatcher, tenantId, ConversationAgentStateIdentity.Domain, directoryId, leaseId,
                    new CommitConversationEffect(leaseId, interactionId, interactionId, directory.Revision), ct).ConfigureAwait(false); continue;
            }
            var input = new WorkflowExecutionState(tenantId, conversationId, interactionId, new ProtectedContentReference(tenantId, interactionId, creation.Revision, permit.Permit.RequestDigest),
                interaction.AutomaticRevision, WorkflowExecutionStatus.Running);
            try { await scheduler.EnsureScheduledAsync(input, ct).ConfigureAwait(false); }
            catch (InvalidOperationException exception) when (exception.Message == "InteractionWorkflowRecoveryRequired")
            { ct.ThrowIfCancellationRequested(); return new(AutomaticInteractionIntakeStatus.RecoveryRequired, interactionId); }
            return new(AutomaticInteractionIntakeStatus.Scheduled, interactionId);

            Task ObserveAsync() => Task.Delay(TimeSpan.FromMilliseconds(Math.Min(200, 25 * (checkpoint + 1))), ct);
        }
        return new(AutomaticInteractionIntakeStatus.RecoveryRequired, interactionId);
    }
}
