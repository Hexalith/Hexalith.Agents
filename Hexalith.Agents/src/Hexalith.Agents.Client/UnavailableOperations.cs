using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;

namespace Hexalith.Agents.Client;

internal sealed class UnavailableProviderCatalogOperations : IProviderCatalogOperations
{
    public ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>> ListEntriesAsync(bool includeDisabled, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Typed<ProviderCatalogInspectionResult>();

    public ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>> GetEntryAsync(string providerId, string modelId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
    {
        Validate(providerId);
        Validate(modelId);
        return Unavailable.Typed<ProviderCatalogInspectionResult>();
    }

    public ValueTask<AgentOperationResult> CreateEntryAsync(CreateProviderModelEntry command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> UpdateEntryAsync(UpdateProviderModelEntry command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> EnableEntryAsync(EnableProviderModelEntry command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> DisableEntryAsync(DisableProviderModelEntry command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    private static void Validate(string value)
        => ArgumentException.ThrowIfNullOrWhiteSpace(value);
}

internal sealed class UnavailableAgentAdministrationOperations : IAgentAdministrationOperations
{
    public ValueTask<AgentOperationResult<AgentInspectionResult>> GetStatusAsync(string agentId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Agent(agentId);

    public ValueTask<AgentOperationResult<AgentInspectionResult>> GetConfigurationAsync(string agentId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Agent(agentId);

    public ValueTask<AgentOperationResult> CreateAsync(CreateAgent command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> UpdateConfigurationAsync(UpdateAgentConfiguration command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> LinkPartyIdentityAsync(LinkAgentPartyIdentity command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> ReplacePartyIdentityAsync(ReplaceAgentPartyIdentity command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> SelectProviderModelAsync(SelectAgentProviderModel command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> ConfigureResponseModeAsync(ConfigureAgentResponseMode command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> ConfigureApproverPolicyAsync(ConfigureAgentApproverPolicy command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> ConfigureContentSafetyPolicyAsync(ConfigureAgentContentSafetyPolicy command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> RecordLaunchReadinessAsync(RecordAgentLaunchReadiness command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> EnableProductionLikeGenerationAsync(EnableProductionLikeGeneration command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> ActivateAsync(ActivateAgent command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    public ValueTask<AgentOperationResult> DisableAsync(DisableAgent command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Command(command);

    private static ValueTask<AgentOperationResult<AgentInspectionResult>> Agent(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return Unavailable.Typed<AgentInspectionResult>();
    }
}

internal sealed class UnavailableAgentInteractionOperations : IAgentInteractionOperations
{
    public ValueTask<AgentOperationResult<AgentCallRequestResult>> RequestAsync(RequestAgentInteraction command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.TypedCommand<AgentCallRequestResult>(command);

    public ValueTask<AgentOperationResult<AgentInteractionInspectionResult>> GetStatusAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Interaction<AgentInteractionInspectionResult>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentInteractionGateEvidenceResult>> GetGateEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Interaction<AgentInteractionGateEvidenceResult>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentInteractionContextEvidenceResult>> GetContextEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Interaction<AgentInteractionContextEvidenceResult>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentGenerationAttemptEvidence>> GetGenerationEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Interaction<AgentGenerationAttemptEvidence>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentPostedMessageEvidence>> GetPostingEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Interaction<AgentPostedMessageEvidence>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentProposalApprovalEvidenceResult>> GetApprovalEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Interaction<AgentProposalApprovalEvidenceResult>(agentInteractionId);

    private static ValueTask<AgentOperationResult<T>> Interaction<T>(string agentInteractionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentInteractionId);
        return Unavailable.Typed<T>();
    }
}

internal sealed class UnavailableProposalWorkflowOperations : IProposalWorkflowOperations
{
    public ValueTask<AgentOperationResult<PendingProposalsResult>> ListAsync(bool includeHistorical, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.Typed<PendingProposalsResult>();

    public ValueTask<AgentOperationResult<ProposalDetailResult>> GetDetailAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Interaction<ProposalDetailResult>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentProposalEditResult>> EditAsync(EditProposedAgentReply command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.TypedCommand<AgentProposalEditResult>(command);

    public ValueTask<AgentOperationResult<AgentProposalRegenerationResult>> RegenerateAsync(RegenerateProposedAgentReply command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.TypedCommand<AgentProposalRegenerationResult>(command);

    public ValueTask<AgentOperationResult<AgentProposalApprovalResult>> ApproveAsync(ApproveProposedAgentReply command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.TypedCommand<AgentProposalApprovalResult>(command);

    public ValueTask<AgentOperationResult<AgentProposalRejectionResult>> RejectAsync(RejectProposedAgentReply command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.TypedCommand<AgentProposalRejectionResult>(command);

    public ValueTask<AgentOperationResult<AgentProposalAbandonmentResult>> AbandonAsync(AbandonProposedAgentReply command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.TypedCommand<AgentProposalAbandonmentResult>(command);

    public ValueTask<AgentOperationResult<AgentProposalExpiryResult>> ExpireAsync(ExpireProposedAgentReply command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Unavailable.TypedCommand<AgentProposalExpiryResult>(command);

    private static ValueTask<AgentOperationResult<T>> Interaction<T>(string agentInteractionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentInteractionId);
        return Unavailable.Typed<T>();
    }
}

internal sealed class UnavailableAgentStatusOperations : IAgentStatusOperations
{
    public ValueTask<AgentOperationResult<AgentReadinessStatus>> GetAgentReadinessAsync(string agentId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Id<AgentReadinessStatus>(agentId);

    public ValueTask<AgentOperationResult<AgentLaunchReadinessView>> GetAgentLaunchReadinessAsync(string agentId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Id<AgentLaunchReadinessView>(agentId);

    public ValueTask<AgentOperationResult<ProviderModelReadinessStatus>> GetProviderModelReadinessAsync(string providerId, string modelId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        return Unavailable.Typed<ProviderModelReadinessStatus>();
    }

    public ValueTask<AgentOperationResult<AgentCallOperationStatus>> GetCallStatusAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Id<AgentCallOperationStatus>(agentInteractionId);

    public ValueTask<AgentOperationResult<ProposalOperationStatus>> GetProposalStatusAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Id<ProposalOperationStatus>(agentInteractionId);

    public ValueTask<AgentOperationResult<AuditAvailabilityStatus>> GetAuditAvailabilityAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Id<AuditAvailabilityStatus>(agentInteractionId);

    private static ValueTask<AgentOperationResult<T>> Id<T>(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return Unavailable.Typed<T>();
    }
}

internal sealed class UnavailableAgentAuditOperations : IAgentAuditOperations
{
    public ValueTask<AgentOperationResult<AgentInteractionGateEvidenceResult>> GetGateEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Evidence<AgentInteractionGateEvidenceResult>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentInteractionContextEvidenceResult>> GetContextEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Evidence<AgentInteractionContextEvidenceResult>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentGenerationAttemptEvidence>> GetGenerationEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Evidence<AgentGenerationAttemptEvidence>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentPostedMessageEvidence>> GetPostingEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Evidence<AgentPostedMessageEvidence>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentProposalEditEvidenceResult>> GetProposalEditEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Evidence<AgentProposalEditEvidenceResult>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentProposalRegenerationEvidenceResult>> GetProposalRegenerationEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Evidence<AgentProposalRegenerationEvidenceResult>(agentInteractionId);

    public ValueTask<AgentOperationResult<AgentProposalApprovalEvidenceResult>> GetProposalApprovalEvidenceAsync(string agentInteractionId, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => Evidence<AgentProposalApprovalEvidenceResult>(agentInteractionId);

    private static ValueTask<AgentOperationResult<T>> Evidence<T>(string agentInteractionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentInteractionId);
        return Unavailable.Typed<T>();
    }
}

internal static class Unavailable
{
    public static ValueTask<AgentOperationResult> Command<T>(T command)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(command);
        return new ValueTask<AgentOperationResult>(AgentOperationResult.Unavailable());
    }

    public static ValueTask<AgentOperationResult<T>> TypedCommand<T>(object command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Typed<T>();
    }

    public static ValueTask<AgentOperationResult<T>> Typed<T>()
        => new(AgentOperationResult<T>.Unavailable());
}
