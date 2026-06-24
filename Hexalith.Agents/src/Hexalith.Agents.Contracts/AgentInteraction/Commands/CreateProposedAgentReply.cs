namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Creates a Proposed Agent Reply for an existing, generated Agent Call (<c>AgentInteraction</c>) in Confirmation Response
/// Mode and records the terminal creation decision — outside the Conversation, never as a Conversation Message (AC1–AC4;
/// FR-13, FR-14, FR-27; AD-3, AD-5, AD-6, AD-13, AD-14). The pure aggregate maps the deterministic outcome carried in
/// <see cref="Result"/> to the success/failure event — it never reads the selected version, the expiry policy, or any
/// dependency itself.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Result"/> is <b>server-assembled from a trusted selected-version read + the optional expiry read</b> by
/// <c>AgentInteractionProposalOrchestrator</c> (Story 3.1). Any client-supplied value is stripped/overwritten by the
/// orchestrator (AD-3 round-trip).
/// </para>
/// <para>
/// The interaction's deterministic identity is the command envelope's <c>AggregateId</c> and the tenant scope is the
/// envelope's <c>TenantId</c>; <see cref="AgentInteractionId"/> on the payload mirrors the posting command's redundant-id
/// precedent. <b>The command carries NO generated content</b> — the content rode into the aggregate on the Story 2.4
/// success event; proposal creation transports only safe ids (AD-14).
/// </para>
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the proposal targets (mirrors the envelope's aggregate id).</param>
/// <param name="Result">The server-assembled proposal-creation outcome the aggregate decides on (safe ids only — no content).</param>
public record CreateProposedAgentReply(
    string AgentInteractionId,
    AgentProposalCreationResult Result);
