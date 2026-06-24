namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Posts the generated automatic response for an existing, generated Agent Call (<c>AgentInteraction</c>) and records the
/// terminal posting decision — through the Conversations-owned membership + append boundary (AC1–AC4; FR-11, FR-12, FR-19–
/// FR-21; AD-3, AD-6, AD-7, AD-13, AD-14). The pure aggregate maps the deterministic outcome carried in
/// <see cref="Result"/> to the success/failure event — it never reads the Agent Party, verifies membership, or appends to
/// a Conversation itself.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Result"/> is <b>server-assembled from a trusted Agent-Party read + selected-version read + the
/// Conversations membership ensure + the message append</b> by <c>AgentInteractionPostingOrchestrator</c> (Story 2.5).
/// Any client-supplied value is stripped/overwritten by the orchestrator (AD-3 round-trip).
/// </para>
/// <para>
/// The interaction's deterministic identity is the command envelope's <c>AggregateId</c> and the tenant scope is the
/// envelope's <c>TenantId</c>; <see cref="AgentInteractionId"/> on the payload mirrors the generation command's
/// redundant-id precedent. <b>The command carries NO generated content</b> — the content rode into the aggregate on the
/// Story 2.4 success event; posting transports only safe ids (AD-14).
/// </para>
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the posting targets (mirrors the envelope's aggregate id).</param>
/// <param name="Result">The server-assembled posting outcome the aggregate decides on (safe ids only — no content).</param>
public record PostAgentResponse(
    string AgentInteractionId,
    AgentResponsePostingResult Result);
