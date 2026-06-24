namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an Agent Call (<c>AgentInteraction</c>) failed Proposed-Agent-Reply creation closed AFTER a successful,
/// safety-passing generation, durably transitioning it to a <see cref="AgentInteractionStatus.ProposalCreationFailed"/>
/// decision with safe attempt evidence (AC1, AC3, AC4; FR-13, FR-24; AD-5, AD-12, AD-14). This is a durable success event
/// (NOT an <c>IRejectionEvent</c>): a failed creation is a <em>successfully-recorded negative decision</em> and this record
/// IS the Audit Evidence — distinct from <see cref="Rejections.ProposedAgentReplyNotCreatableRejection"/>, which is used
/// only when creation cannot be evaluated at all. No approvable proposal exists.
/// </summary>
/// <remarks>
/// <see cref="Reason"/> classifies the failure (version-unavailable/adapter) and <see cref="Evidence"/> carries the safe
/// ids that were attempted (the deterministic <c>ProposalId</c>, <c>SourceConversationId</c>, <c>ProposedVersionId</c>, and
/// policy-snapshot versions) — NEVER the generated content, a raw provider/Conversations payload, a stack trace, or a
/// secret (AD-14). There is no wall-clock field — failure time is the EventStore event metadata (AD-3). Mirrors
/// <see cref="AgentResponsePostingFailed"/> (with <see cref="Reason"/> + safe-id evidence).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Reason">The safe classification of the creation failure class.</param>
/// <param name="Evidence">The safe creation-attempt evidence (ids only; never content).</param>
public record ProposedAgentReplyCreationFailed(
    string AgentInteractionId,
    AgentProposalCreationFailureReason Reason,
    AgentProposedReplyEvidence Evidence) : IEventPayload;
