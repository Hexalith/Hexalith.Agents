using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal result of a provider-generation invocation (Story 2.4; AC1, AC3; AD-9, AD-14). It carries ONLY safe
/// outputs — a coarse <see cref="Outcome"/>, the generated content (held in memory only), and token usage — never a
/// provider SDK type, raw payload, or provider-specific error string (those stay behind the adapter; AD-9, AD-14).
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type. <see cref="GeneratedContent"/> is present only on a
/// <see cref="AgentGenerationOutcome.Succeeded"/> result and is sensitive conversation-derived content used transiently
/// in memory (the content-safety gate, then the success command); it is never logged or persisted by the orchestrator
/// outside the trusted command (AD-14). <see cref="Unavailable"/> is the fail-closed default the deferred adapter returns.
/// </remarks>
/// <param name="Outcome">The safe, coarse provider outcome (mapped to the overall generation outcome by the orchestrator).</param>
/// <param name="GeneratedContent">The generated content (non-null only on a <see cref="AgentGenerationOutcome.Succeeded"/> result), or <see langword="null"/>.</param>
/// <param name="PromptTokenCount">The prompt/input token usage (<c>0</c> when the provider produced none).</param>
/// <param name="OutputTokenCount">The generated-output token usage (<c>0</c> when the provider produced none).</param>
public sealed record AgentGenerationProviderResult(
    AgentGenerationOutcome Outcome,
    string? GeneratedContent,
    int PromptTokenCount,
    int OutputTokenCount)
{
    /// <summary>Gets the fail-closed not-available result (the deferred default) — provider unavailable, no content, no usage.</summary>
    public static AgentGenerationProviderResult Unavailable { get; } = new(AgentGenerationOutcome.ProviderUnavailable, null, 0, 0);
}
