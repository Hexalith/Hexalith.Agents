using System.Collections.Generic;
using System.Linq;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Factory helpers for the safe display contracts used across the Agents.UI tests. Defaults are deliberately
/// minimal; tests use <c>with</c> expressions to vary a single field.
/// </summary>
internal static class AgentUiTestData
{
    public static AgentStatusView Status(
        AgentLifecycleStatus lifecycle = AgentLifecycleStatus.Draft,
        IReadOnlyList<AgentActivationBlocker>? blockers = null,
        AgentResponseMode responseMode = AgentResponseMode.Automatic,
        bool hasProviderSelection = false,
        string? providerId = null,
        string? modelId = null,
        bool hasInstructions = false,
        bool instructionsValid = false,
        bool hasApproverPolicy = false,
        ApproverPolicyBasisDisclosure approverPolicyDisclosure = ApproverPolicyBasisDisclosure.OperatorOnly,
        bool hasContentSafetyPolicy = false,
        bool hasAutomaticContentSafetyOverride = false,
        bool hasConfirmationContentSafetyOverride = false)
        => new(
            AgentId: "agent-1",
            TenantId: "tenant-1",
            DisplayName: "hexa",
            Description: null,
            Lifecycle: lifecycle,
            ConfigurationVersion: 1,
            HasInstructions: hasInstructions,
            InstructionsValid: instructionsValid,
            InstructionsVersion: hasInstructions ? 1 : 0,
            HasPartyIdentity: false,
            HasProviderSelection: hasProviderSelection,
            SelectedProviderId: providerId,
            SelectedModelId: modelId,
            ResponseMode: responseMode,
            HasApproverPolicy: hasApproverPolicy,
            ApproverPolicyDisclosure: approverPolicyDisclosure,
            ApproverPolicyVersion: hasApproverPolicy ? 1 : 0,
            HasContentSafetyPolicy: hasContentSafetyPolicy,
            ContentSafetyPolicyVersion: hasContentSafetyPolicy ? 1 : 0,
            HasAutomaticContentSafetyOverride: hasAutomaticContentSafetyOverride,
            HasConfirmationContentSafetyOverride: hasConfirmationContentSafetyOverride,
            ActivationBlockers: blockers ?? []);

    public static ProviderCatalogEntryView Entry(
        string providerId = "openai",
        string modelId = "gpt-x",
        ProviderModelStatus status = ProviderModelStatus.Enabled,
        ProviderConfigurationState configurationState = ProviderConfigurationState.Configured,
        string? configurationReferenceId = null,
        bool isSelectable = true,
        ProviderModelCapabilityFlags capabilities = ProviderModelCapabilityFlags.None,
        bool supportsTextGeneration = true)
        => new(
            ProviderId: providerId,
            ModelId: modelId,
            DisplayLabel: $"{providerId} {modelId}",
            Status: status,
            SupportsTextGeneration: supportsTextGeneration,
            ContextWindowTokenLimit: 128000,
            MaxOutputTokenLimit: 4096,
            TimeoutPolicy: new ProviderModelTimeoutPolicy(30000, 2),
            SafeCapabilityFlags: capabilities,
            ConfigurationState: configurationState,
            ConfigurationReferenceId: configurationReferenceId,
            IsSelectableForNewActiveUse: isSelectable,
            CapabilityVersion: 1);

    public static PendingProposalView PendingProposal(
        string agentInteractionId = "interaction-1",
        string proposalId = "proposal-1",
        ProposedAgentReplyState state = ProposedAgentReplyState.Pending,
        AgentInteractionStatus interactionStatus = AgentInteractionStatus.ProposalCreated,
        string sourceConversationId = "conversation-1",
        string callerPartyId = "caller-1",
        string agentId = "agent-1",
        bool needsCurrentUserAction = true,
        string proposedVersionId = "version-1",
        int approverPolicyVersion = 1,
        int contentSafetyPolicyVersion = 1,
        string? expiresAt = null,
        string? createdAt = null)
        => new(
            AgentInteractionId: agentInteractionId,
            ProposalId: proposalId,
            State: state,
            InteractionStatus: interactionStatus,
            SourceConversationId: sourceConversationId,
            CallerPartyId: callerPartyId,
            AgentId: agentId,
            NeedsCurrentUserAction: needsCurrentUserAction,
            ProposedVersionId: proposedVersionId,
            ApproverPolicyVersion: approverPolicyVersion,
            ContentSafetyPolicyVersion: contentSafetyPolicyVersion,
            ExpiresAt: expiresAt,
            CreatedAt: createdAt);

    // Builds a Success result, deriving the AC3 pending count from the rows' "needs my action" flags.
    public static PendingProposalsResult ProposalsResult(params PendingProposalView[] proposals)
        => PendingProposalsResult.Success(proposals, proposals.Count(proposal => proposal.NeedsCurrentUserAction));

    // ===== Story 3.3 edit factories =====

    /// <summary>A safe edited generated version (Kind=Edited, carrying its source/editor provenance) for the editor / version-history tests.</summary>
    public static AgentGeneratedVersion EditedVersion(
        string versionId = "edited-version-1",
        string sourceVersionId = "version-1",
        string editorPartyId = "editor-1",
        string content = "an edited reply")
        => new(
            VersionId: versionId,
            AttemptId: "edit-attempt-1",
            Kind: AgentGenerationKind.Edited,
            GeneratedContent: content,
            ProviderId: "openai",
            ModelId: "gpt-x",
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion: 1,
            PromptTokenCount: 0,
            OutputTokenCount: 0,
            SourceVersionId: sourceVersionId,
            EditorPartyId: editorPartyId);
}
