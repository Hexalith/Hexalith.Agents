namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Edits a pending Proposed Agent Reply on an existing Confirmation-mode Agent Call (<c>AgentInteraction</c>), appending a
/// new immutable edited version and recording the terminal edit decision — outside the Conversation, never as a
/// Conversation Message (AC1–AC4; FR-14, FR-15; AD-3, AD-5, AD-13, AD-14). The pure aggregate validates the preconditions
/// (the proposal must be pending/edited, AC2) and maps the deterministic outcome carried in <see cref="Result"/> to the
/// success/failure event — it never resolves authorization, derives the version id, or reads any dependency itself.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Result"/> is <b>server-assembled</b> by <c>AgentInteractionProposalEditOrchestrator</c> (Story 3.3),
/// which resolves edit-time approver authorization, derives the deterministic edited-version id, and assembles the result.
/// Any client-supplied value is stripped/overwritten by the orchestrator (AD-3 round-trip).
/// </para>
/// <para>
/// Unlike the create command, <b>this command DOES carry content</b> — the edited content originates from the user and
/// rides into the aggregate on <see cref="Result"/>.<c>EditedVersion</c> (its legitimate, payload-protected write-path
/// home — AD-14). The interaction's deterministic identity is the command envelope's <c>AggregateId</c>;
/// <see cref="AgentInteractionId"/> mirrors it (the create/post commands' redundant-id precedent).
/// </para>
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the proposal edit targets (mirrors the envelope's aggregate id).</param>
/// <param name="Result">The server-assembled proposal-edit outcome the aggregate decides on (carries the edited version + content).</param>
public record EditProposedAgentReply(
    string AgentInteractionId,
    AgentProposalEditResult Result);
