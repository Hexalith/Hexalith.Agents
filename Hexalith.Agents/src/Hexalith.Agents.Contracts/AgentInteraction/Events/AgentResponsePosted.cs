namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an Agent Call (<c>AgentInteraction</c>) in Automatic Response Mode posted its generated response as a
/// Conversation Message authored by the Agent Party identity — the terminal success state for an automatic interaction
/// (AC1, AC2; FR-11, FR-19, FR-20; AD-6, AD-7, AD-13). This is a durable success event (NOT an <c>IRejectionEvent</c>):
/// it transitions the interaction status to <see cref="AgentInteractionStatus.Posted"/>.
/// </summary>
/// <remarks>
/// The event carries the deterministic aggregate id and the safe <see cref="AgentPostedMessageEvidence"/> — the
/// Conversations-owned <c>MessageId</c>/<c>ConversationId</c>, the Agent's stable <c>PartyId</c>, and the posted
/// <c>VersionId</c>. <b>It NEVER carries the generated content (AD-14):</b> the content's sole durable home remains the
/// Story 2.4 <see cref="AgentOutputGenerated"/> event/version. There is no wall-clock field — post time is the EventStore
/// event metadata (AD-3). Mirrors <see cref="AgentOutputGenerated"/>.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Evidence">The safe posted-message evidence (ids only; never content).</param>
public record AgentResponsePosted(
    string AgentInteractionId,
    AgentPostedMessageEvidence Evidence) : IEventPayload;
