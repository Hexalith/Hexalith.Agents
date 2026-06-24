namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Regenerates a pending Proposed Agent Reply on an existing Confirmation-mode Agent Call (<c>AgentInteraction</c>),
/// appending a new immutable regenerated version and recording the terminal regeneration decision — outside the
/// Conversation, never as a Conversation Message (AC1–AC4; FR-14, FR-16; AD-3, AD-5, AD-13, AD-14). The pure aggregate
/// validates the preconditions (the proposal must be pending/edited/regenerated, AC4 — a terminal proposal never invokes the
/// provider) and maps the deterministic outcome carried in <see cref="Result"/> to the success/failure event — it never
/// resolves authorization, invokes the provider, derives the version id, or reads any dependency itself.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Result"/> is <b>server-assembled</b> by <c>AgentInteractionProposalRegenerationOrchestrator</c>
/// (Story 3.4), which resolves regeneration-time approver authorization, re-reads the same Source Conversation, re-invokes
/// the provider behind its adapter, runs the content-safety gate, and derives the deterministic regenerated-version id. Any
/// client-supplied value is stripped/overwritten by the orchestrator (AD-3 round-trip).
/// </para>
/// <para>
/// Like the generate/edit commands, <b>this command carries content</b> on a success outcome — the freshly regenerated
/// content rides into the aggregate on <see cref="Result"/>.<c>RegeneratedVersion</c> (its legitimate, payload-protected
/// write-path home — AD-14); on any failure the version is <see langword="null"/> and no content crosses the boundary. The
/// interaction's deterministic identity is the command envelope's <c>AggregateId</c>; <see cref="AgentInteractionId"/>
/// mirrors it (the create/post/edit commands' redundant-id precedent).
/// </para>
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the proposal regeneration targets (mirrors the envelope's aggregate id).</param>
/// <param name="Result">The server-assembled proposal-regeneration outcome the aggregate decides on (carries the regenerated version + content on success).</param>
public record RegenerateProposedAgentReply(
    string AgentInteractionId,
    AgentProposalRegenerationResult Result);
