namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Builds the Conversation context for an existing, authorized Agent Call (<c>AgentInteraction</c>) within safe bounds
/// before any provider invocation (AC1–AC4; FR-9; AD-3, AD-11, AD-12). The pure aggregate computes the deterministic
/// budget decision from the supplied <see cref="Measurement"/> only — it never reads any dependency itself.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Measurement"/> is <b>server-assembled from a trusted live Conversations read + token measurement +
/// Provider-catalog budget read</b> by <c>AgentInteractionContextOrchestrator</c> (Story 2.3): the orchestration reads
/// the authorized visible timeline through <c>IConversationClient.GetConversationAsync</c>, measures its token count,
/// reads the model budget from the catalog, resolves the context policy, and dispatches this command. Any
/// client-supplied measurement is stripped/overwritten by the orchestrator — a direct-gateway client cannot forge a
/// fit (AD-3 round-trip).
/// </para>
/// <para>
/// The interaction's deterministic identity is the command envelope's <c>AggregateId</c> and the tenant scope is the
/// envelope's <c>TenantId</c>; <see cref="AgentInteractionId"/> on the payload mirrors the gate command's redundant-id
/// precedent. The measurement carries only safe numerics/enums/references and the measured token count — never the raw
/// Conversation text (AD-14).
/// </para>
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the context build targets (mirrors the envelope's aggregate id).</param>
/// <param name="Measurement">The server-assembled context measurement the aggregate decides on.</param>
public record BuildAgentInteractionContext(
    string AgentInteractionId,
    AgentInteractionContextMeasurement Measurement);
