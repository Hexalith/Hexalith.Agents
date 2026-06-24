using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;
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
}
