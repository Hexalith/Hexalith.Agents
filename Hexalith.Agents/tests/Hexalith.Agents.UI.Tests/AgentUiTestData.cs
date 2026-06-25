using System.Collections.Generic;
using System.Linq;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.UI.Services.Gateways;

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

    // ===== Story 3.7 proposal-detail factories =====

    /// <summary>A safe content-free version summary for the version-history / detail tests.</summary>
    public static ProposalVersionSummary VersionSummary(
        string versionId = "version-1",
        AgentGenerationKind kind = AgentGenerationKind.Generated,
        string providerId = "openai",
        string modelId = "gpt-x",
        string? sourceVersionId = null,
        string? editorPartyId = null,
        string? createdAt = "2026-06-24T08:00:00Z",
        bool isApproved = false,
        bool isPosted = false)
        => new(versionId, kind, providerId, modelId, sourceVersionId, editorPartyId, createdAt, isApproved, isPosted);

    /// <summary>A safe proposal-detail view; tests use <c>with</c> expressions or the named params to vary a single field.</summary>
    public static ProposalDetailView Detail(
        string agentInteractionId = "interaction-1",
        string proposalId = "proposal-1",
        ProposedAgentReplyState state = ProposedAgentReplyState.Pending,
        AgentInteractionStatus interactionStatus = AgentInteractionStatus.ProposalCreated,
        string sourceConversationId = "conversation-1",
        string callerPartyId = "caller-1",
        string agentId = "agent-1",
        bool needsCurrentUserAction = true,
        string selectedVersionId = "version-1",
        AgentResponseMode responseMode = AgentResponseMode.Confirmation,
        string providerId = "openai",
        string modelId = "gpt-x",
        int approverPolicyVersion = 1,
        int contentSafetyPolicyVersion = 1,
        string? expiresAt = null,
        string? createdAt = "2026-06-24T08:00:00Z",
        string? approvedVersionId = null,
        string? approvedAt = null,
        string? postedAt = null,
        IReadOnlyList<ProposalVersionSummary>? versions = null)
        => new(
            AgentInteractionId: agentInteractionId,
            ProposalId: proposalId,
            State: state,
            InteractionStatus: interactionStatus,
            SourceConversationId: sourceConversationId,
            CallerPartyId: callerPartyId,
            AgentId: agentId,
            NeedsCurrentUserAction: needsCurrentUserAction,
            SelectedVersionId: selectedVersionId,
            ResponseMode: responseMode,
            ProviderId: providerId,
            ModelId: modelId,
            ApproverPolicyVersion: approverPolicyVersion,
            ContentSafetyPolicyVersion: contentSafetyPolicyVersion,
            ExpiresAt: expiresAt,
            CreatedAt: createdAt,
            ApprovedVersionId: approvedVersionId,
            ApprovedAt: approvedAt,
            PostedAt: postedAt,
            Versions: versions ?? [VersionSummary()]);

    /// <summary>Wraps a detail view in a successful read result (defaults to a single pending generated version).</summary>
    public static ProposalDetailResult DetailResult(ProposalDetailView? detail = null)
        => ProposalDetailResult.Success(detail ?? Detail());

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

    // ===== Story 4.3 operational-status + audit-evidence factories =====

    /// <summary>A safe operational-status summary; tests use <c>with</c> expressions or named params to vary a single field.</summary>
    public static AgentOperationalStatusSummaryView OperationalStatusSummary(
        AgentReadinessStatus agentReadiness = AgentReadinessStatus.Callable,
        IReadOnlyList<AgentActivationBlocker>? readinessBlockers = null,
        IReadOnlyList<string>? auditGovernanceBlockers = null,
        AuditAvailabilityStatus auditAvailability = AuditAvailabilityStatus.AuditAvailable,
        IReadOnlyList<AgentCallOutcomeCount>? recentCallOutcomes = null,
        IReadOnlyList<ProposalOutcomeCount>? proposalOutcomes = null,
        int pendingProposalCount = 2,
        string? generatedAt = "2026-06-24T12:00:00Z")
        => new(
            AgentReadiness: agentReadiness,
            ReadinessBlockers: readinessBlockers ?? [],
            AuditGovernanceBlockers: auditGovernanceBlockers ?? [AgentAuditGovernanceReadiness.RetentionLegalHoldExportDeletionPolicyUnresolved],
            AuditAvailability: auditAvailability,
            RecentCallOutcomes: recentCallOutcomes ?? [new AgentCallOutcomeCount(AgentCallOperationStatus.Generated, 3), new AgentCallOutcomeCount(AgentCallOperationStatus.Denied, 1)],
            ProposalOutcomes: proposalOutcomes ?? [new ProposalOutcomeCount(ProposalOperationStatus.Posted, 4), new ProposalOutcomeCount(ProposalOperationStatus.PostingFailed, 1)],
            PendingProposalCount: pendingProposalCount,
            GeneratedAt: generatedAt);

    /// <summary>Wraps a summary in a successful read result.</summary>
    public static AgentOperationalStatusSummaryResult OperationalStatusResult(AgentOperationalStatusSummaryView? summary = null)
        => AgentOperationalStatusSummaryResult.Success(summary ?? OperationalStatusSummary());

    /// <summary>A safe approval/posting evidence view for the audit panel tests.</summary>
    public static AgentProposalApprovalEvidenceView ApprovalEvidence(
        string agentInteractionId = "interaction-1",
        string proposalId = "proposal-1",
        ProposedAgentReplyState state = ProposedAgentReplyState.Posted,
        string approvedVersionId = "version-1",
        string approverPartyId = "approver-1",
        string postedConversationMessageId = "posted-message-1",
        string? approvedAt = "2026-06-24T09:00:00Z",
        string? postedAt = "2026-06-24T09:01:00Z")
        => new(
            AgentInteractionId: agentInteractionId,
            ProposalId: proposalId,
            State: state,
            ApprovedVersionId: approvedVersionId,
            ApproverPartyId: approverPartyId,
            SourceConversationId: "conversation-1",
            AgentPartyId: "agent-party-1",
            MessageId: "message-1",
            IdempotencyKey: "idempotency-1",
            PostedConversationMessageId: postedConversationMessageId,
            ApproverPolicyVersion: 1,
            PolicyBasisVerdict: ApproverPolicyValidationStatus.Valid,
            DisclosureCategory: ApproverPolicyBasisDisclosure.OperatorOnly,
            ApprovalFailureReason: default,
            PostingFailureReason: default,
            ApprovedAt: approvedAt,
            PostedAt: postedAt);

    /// <summary>Wraps audit evidence in a successful read result (defaults to a posted, audit-available interaction).</summary>
    public static AuditEvidenceResult AuditEvidence(
        ProposalDetailView? detail = null,
        AgentProposalApprovalEvidenceView? approval = null,
        AuditAvailabilityStatus availability = AuditAvailabilityStatus.AuditAvailable)
        => AuditEvidenceResult.Success(
            detail ?? Detail(state: ProposedAgentReplyState.Posted, approvedVersionId: "version-1", approvedAt: "2026-06-24T09:00:00Z", postedAt: "2026-06-24T09:01:00Z"),
            approval ?? ApprovalEvidence(),
            availability);
}
