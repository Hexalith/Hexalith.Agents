using System.Text.Json.Serialization;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>Additive protected automatic-interaction replay facts; legacy events retain their original fold.</summary>
public sealed partial class AgentInteractionState
{
    /// <summary>Gets the permit-created first event; absent on a legacy stream.</summary>
    [JsonInclude] public PermittedAgentInteractionCreated? PermittedCreation { get; private set; }
    /// <summary>Gets the additive automatic-path successful event revision.</summary>
    [JsonInclude] public long AutomaticRevision { get; private set; }
    /// <summary>Gets the exact immutable phase facts.</summary>
    [JsonInclude] public Dictionary<AutomaticInteractionStep, AutomaticInteractionFact> AutomaticFacts { get; private set; } = [];
    /// <summary>Folds the first protected creation event without materializing request content.</summary>
    public void Apply(PermittedAgentInteractionCreated item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (IsRequested) { throw new InvalidOperationException("An existing interaction cannot be recreated."); }
        PermittedCreation = item; AutomaticRevision = item.Revision; IsRequested = true;
        AgentInteractionId = item.Permit.AgentInteractionId; AgentId = item.Permit.AgentId; CallerPartyId = item.Permit.CallerPartyId;
        SourceConversationId = item.Permit.ConversationId; Snapshot = item.Permit.Snapshot; IdempotencyKey = item.Permit.ClientIdempotencyKey;
        Status = AgentInteractionStatus.Requested;
    }
    /// <summary>Folds the exact WorkflowStartCheckpoint fact.</summary>
    public void Apply(WorkflowStartCheckpoint item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact RateAdmissionAuthorized fact.</summary>
    public void Apply(RateAdmissionAuthorized item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact OpenInteractionLeaseAuthorized fact.</summary>
    public void Apply(OpenInteractionLeaseAuthorized item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact RateAdmissionRecorded fact.</summary>
    public void Apply(RateAdmissionRecorded item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact OpenInteractionLeaseRecorded fact.</summary>
    public void Apply(OpenInteractionLeaseRecorded item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact ApproverResolutionRecorded fact.</summary>
    public void Apply(ApproverResolutionRecorded item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact ContextReadRecorded fact.</summary>
    public void Apply(ContextReadRecorded item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact BudgetReservationAuthorized fact.</summary>
    public void Apply(BudgetReservationAuthorized item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact BudgetReservationRecorded fact.</summary>
    public void Apply(BudgetReservationRecorded item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact PromptSafetyRecorded fact.</summary>
    public void Apply(PromptSafetyRecorded item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact ConversationMembershipRecorded fact.</summary>
    public void Apply(ConversationMembershipRecorded item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact AgentCallAccepted fact.</summary>
    public void Apply(AgentCallAccepted item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact CapacityAdmissionAuthorized fact.</summary>
    public void Apply(CapacityAdmissionAuthorized item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact CapacityAdmissionRecorded fact.</summary>
    public void Apply(CapacityAdmissionRecorded item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact ProviderInvocationAuthorized fact.</summary>
    public void Apply(ProviderInvocationAuthorized item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact ProviderInvocationNotAuthorized fact.</summary>
    public void Apply(ProviderInvocationNotAuthorized item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact BudgetDispositionDecided fact.</summary>
    public void Apply(BudgetDispositionDecided item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact PreProviderCapacityDispositionDecided fact.</summary>
    public void Apply(PreProviderCapacityDispositionDecided item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact GenerationFailed fact.</summary>
    public void Apply(GenerationFailed item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact GeneratedVersionRecorded fact.</summary>
    public void Apply(GeneratedVersionRecorded item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact ConversationPostingRecorded fact.</summary>
    public void Apply(ConversationPostingRecorded item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds post suppression without changing the generated version or marking it failed.</summary>
    public void Apply(ConversationPostingSuppressedByDeletion item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact StartSuppressedByDeletion fact.</summary>
    public void Apply(StartSuppressedByDeletion item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }
    /// <summary>Folds the exact InteractionPreAcceptanceFailed fact.</summary>
    public void Apply(InteractionPreAcceptanceFailed item) { ArgumentNullException.ThrowIfNull(item); ApplyAutomaticFact(item.Fact); }

    /// <summary>A rejected automatic command preserves accepted state.</summary>
    public void Apply(AutomaticInteractionRejected item) { ArgumentNullException.ThrowIfNull(item); _ = AutomaticRevision; }
    private void ApplyAutomaticFact(AutomaticInteractionFact fact)
    {
        AutomaticFacts.Add(fact.Step, fact); AutomaticRevision = fact.Revision;
        if (fact.Step == AutomaticInteractionStep.GeneratedVersionRecorded) { Status = AgentInteractionStatus.Generated; }
        if (fact.Step == AutomaticInteractionStep.ConversationPostingRecorded) { Status = fact.Outcome == AutomaticInteractionOutcome.Posted ? AgentInteractionStatus.Posted : AgentInteractionStatus.PostingFailed; }
        if (fact.Step is AutomaticInteractionStep.InteractionPreAcceptanceFailed or AutomaticInteractionStep.StartSuppressedByDeletion) { Status = AgentInteractionStatus.Blocked; }
        if (fact.Step == AutomaticInteractionStep.GenerationFailed)
        {
            Status = fact.FailureKind is AutomaticInteractionFailureKind.OutputSafetyDenied or AutomaticInteractionFailureKind.OutputSafetyUnavailable
                ? AutomaticFacts[AutomaticInteractionStep.ProviderInvocationAuthorized].OutputAuthority!.FailureStatus
                : AgentInteractionStatus.GenerationFailed;
        }
    }
}
