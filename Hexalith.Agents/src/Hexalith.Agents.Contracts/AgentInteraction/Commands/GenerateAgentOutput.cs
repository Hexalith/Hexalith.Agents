namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Generates Agent output for an existing, context-ready Agent Call (<c>AgentInteraction</c>) and records the terminal
/// generation decision — respecting Provider and Content Safety policies (AC1–AC4; FR-9, FR-10, FR-12, FR-19–FR-21;
/// AD-3, AD-5, AD-9, AD-13). The pure aggregate maps the deterministic outcome carried in <see cref="Result"/> to the
/// success/failure event — it never invokes a provider or evaluates safety itself.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Result"/> is <b>server-assembled from a trusted conversation re-read + Provider-catalog budget read +
/// the provider invocation + the Content Safety gate</b> by <c>AgentInteractionGenerationOrchestrator</c> (Story 2.4):
/// the orchestration re-reads the Source Conversation content (Story 2.3 discarded it, keeping only token counts),
/// invokes the provider behind its adapter, evaluates the effective Content Safety Policy, classifies the outcome, and
/// dispatches this command. Any client-supplied value is stripped/overwritten by the orchestrator (AD-3 round-trip).
/// </para>
/// <para>
/// The interaction's deterministic identity is the command envelope's <c>AggregateId</c> and the tenant scope is the
/// envelope's <c>TenantId</c>; <see cref="AgentInteractionId"/> on the payload mirrors the context command's redundant-id
/// precedent. Generated content rides on <see cref="Result"/> ONLY on a success outcome, as the transport into the
/// aggregate whose durable success event becomes its sole home (AD-14).
/// </para>
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the generation targets (mirrors the envelope's aggregate id).</param>
/// <param name="Result">The server-assembled generation outcome the aggregate decides on.</param>
public record GenerateAgentOutput(
    string AgentInteractionId,
    AgentOutputGenerationResult Result);
