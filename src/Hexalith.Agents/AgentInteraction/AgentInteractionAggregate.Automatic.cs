using System.Text.Json;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.Security;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>Additive permit and phase handlers, independent of legacy plaintext creation and attempt identities.</summary>
public partial class AgentInteractionAggregate
{
    /// <summary>Creates only the exact directory-permitted interaction; a legacy stream is never upgraded in place.</summary>
    public static DomainResult Handle(CreatePermittedAgentInteraction command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(envelope);
        if (command.Permit is not { } p) { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidPermit); }
        if (envelope.Domain != "agent-interaction" || p.TenantId != envelope.TenantId || p.AgentInteractionId != envelope.AggregateId || !AutomaticAuthority(envelope)) { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidTarget); }
        if (state?.PermittedCreation is { } existing)
        {
            return JsonSerializer.Serialize(existing.Permit) == JsonSerializer.Serialize(p) && existing.PermitRevision == command.PermitRevision ? DomainResult.NoOp() : AutomaticReject(AutomaticInteractionRejectionReason.DivergentDuplicate);
        }
        if (state is not null) { return AutomaticReject(AutomaticInteractionRejectionReason.LegacyStream); }
        if (p.Snapshot is null || p.DirectoryEpoch is null || p.DirectoryEpoch.TenantId != p.TenantId || string.IsNullOrWhiteSpace(p.DirectoryEpoch.EpochId)
            || !AutomaticDigest(p.RequestDigest) || string.IsNullOrWhiteSpace(p.DigestKeyVersion) || string.IsNullOrWhiteSpace(p.AgentId) || string.IsNullOrWhiteSpace(p.CallerPartyId) || string.IsNullOrWhiteSpace(p.ClientIdempotencyKey)
            || command.ExpectedRevision != 0 || command.PermitRevision <= 0 || p.PermitId != ConversationAgentStateIdentity.Permit(p.TenantId, p.AgentInteractionId)
            || p.CreationOutboxId != ConversationAgentStateIdentity.CreationOutbox(p.TenantId, p.AgentInteractionId)
            || p.AgentInteractionId != ConversationAgentStateIdentity.Interaction(p.TenantId, p.AgentId, p.ConversationId, p.CallerPartyId, p.ClientIdempotencyKey)
            || p.Request?.Target is not { } target || target.TenantId != p.TenantId || target.AgentInteractionId != p.AgentInteractionId
            || string.IsNullOrWhiteSpace(target.TargetProtectionKeyAlias) || p.Snapshot.ResponseMode is not (AgentResponseMode.Automatic or AgentResponseMode.Confirmation)) { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidPermit); }
        string attemptId = ConversationAgentStateIdentity.Attempt(p.TenantId, p.AgentInteractionId, 1);
        string versionId = ConversationAgentStateIdentity.Version(p.AgentInteractionId, 1, "Generated");
        return DomainResult.Success([new PermittedAgentInteractionCreated(p, command.PermitRevision,
            ConversationAgentStateIdentity.WorkflowStartOutbox(p.TenantId, p.AgentInteractionId), ConversationAgentStateIdentity.DirectoryAckOutbox(p.TenantId, p.AgentInteractionId),
            attemptId, ConversationAgentStateIdentity.Reservation(attemptId), ConversationAgentStateIdentity.Admission(attemptId), versionId,
            ConversationAgentStateIdentity.Message(p.AgentInteractionId, versionId), 1)]);
    }
    /// <summary>Records at most one trusted durable phase decision and its inseparable Provider dispositions.</summary>
    public static DomainResult Handle(RecordAutomaticInteractionStep command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(envelope);
        if (state?.PermittedCreation is not { } creation || envelope.Domain != "agent-interaction" || envelope.TenantId != command.TenantId
            || creation.Permit.TenantId != command.TenantId || envelope.AggregateId != command.AgentInteractionId || state.AgentInteractionId != command.AgentInteractionId || !AutomaticAuthority(envelope)) { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidTarget); }
        if (command.Fact is not { } f) { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidPhase); }
        if (!Enum.IsDefined(f.Step) || f.Step == AutomaticInteractionStep.Unknown || !Enum.IsDefined(f.Outcome) || f.Outcome == AutomaticInteractionOutcome.Unknown
            || !AutomaticDigest(f.OperationId) || f.OperationId != AgentsSecurityIdentity.Derive("interaction-step", command.TenantId, command.AgentInteractionId, f.Step.ToString())) { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidPhase); }
        if (!ValidAutomaticOutcome(f.Step, f.Outcome)) { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidPhase); }
        if (state.AutomaticFacts.TryGetValue(f.Step, out AutomaticInteractionFact? existing))
        {
            return JsonSerializer.Serialize(existing with { Revision = 0 }) == JsonSerializer.Serialize(f with { Revision = 0 }) ? DomainResult.NoOp() : AutomaticReject(AutomaticInteractionRejectionReason.DivergentDuplicate);
        }
        if (command.ExpectedRevision != state.AutomaticRevision || f.Revision != state.AutomaticRevision + 1) { return AutomaticReject(AutomaticInteractionRejectionReason.StaleRevision); }
        if (AutomaticTerminal(state)) { return AutomaticReject(AutomaticInteractionRejectionReason.Terminal); }
        if (!AutomaticPrecondition(f.Step, state)) { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidPhase); }
        ConversationEffectKind kind = LeaseKind(f.Step);
        AutomaticInteractionStep leaseStep = LeaseOperationStep(f.Step);
        if (f.Step == AutomaticInteractionStep.GenerationFailed && f.LeaseId == ConversationAgentStateIdentity.Lease(command.TenantId, creation.Permit.ConversationId, command.AgentInteractionId,
            ConversationEffectKind.ProposalMutation, AutomaticInteractionStep.GeneratedVersionRecorded.ToString(), AgentsSecurityIdentity.Derive("interaction-step", command.TenantId, command.AgentInteractionId, AutomaticInteractionStep.GeneratedVersionRecorded.ToString())))
        { kind = ConversationEffectKind.ProposalMutation; leaseStep = AutomaticInteractionStep.GeneratedVersionRecorded; }
        if (kind != ConversationEffectKind.Unknown && (f.LeaseCommitRevision <= 0 || f.LeaseId != ConversationAgentStateIdentity.Lease(command.TenantId, creation.Permit.ConversationId, command.AgentInteractionId, kind, leaseStep.ToString(), AgentsSecurityIdentity.Derive("interaction-step", command.TenantId, command.AgentInteractionId, leaseStep.ToString())))) { return AutomaticReject(AutomaticInteractionRejectionReason.LeaseCommitRequired); }
        if (f.Step == AutomaticInteractionStep.ConversationPostingSuppressedByDeletion)
        {
            string postingOperation = AgentsSecurityIdentity.Derive("interaction-step", command.TenantId, command.AgentInteractionId, AutomaticInteractionStep.ConversationPostingRecorded.ToString());
            string postingLease = ConversationAgentStateIdentity.Lease(command.TenantId, creation.Permit.ConversationId, command.AgentInteractionId,
                ConversationEffectKind.ConversationPosting, AutomaticInteractionStep.ConversationPostingRecorded.ToString(), postingOperation);
            if (!AutomaticDigest(f.DeletionRequestId!) || f.DirectoryClosingRevision <= 0 || f.LeaseCommitRevision != 0
                || f.LeaseId is not null && f.LeaseId != postingLease)
            { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidPhase); }
        }
        else if (f.DeletionRequestId is not null || f.DirectoryClosingRevision != 0) { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidPhase); }
        if (f.Step == AutomaticInteractionStep.ProviderInvocationAuthorized && f.OutputAuthority?.IsEffective() != true) { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidAuthority); }
        if (!Enum.IsDefined(f.FailureKind) || f.Step != AutomaticInteractionStep.GenerationFailed && f.FailureKind != AutomaticInteractionFailureKind.Unknown
            || f.Step == AutomaticInteractionStep.GenerationFailed && f.Outcome == AutomaticInteractionOutcome.Failed && f.FailureKind == AutomaticInteractionFailureKind.Unknown)
        { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidPhase); }
        if (f.Step != AutomaticInteractionStep.ProviderInvocationAuthorized && f.OutputAuthority is not null) { return AutomaticReject(AutomaticInteractionRejectionReason.InvalidAuthority); }
        if (f.Step == AutomaticInteractionStep.GeneratedVersionRecorded)
        {
            if (f.Outcome != AutomaticInteractionOutcome.Allowed || f.Output?.Target is not { } target || target.TenantId != command.TenantId
                || target.AgentInteractionId != command.AgentInteractionId || target.TargetProtectionKeyAlias != creation.Permit.Request.Target.TargetProtectionKeyAlias || !AutomaticDigest(f.ContentDigest!) || string.IsNullOrWhiteSpace(f.DigestKeyVersion)) { return AutomaticReject(AutomaticInteractionRejectionReason.ProtectedContentRequired); }
        }
        else if (f.Output is not null || f.ContentDigest is not null || f.DigestKeyVersion is not null) { return AutomaticReject(AutomaticInteractionRejectionReason.ProtectedContentRequired); }
        return DomainResult.Success(AutomaticInteractionPolicy.Evaluate(f));
    }
    /// <summary>Gets the exact directory lease kind for a leased target phase.</summary>
    internal static ConversationEffectKind LeaseKind(AutomaticInteractionStep step) => step switch
    {
        AutomaticInteractionStep.WorkflowStartCheckpoint => ConversationEffectKind.WorkflowStart,
        AutomaticInteractionStep.ApproverResolutionRecorded => ConversationEffectKind.ApproverResolution,
        AutomaticInteractionStep.ContextReadRecorded or AutomaticInteractionStep.PromptSafetyRecorded => ConversationEffectKind.ContextRead,
        AutomaticInteractionStep.ConversationMembershipRecorded => ConversationEffectKind.ConversationMembership,
        AutomaticInteractionStep.ProviderInvocationAuthorized or AutomaticInteractionStep.ProviderInvocationNotAuthorized or AutomaticInteractionStep.GenerationFailed => ConversationEffectKind.ProviderInvocation,
        AutomaticInteractionStep.GeneratedVersionRecorded => ConversationEffectKind.ProposalMutation,
        AutomaticInteractionStep.ConversationPostingRecorded => ConversationEffectKind.ConversationPosting,
        _ => ConversationEffectKind.Unknown,
    };
    /// <summary>Gets the original operation shared by the immutable Provider branch and its failure result.</summary>
    internal static AutomaticInteractionStep LeaseOperationStep(AutomaticInteractionStep step) => step is AutomaticInteractionStep.ProviderInvocationNotAuthorized or AutomaticInteractionStep.GenerationFailed
        ? AutomaticInteractionStep.ProviderInvocationAuthorized : step;
    private static bool AutomaticPrecondition(AutomaticInteractionStep step, AgentInteractionState s)
    {
        bool Has(AutomaticInteractionStep p) => s.AutomaticFacts.TryGetValue(p, out AutomaticInteractionFact? f) && f.Outcome is AutomaticInteractionOutcome.Allowed or AutomaticInteractionOutcome.Authorized or AutomaticInteractionOutcome.Posted;
        return step switch
        {
            AutomaticInteractionStep.WorkflowStartCheckpoint => s.AutomaticFacts.Count == 0,
            AutomaticInteractionStep.RateAdmissionAuthorized => Has(AutomaticInteractionStep.WorkflowStartCheckpoint),
            AutomaticInteractionStep.OpenInteractionLeaseAuthorized => Has(AutomaticInteractionStep.RateAdmissionAuthorized),
            AutomaticInteractionStep.RateAdmissionRecorded => Has(AutomaticInteractionStep.OpenInteractionLeaseAuthorized),
            AutomaticInteractionStep.OpenInteractionLeaseRecorded => Has(AutomaticInteractionStep.RateAdmissionRecorded),
            AutomaticInteractionStep.ApproverResolutionRecorded => s.Snapshot?.ResponseMode == AgentResponseMode.Confirmation && Has(AutomaticInteractionStep.OpenInteractionLeaseRecorded),
            AutomaticInteractionStep.ContextReadRecorded => Has(AutomaticInteractionStep.OpenInteractionLeaseRecorded) && (s.Snapshot?.ResponseMode == AgentResponseMode.Automatic || Has(AutomaticInteractionStep.ApproverResolutionRecorded)),
            AutomaticInteractionStep.BudgetReservationAuthorized => Has(AutomaticInteractionStep.ContextReadRecorded),
            AutomaticInteractionStep.BudgetReservationRecorded => Has(AutomaticInteractionStep.BudgetReservationAuthorized),
            AutomaticInteractionStep.PromptSafetyRecorded => Has(AutomaticInteractionStep.BudgetReservationRecorded),
            AutomaticInteractionStep.ConversationMembershipRecorded => Has(AutomaticInteractionStep.PromptSafetyRecorded),
            AutomaticInteractionStep.AgentCallAccepted => Has(AutomaticInteractionStep.ConversationMembershipRecorded),
            AutomaticInteractionStep.CapacityAdmissionAuthorized => Has(AutomaticInteractionStep.AgentCallAccepted),
            AutomaticInteractionStep.CapacityAdmissionRecorded => Has(AutomaticInteractionStep.CapacityAdmissionAuthorized),
            AutomaticInteractionStep.ProviderInvocationAuthorized or AutomaticInteractionStep.ProviderInvocationNotAuthorized => Has(AutomaticInteractionStep.CapacityAdmissionRecorded)
                && !s.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ProviderInvocationAuthorized) && !s.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ProviderInvocationNotAuthorized),
            AutomaticInteractionStep.GeneratedVersionRecorded or AutomaticInteractionStep.GenerationFailed => Has(AutomaticInteractionStep.ProviderInvocationAuthorized) && !s.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded),
            AutomaticInteractionStep.ConversationPostingRecorded or AutomaticInteractionStep.ConversationPostingSuppressedByDeletion => s.Snapshot?.ResponseMode == AgentResponseMode.Automatic
                && Has(AutomaticInteractionStep.AgentCallAccepted) && Has(AutomaticInteractionStep.GeneratedVersionRecorded),
            AutomaticInteractionStep.StartSuppressedByDeletion => !Has(AutomaticInteractionStep.WorkflowStartCheckpoint),
            AutomaticInteractionStep.InteractionPreAcceptanceFailed => !Has(AutomaticInteractionStep.AgentCallAccepted),
            AutomaticInteractionStep.PreProviderCapacityDispositionDecided => Has(AutomaticInteractionStep.AgentCallAccepted) && !Has(AutomaticInteractionStep.ProviderInvocationAuthorized),
            _ => false,
        };
    }
    private static bool AutomaticTerminal(AgentInteractionState s) => s.AutomaticFacts.Keys.Any(p => p is AutomaticInteractionStep.StartSuppressedByDeletion or AutomaticInteractionStep.InteractionPreAcceptanceFailed
        or AutomaticInteractionStep.ProviderInvocationNotAuthorized or AutomaticInteractionStep.GenerationFailed or AutomaticInteractionStep.ConversationPostingRecorded or AutomaticInteractionStep.ConversationPostingSuppressedByDeletion or AutomaticInteractionStep.PreProviderCapacityDispositionDecided)
        || s.Snapshot?.ResponseMode == AgentResponseMode.Confirmation && s.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded);
    private static bool ValidAutomaticOutcome(AutomaticInteractionStep step, AutomaticInteractionOutcome outcome) => step switch
    {
        AutomaticInteractionStep.RateAdmissionAuthorized or AutomaticInteractionStep.OpenInteractionLeaseAuthorized or AutomaticInteractionStep.BudgetReservationAuthorized
            or AutomaticInteractionStep.CapacityAdmissionAuthorized or AutomaticInteractionStep.ProviderInvocationAuthorized => outcome == AutomaticInteractionOutcome.Authorized,
        AutomaticInteractionStep.WorkflowStartCheckpoint or AutomaticInteractionStep.AgentCallAccepted or AutomaticInteractionStep.GeneratedVersionRecorded => outcome == AutomaticInteractionOutcome.Allowed,
        AutomaticInteractionStep.ProviderInvocationNotAuthorized => outcome is AutomaticInteractionOutcome.Blocked or AutomaticInteractionOutcome.DependencyNotAvailable,
        AutomaticInteractionStep.GenerationFailed => outcome is AutomaticInteractionOutcome.Failed or AutomaticInteractionOutcome.Discarded,
        AutomaticInteractionStep.PreProviderCapacityDispositionDecided => outcome == AutomaticInteractionOutcome.CancelNoInvocation,
        AutomaticInteractionStep.StartSuppressedByDeletion or AutomaticInteractionStep.ConversationPostingSuppressedByDeletion => outcome == AutomaticInteractionOutcome.Suppressed,
        AutomaticInteractionStep.InteractionPreAcceptanceFailed => outcome is AutomaticInteractionOutcome.Blocked or AutomaticInteractionOutcome.DependencyNotAvailable or AutomaticInteractionOutcome.Suppressed,
        AutomaticInteractionStep.ConversationPostingRecorded => outcome is AutomaticInteractionOutcome.Posted or AutomaticInteractionOutcome.Blocked or AutomaticInteractionOutcome.Failed or AutomaticInteractionOutcome.DependencyNotAvailable,
        _ => outcome is AutomaticInteractionOutcome.Allowed or AutomaticInteractionOutcome.Blocked or AutomaticInteractionOutcome.DependencyNotAvailable or AutomaticInteractionOutcome.Failed,
    };
    private static bool AutomaticAuthority(CommandEnvelope e) => e.Extensions?.TryGetValue(ConversationAgentStateIdentity.WorkflowAuthority, out string? value) == true && value == "true";
    private static bool AutomaticDigest(string value) => value is { Length: 64 } && value.All(char.IsAsciiHexDigit);
    private static DomainResult AutomaticReject(AutomaticInteractionRejectionReason reason) => DomainResult.Rejection([new AutomaticInteractionRejected(reason)]);
}
