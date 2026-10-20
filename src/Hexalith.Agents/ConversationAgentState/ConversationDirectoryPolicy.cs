using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Agents.ConversationAgentState;

/// <summary>Pure twin mapping after authoritative aggregate guards; one Compute owns every emitted directory decision.</summary>
internal static class ConversationDirectoryPolicy
{
    /// <summary>Emits one accepted command's atomic directory decision.</summary>
    internal static IReadOnlyList<IEventPayload> Evaluate(object command, ConversationAgentStateState? state) => Compute(command, state).Events;
    /// <summary>Reports the same accepted command's safe decision.</summary>
    internal static string Decide(object command, ConversationAgentStateState? state) => Compute(command, state).Decision;
    private static (IReadOnlyList<IEventPayload> Events, string Decision) Compute(object command, ConversationAgentStateState? state)
    {
        long revision = (state?.Revision ?? 0) + 1;
        IEventPayload item = command switch
        {
            RegisterInteractionPermit c => new InteractionCreationPermitted(c.Permit, (state?.PermitHighWater ?? 0) + 1, c.Permit.CreationOutboxId, revision),
            AcknowledgeInteractionCreation c => new InteractionCreationAcknowledged(c.AgentInteractionId, c.PermitId, c.OutboxId, c.InteractionCreationRevision, revision),
            AcquireConversationEffectLease c => new ConversationEffectLeaseAcquired(c.Lease, revision),
            CommitConversationEffect c => new ConversationEffectCommitted(c.LeaseId, revision),
            SettleConversationEffectLease c => new ConversationEffectLeaseSettled(c.LeaseId, c.CommitRevision, c.ResultRevision, revision),
            CancelConversationEffectLease c => new ConversationEffectLeaseCancelled(c.LeaseId, revision),
            EnterConversationDeletionClosing c => new ConversationDeletionClosingEntered(c.TenantId, c.ConversationId, c.DeletionRequestId,
                (state?.Leases.Values.AsEnumerable() ?? []).Where(l => l.Status == ConversationEffectLeaseStatus.CommittedToEffect).Select(l => l.LeaseId).Order(StringComparer.Ordinal).ToArray(), revision),
            _ => throw new ArgumentOutOfRangeException(nameof(command)),
        };
        List<IEventPayload> events = [item];
        if (command is EnterConversationDeletionClosing && state is not null)
        {
            foreach (ConversationEffectLease lease in state!.Leases.Values.Where(l => l.Status == ConversationEffectLeaseStatus.Reserved).OrderBy(l => l.LeaseId, StringComparer.Ordinal))
            { events.Add(new ConversationEffectLeaseCancelled(lease.LeaseId, ++revision)); }
        }
        return (events, item.GetType().Name);
    }
}
