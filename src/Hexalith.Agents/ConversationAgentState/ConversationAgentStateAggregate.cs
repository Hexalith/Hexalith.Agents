using System.Text.Json;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;
using Hexalith.Agents.Contracts.Security;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.ConversationAgentState;

/// <summary>Pure Conversation-owned interaction directory; acceptance and Closing serialize at this owner.</summary>
[EventStoreDomain(ConversationAgentStateIdentity.Domain)]
public sealed class ConversationAgentStateAggregate : EventStoreAggregate<ConversationAgentStateState>
{
    /// <summary>Registers one protected creation intent; identical retry never loses the permit.</summary>
    public static DomainResult Handle(RegisterInteractionPermit command, ConversationAgentStateState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(envelope);
        if (command.Permit is not { } p) { return Reject(ConversationDirectoryRejectionReason.InvalidTarget); }
        if (!Scope(state, envelope, p.ConversationId) || p.TenantId != envelope.TenantId || !Authority(envelope)) { return Reject(ConversationDirectoryRejectionReason.InvalidTarget); }
        if (p.Request?.Target is not { } target || target.TenantId != p.TenantId || target.AgentInteractionId != p.AgentInteractionId || string.IsNullOrWhiteSpace(target.TargetProtectionKeyAlias)) { return Reject(ConversationDirectoryRejectionReason.ProtectionTargetMismatch); }
        if (p.DirectoryEpoch is not { } epoch || epoch.TenantId != p.TenantId || string.IsNullOrWhiteSpace(epoch.EpochId)
            || string.IsNullOrWhiteSpace(epoch.WriterEnforcementReceipt) || string.IsNullOrWhiteSpace(epoch.LegacyRevocationReceipt)) { return Reject(ConversationDirectoryRejectionReason.DirectoryNotReady); }
        if (p.AgentInteractionId != ConversationAgentStateIdentity.Interaction(p.TenantId, p.AgentId, p.ConversationId, p.CallerPartyId, p.ClientIdempotencyKey)
            || p.PermitId != ConversationAgentStateIdentity.Permit(p.TenantId, p.AgentInteractionId)
            || p.CreationOutboxId != ConversationAgentStateIdentity.CreationOutbox(p.TenantId, p.AgentInteractionId)
            || !Digest(p.RequestDigest) || string.IsNullOrWhiteSpace(p.DigestKeyVersion) || string.IsNullOrWhiteSpace(p.AgentId)
            || string.IsNullOrWhiteSpace(p.CallerPartyId) || string.IsNullOrWhiteSpace(p.ClientIdempotencyKey)
            || p.Snapshot is null || p.Snapshot.ResponseMode is not (AgentResponseMode.Automatic or AgentResponseMode.Confirmation)) { return Reject(ConversationDirectoryRejectionReason.InvalidTarget); }
        if (state?.DeletionRequestId is not null) { return Reject(ConversationDirectoryRejectionReason.ConversationClosing); }
        if (state?.Permits.TryGetValue(p.AgentInteractionId, out InteractionCreationPermitted? original) == true)
        {
            return JsonSerializer.Serialize(original.Permit) == JsonSerializer.Serialize(p) ? DomainResult.NoOp() : Reject(ConversationDirectoryRejectionReason.DivergentDuplicate);
        }
        if (state?.DeletionRequestId is not null) { return Reject(ConversationDirectoryRejectionReason.ConversationClosing); }
        if (command.ExpectedRevision != (state?.Revision ?? 0)) { return Reject(ConversationDirectoryRejectionReason.StaleRevision); }
        return Accept(command, state);
    }
    /// <summary>Acknowledges only the exact interaction's creation outbox.</summary>
    public static DomainResult Handle(AcknowledgeInteractionCreation command, ConversationAgentStateState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(envelope);
        if (state is null || !ExistingScope(state, envelope) || !Authority(envelope)) { return Reject(ConversationDirectoryRejectionReason.InvalidTarget); }
        if (!state!.Permits.TryGetValue(command.AgentInteractionId, out InteractionCreationPermitted? p)) { return Reject(ConversationDirectoryRejectionReason.PermitNotFound); }
        if (command.PermitId != p.Permit.PermitId || command.OutboxId != p.OutboxId || command.InteractionCreationRevision <= 0) { return Reject(ConversationDirectoryRejectionReason.InvalidTarget); }
        if (state.Acknowledgements.TryGetValue(command.AgentInteractionId, out InteractionCreationAcknowledged? ack))
        {
            return ack.PermitId == command.PermitId && ack.OutboxId == command.OutboxId && ack.InteractionCreationRevision == command.InteractionCreationRevision ? DomainResult.NoOp() : Reject(ConversationDirectoryRejectionReason.DivergentDuplicate);
        }
        if (command.ExpectedRevision != state.Revision) { return Reject(ConversationDirectoryRejectionReason.StaleRevision); }
        return Accept(command, state);
    }
    /// <summary>Reserves one deterministic lease without granting effect authority.</summary>
    public static DomainResult Handle(AcquireConversationEffectLease command, ConversationAgentStateState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(envelope);
        if (command.Lease is not { } l) { return Reject(ConversationDirectoryRejectionReason.InvalidLease); }
        if (state is null || !ExistingScope(state, envelope) || !Authority(envelope)) { return Reject(ConversationDirectoryRejectionReason.InvalidTarget); }
        if (!state!.Permits.TryGetValue(l.AgentInteractionId, out InteractionCreationPermitted? p)) { return Reject(ConversationDirectoryRejectionReason.PermitNotFound); }
        if (state.DeletionRequestId is not null) { return Reject(ConversationDirectoryRejectionReason.ConversationClosing); }
        if (l.Status != ConversationEffectLeaseStatus.Reserved || l.CommitRevision != 0 || l.ResultRevision != 0 || l.MustSettle) { return Reject(ConversationDirectoryRejectionReason.InvalidLease); }
        if (state.Leases.TryGetValue(l.LeaseId, out ConversationEffectLease? existing))
        {
            return existing with { Status = ConversationEffectLeaseStatus.Reserved, CommitRevision = 0, ResultRevision = 0, MustSettle = false } == l ? DomainResult.NoOp() : Reject(ConversationDirectoryRejectionReason.DivergentDuplicate);
        }
        if (l.LeaseId != ConversationAgentStateIdentity.Lease(envelope.TenantId, state.ConversationId, l.AgentInteractionId, l.Kind, l.OperationFamily, l.OperationIdentity)
            || l.Kind == ConversationEffectKind.Unknown || !Enum.IsDefined(l.Kind) || l.WorkflowInstanceId != l.AgentInteractionId
            || l.DirectoryEpochId != p.Permit.DirectoryEpoch.EpochId || string.IsNullOrWhiteSpace(l.OperationFamily) || !Digest(l.OperationIdentity)
            || l.Status != ConversationEffectLeaseStatus.Reserved || l.CommitRevision != 0 || l.ResultRevision != 0 || l.MustSettle) { return Reject(ConversationDirectoryRejectionReason.InvalidLease); }
        if (state.DeletionRequestId is not null) { return Reject(ConversationDirectoryRejectionReason.ConversationClosing); }

        if (state.DeletionRequestId is not null) { return Reject(ConversationDirectoryRejectionReason.ConversationClosing); }
        if (command.ExpectedRevision != state.Revision) { return Reject(ConversationDirectoryRejectionReason.StaleRevision); }
        return Accept(command, state);
    }
    /// <summary>Commits at the same owner revision raced by Closing.</summary>
    public static DomainResult Handle(CommitConversationEffect command, ConversationAgentStateState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(envelope);
        if (state is null || !TryLease(state, envelope, command.LeaseId, command.AgentInteractionId, command.WorkflowInstanceId, out ConversationEffectLease? lease)) { return Reject(ConversationDirectoryRejectionReason.LeaseNotFound); }
        if (state!.DeletionRequestId is not null) { return Reject(ConversationDirectoryRejectionReason.ConversationClosing); }
        if (lease!.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit) { return Reject(ConversationDirectoryRejectionReason.LeaseCancelled); }
        if (lease.CommitRevision > 0) { return DomainResult.NoOp(); }
        if (state!.DeletionRequestId is not null) { return Reject(ConversationDirectoryRejectionReason.ConversationClosing); }
        if (command.ExpectedRevision != state.Revision) { return Reject(ConversationDirectoryRejectionReason.StaleRevision); }
        return Accept(command, state);
    }
    /// <summary>Settles a committed phase only with its exact positive or negative result revision.</summary>
    public static DomainResult Handle(SettleConversationEffectLease command, ConversationAgentStateState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(envelope);
        if (state is null || !TryLease(state, envelope, command.LeaseId, command.AgentInteractionId, command.WorkflowInstanceId, out ConversationEffectLease? lease)) { return Reject(ConversationDirectoryRejectionReason.LeaseNotFound); }
        if (lease!.CommitRevision <= 0 || command.CommitRevision != lease.CommitRevision || command.ResultRevision <= 0) { return Reject(ConversationDirectoryRejectionReason.LeaseNotCommitted); }
        if (lease.Status == ConversationEffectLeaseStatus.Settled) { return lease.ResultRevision == command.ResultRevision ? DomainResult.NoOp() : Reject(ConversationDirectoryRejectionReason.DivergentDuplicate); }
        if (command.ExpectedRevision != state!.Revision) { return Reject(ConversationDirectoryRejectionReason.StaleRevision); }
        return Accept(command, state);
    }
    /// <summary>Cancels reservations only; a committed phase must settle.</summary>
    public static DomainResult Handle(CancelConversationEffectLease command, ConversationAgentStateState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(envelope);
        if (state is null || !TryLease(state, envelope, command.LeaseId, command.AgentInteractionId, command.WorkflowInstanceId, out ConversationEffectLease? lease)) { return Reject(ConversationDirectoryRejectionReason.LeaseNotFound); }
        if (lease!.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit) { return DomainResult.NoOp(); }
        if (lease.Status != ConversationEffectLeaseStatus.Reserved) { return Reject(ConversationDirectoryRejectionReason.LeaseNotCommitted); }
        if (command.ExpectedRevision != state!.Revision) { return Reject(ConversationDirectoryRejectionReason.StaleRevision); }
        return Accept(command, state);
    }
    /// <summary>Atomically closes admission, cancels every reservation, and manifests committed work to settle.</summary>
    public static DomainResult Handle(EnterConversationDeletionClosing command, ConversationAgentStateState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(envelope);
        if (!Scope(state, envelope, command.ConversationId) || command.TenantId != envelope.TenantId || envelope.Extensions?.GetValueOrDefault(ConversationAgentStateIdentity.DeletionAuthority) != "true" || !Digest(command.DeletionRequestId)) { return Reject(ConversationDirectoryRejectionReason.InvalidTarget); }
        if (state?.DeletionRequestId is not null) { return state.DeletionRequestId == command.DeletionRequestId ? DomainResult.NoOp() : Reject(ConversationDirectoryRejectionReason.DivergentDuplicate); }
        if (command.ExpectedRevision != (state?.Revision ?? 0)) { return Reject(ConversationDirectoryRejectionReason.StaleRevision); }
        return Accept(command, state);
    }

