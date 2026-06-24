namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an authorized Approver regenerated a pending Proposed Agent Reply, appending a new immutable regenerated
/// version that passed Content Safety Policy and preserves every prior generated/edited/regenerated version (AC1, AC2, AC4;
/// FR-14, FR-16; AD-5, AD-13). This is a durable success event (NOT an <c>IRejectionEvent</c>): it transitions the
/// interaction status to <see cref="AgentInteractionStatus.ProposalRegenerated"/> and the proposal sub-state to
/// <see cref="ProposedAgentReplyState.Regenerated"/>. This event <b>is</b> the AC1/AC2/AC4 Audit Evidence that a
/// regeneration happened.
/// </summary>
/// <remarks>
/// <b>Content-bearing (AD-14):</b> like <see cref="AgentOutputGenerated"/> and <see cref="ProposedAgentReplyEdited"/>, this
/// event carries the new <see cref="AgentGeneratedVersion"/> including its regenerated <c>GeneratedContent</c> — its
/// legitimate, payload-protected durable home. The companion <see cref="Evidence"/> is the safe, content-free audit record
/// (ids + provider/model/policy versions + policy basis only). There is no wall-clock field — regeneration time is the
/// EventStore event metadata (AD-3). Mirrors <see cref="ProposedAgentReplyEdited"/> (a content-bearing version + safe evidence).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="RegeneratedVersion">The new immutable regenerated version (the approvable unit; carries the regenerated content — AD-14).</param>
/// <param name="Evidence">The safe regeneration evidence (ids + provider/model/policy versions + policy basis only; never content).</param>
public record ProposedAgentReplyRegenerated(
    string AgentInteractionId,
    AgentGeneratedVersion RegeneratedVersion,
    AgentProposedReplyRegenerationEvidence Evidence) : IEventPayload;
