using System.Text.Json.Serialization;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;

namespace Hexalith.Agents.ConversationAgentState;

/// <summary>The authoritative permit/outbox and effect directory, folded exclusively from accepted events.</summary>
public sealed class ConversationAgentStateState
{
    /// <summary>Gets the bound tenant.</summary>
    [JsonInclude] public string TenantId { get; private set; } = string.Empty;
    /// <summary>Gets the bound Conversation.</summary>
    [JsonInclude] public string ConversationId { get; private set; } = string.Empty;
    /// <summary>Gets the successful event revision.</summary>
    [JsonInclude] public long Revision { get; private set; }
    /// <summary>Gets the append-only permit high-water.</summary>
    [JsonInclude] public long PermitHighWater { get; private set; }
    /// <summary>Gets the Closing request; no Effective or purge state is modelled.</summary>
    [JsonInclude] public string? DeletionRequestId { get; private set; }
    /// <summary>Gets the immutable revision of the Closing decision, preserved across later lease settlements.</summary>
    [JsonInclude] public long DeletionClosingRevision { get; private set; }
    /// <summary>Gets every accepted permit, including those with lagging creation acknowledgement.</summary>
    [JsonInclude] public Dictionary<string, InteractionCreationPermitted> Permits { get; private set; } = [];
    /// <summary>Gets exact creation acknowledgements.</summary>
    [JsonInclude] public Dictionary<string, InteractionCreationAcknowledged> Acknowledgements { get; private set; } = [];
    /// <summary>Gets the owner-local lease history.</summary>
    [JsonInclude] public Dictionary<string, ConversationEffectLease> Leases { get; private set; } = [];
    /// <summary>Folds a permit without consulting a projection.</summary>
    public void Apply(InteractionCreationPermitted item)
    {
        ArgumentNullException.ThrowIfNull(item);
        TenantId = item.Permit.TenantId; ConversationId = item.Permit.ConversationId;
        Permits.Add(item.Permit.AgentInteractionId, item); PermitHighWater = item.PermitHighWater; Revision = item.Revision;
    }
    /// <summary>Folds the exact reciprocal acknowledgement.</summary>
    public void Apply(InteractionCreationAcknowledged item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Acknowledgements.Add(item.AgentInteractionId, item); Revision = item.Revision;
    }
    /// <summary>Folds a reservation, which authorizes no effect.</summary>
    public void Apply(ConversationEffectLeaseAcquired item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Leases.Add(item.Lease.LeaseId, item.Lease); Revision = item.Revision;
    }
    /// <summary>Folds the sole effect authorization revision.</summary>
    public void Apply(ConversationEffectCommitted item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Leases[item.LeaseId] = Leases[item.LeaseId] with { Status = ConversationEffectLeaseStatus.CommittedToEffect, CommitRevision = item.Revision }; Revision = item.Revision;
    }
    /// <summary>Folds the bound result after settlement.</summary>
    public void Apply(ConversationEffectLeaseSettled item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Leases[item.LeaseId] = Leases[item.LeaseId] with { Status = ConversationEffectLeaseStatus.Settled, ResultRevision = item.ResultRevision, MustSettle = false }; Revision = item.Revision;
    }
    /// <summary>Folds cancellation only before commit.</summary>
    public void Apply(ConversationEffectLeaseCancelled item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Leases[item.LeaseId] = Leases[item.LeaseId] with { Status = ConversationEffectLeaseStatus.CancelledBeforeCommit }; Revision = item.Revision;
    }
    /// <summary>Folds Closing and its immutable must-settle manifest.</summary>
    public void Apply(ConversationDeletionClosingEntered item)
    {
        ArgumentNullException.ThrowIfNull(item);
        TenantId = item.TenantId; ConversationId = item.ConversationId; DeletionRequestId = item.DeletionRequestId; DeletionClosingRevision = item.Revision;
        foreach (string id in item.MustSettleLeaseIds) { Leases[id] = Leases[id] with { MustSettle = true }; }
        Revision = item.Revision;
    }
    /// <summary>Rejections preserve accepted state.</summary>
    public void Apply(ConversationDirectoryRejected item) { ArgumentNullException.ThrowIfNull(item); _ = Revision; }
}
