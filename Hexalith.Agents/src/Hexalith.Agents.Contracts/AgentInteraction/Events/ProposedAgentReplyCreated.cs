namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an Agent Call (<c>AgentInteraction</c>) in Confirmation Response Mode created a Proposed Agent Reply — a
/// reviewable proposal held OUTSIDE the Conversation that awaits authorized Approver action — the Confirmation-mode
/// counterpart to <see cref="AgentResponsePosted"/> (AC1, AC2; FR-13, FR-14; AD-5, AD-6, AD-13). This is a durable success
/// event (NOT an <c>IRejectionEvent</c>): it transitions the interaction status to
/// <see cref="AgentInteractionStatus.ProposalCreated"/>. This event <b>is</b> the AC1 Audit Evidence that a proposal was
/// created.
/// </summary>
/// <remarks>
/// The event carries the deterministic aggregate id and the safe <see cref="AgentProposedReplyEvidence"/> — the
/// deterministic <c>ProposalId</c>, the opaque <c>SourceConversationId</c>, the proposed <c>ProposedVersionId</c>, the
/// policy-snapshot versions, and the optional <c>ExpiresAt</c>. <b>It NEVER carries the generated content (AD-14):</b> the
/// content's sole durable home remains the Story 2.4 <see cref="AgentOutputGenerated"/> event/version; the proposal
/// references the version <em>id</em> only. There is no wall-clock field — creation time is the EventStore event metadata
/// (AD-3). Mirrors <see cref="AgentResponsePosted"/>.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Evidence">The safe proposal evidence (ids only; never content).</param>
public record ProposedAgentReplyCreated(
    string AgentInteractionId,
    AgentProposedReplyEvidence Evidence) : IEventPayload;
