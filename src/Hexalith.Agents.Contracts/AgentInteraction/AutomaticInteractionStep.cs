using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>Closed automatic-interaction vocabulary; zero never grants authority.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AutomaticInteractionStep>))]
public enum AutomaticInteractionStep
{
    /// <summary>Unknown classification.</summary>
    Unknown = 0,
    /// <summary>WorkflowStartCheckpoint classification.</summary>
    WorkflowStartCheckpoint = 1,
    /// <summary>RateAdmissionAuthorized classification.</summary>
    RateAdmissionAuthorized = 2,
    /// <summary>OpenInteractionLeaseAuthorized classification.</summary>
    OpenInteractionLeaseAuthorized = 3,
    /// <summary>RateAdmissionRecorded classification.</summary>
    RateAdmissionRecorded = 4,
    /// <summary>OpenInteractionLeaseRecorded classification.</summary>
    OpenInteractionLeaseRecorded = 5,
    /// <summary>ApproverResolutionRecorded classification.</summary>
    ApproverResolutionRecorded = 6,
    /// <summary>ContextReadRecorded classification.</summary>
    ContextReadRecorded = 7,
    /// <summary>BudgetReservationAuthorized classification.</summary>
    BudgetReservationAuthorized = 8,
    /// <summary>BudgetReservationRecorded classification.</summary>
    BudgetReservationRecorded = 9,
    /// <summary>PromptSafetyRecorded classification.</summary>
    PromptSafetyRecorded = 10,
    /// <summary>ConversationMembershipRecorded classification.</summary>
    ConversationMembershipRecorded = 11,
    /// <summary>AgentCallAccepted classification.</summary>
    AgentCallAccepted = 12,
    /// <summary>CapacityAdmissionAuthorized classification.</summary>
    CapacityAdmissionAuthorized = 13,
    /// <summary>CapacityAdmissionRecorded classification.</summary>
    CapacityAdmissionRecorded = 14,
    /// <summary>ProviderInvocationAuthorized classification.</summary>
    ProviderInvocationAuthorized = 15,
    /// <summary>ProviderInvocationNotAuthorized classification.</summary>
    ProviderInvocationNotAuthorized = 16,
    /// <summary>BudgetDispositionDecided classification.</summary>
    BudgetDispositionDecided = 17,
    /// <summary>PreProviderCapacityDispositionDecided classification.</summary>
    PreProviderCapacityDispositionDecided = 18,
    /// <summary>GenerationFailed classification.</summary>
    GenerationFailed = 19,
    /// <summary>GeneratedVersionRecorded classification.</summary>
    GeneratedVersionRecorded = 20,
    /// <summary>ConversationPostingRecorded classification.</summary>
    ConversationPostingRecorded = 21,
    /// <summary>StartSuppressedByDeletion classification.</summary>
    StartSuppressedByDeletion = 22,
    /// <summary>InteractionPreAcceptanceFailed classification.</summary>
    InteractionPreAcceptanceFailed = 23,
    /// <summary>Closing suppressed a post whose effect had not committed; generated content remains unchanged.</summary>
    ConversationPostingSuppressedByDeletion = 24,
}