    private static bool Authority(CommandEnvelope e) => e.Extensions?.TryGetValue(ConversationAgentStateIdentity.WorkflowAuthority, out string? value) == true && value == "true";
    private static bool Digest(string value) => value is { Length: 64 } && value.All(char.IsAsciiHexDigit);
    private static bool Scope(ConversationAgentStateState? state, CommandEnvelope e, string conversationId) => !string.IsNullOrWhiteSpace(conversationId) && !string.IsNullOrWhiteSpace(e.TenantId)
        && e.Domain == ConversationAgentStateIdentity.Domain && e.AggregateId == ConversationAgentStateIdentity.Directory(e.TenantId, conversationId)
        && (state is null || state.Revision == 0 || state.TenantId == e.TenantId && state.ConversationId == conversationId);
    private static bool ExistingScope(ConversationAgentStateState? state, CommandEnvelope e) => state is not null && Scope(state, e, state.ConversationId);
    private static bool TryLease(ConversationAgentStateState? state, CommandEnvelope e, string leaseId, string interactionId, string workflowId, out ConversationEffectLease? lease)
    {
        lease = null;
        return ExistingScope(state, e) && Authority(e) && state!.Leases.TryGetValue(leaseId, out lease) && lease.AgentInteractionId == interactionId && lease.WorkflowInstanceId == workflowId;
    }
    private static DomainResult Reject(ConversationDirectoryRejectionReason reason) => DomainResult.Rejection([new ConversationDirectoryRejected(reason)]);
    private static DomainResult Accept(object command, ConversationAgentStateState? state) => DomainResult.Success(ConversationDirectoryPolicy.Evaluate(command, state));
}
