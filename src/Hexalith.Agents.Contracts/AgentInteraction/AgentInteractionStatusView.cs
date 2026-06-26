using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, display/audit-ready projection of one Agent Call (<c>AgentInteraction</c>) for authorized status
/// inspection (AC2, AC4; FR-8, FR-24). It exposes the safe identity references, the coarse status, the snapshotted
/// Response Mode, and the snapshotted version numbers — and deliberately <b>never</b> the raw prompt, any
/// Conversation-derived content, an EventStore stream name, or a provider SDK detail (AD-14).
/// </summary>
/// <remarks>
/// The <c>Prompt</c> is sensitive content that lives only on the durable <see cref="Events.InteractionRequested"/>
/// event and <c>AgentInteractionState</c> — it is intentionally absent here, mirroring how the Agent Instructions
/// text is excluded from <see cref="AgentStatusView"/>. Binding this view to the EventStore read path is deferred
/// to the dedicated Agents read-model story (mirroring Story 1.2); the stable contract lands here.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Status">The coarse Agent Call status.</param>
/// <param name="AgentId">The target Agent identifier captured at request time.</param>
/// <param name="CallerPartyId">The caller's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="SourceConversationId">The source Conversation reference (an opaque reference — AD-6).</param>
/// <param name="ResponseMode">The Response Mode snapshotted at request time (AD-4).</param>
/// <param name="ConfigurationVersion">The Agent configuration version snapshotted at request time.</param>
/// <param name="InstructionsVersion">The Agent Instructions version snapshotted at request time.</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time.</param>
/// <param name="ProviderId">The selected stable safe provider identifier (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The selected stable safe model identifier (a reference, not a secret — AD-9).</param>
/// <param name="ProviderCapabilityVersion">The provider capability version snapshotted at request time.</param>
/// <param name="ContentSafetyPolicyVersion">The content-safety policy version snapshotted at request time.</param>
public record AgentInteractionStatusView(
    string AgentInteractionId,
    AgentInteractionStatus Status,
    string AgentId,
    string CallerPartyId,
    string SourceConversationId,
    AgentResponseMode ResponseMode,
    int ConfigurationVersion,
    int InstructionsVersion,
    int ApproverPolicyVersion,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int ContentSafetyPolicyVersion);
