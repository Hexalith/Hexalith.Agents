namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an authorized Approver edited a pending Proposed Agent Reply, appending a new immutable edited version that
/// preserves every prior generated/edited version (AC1, AC4; FR-14, FR-15; AD-5, AD-13). This is a durable success event
/// (NOT an <c>IRejectionEvent</c>): it transitions the interaction status to
/// <see cref="AgentInteractionStatus.ProposalEdited"/> and the proposal sub-state to
/// <see cref="ProposedAgentReplyState.Edited"/>. This event <b>is</b> the AC1/AC4 Audit Evidence that an edit happened.
/// </summary>
/// <remarks>
/// <b>Content-bearing (AD-14):</b> like <see cref="AgentOutputGenerated"/>, this event carries the new
/// <see cref="AgentGeneratedVersion"/> including its edited <c>GeneratedContent</c> — its legitimate, payload-protected
/// durable home. The companion <see cref="Evidence"/> is the safe, content-free audit record (ids + policy basis only).
/// There is no wall-clock field — edit time is the EventStore event metadata (AD-3). Mirrors
/// <see cref="ProposedAgentReplyCreated"/> (with an added content-bearing version, like <see cref="AgentOutputGenerated"/>).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="EditedVersion">The new immutable edited version (the approvable unit; carries the edited content — AD-14).</param>
/// <param name="Evidence">The safe edit evidence (ids + policy basis only; never content).</param>
public record ProposedAgentReplyEdited(
    string AgentInteractionId,
    AgentGeneratedVersion EditedVersion,
    AgentProposedReplyEditEvidence Evidence) : IEventPayload;
