namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The server-assembled input to the pure generation decision (AC2, AC3, AC4; AD-3). The orchestration assembles this
/// from a trusted conversation re-read, the catalog budget/timeout read, the provider invocation, and the content-safety
/// gate, then puts it on <see cref="Commands.GenerateAgentOutput"/>; the pure aggregate decides on it and never reads any
/// dependency itself (AD-3). It mirrors <see cref="AgentInteractionContextMeasurement"/> as the server→aggregate carrier.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sensitive content (AD-14):</b> <see cref="GeneratedContent"/> is conversation-derived content of the same class as
/// the caller <c>Prompt</c>. It is carried here ONLY as the transport into the aggregate so the durable success event can
/// become its sole home — exactly as the prompt is carried on the request command. The orchestrator populates it ONLY for
/// a <see cref="AgentGenerationOutcome.Succeeded"/> outcome; for every failure outcome (including a safety block) it is
/// <see langword="null"/>, so unsafe/failed content never reaches the aggregate and no approvable version is ever built
/// (AD-5, AD-14).
/// </para>
/// <para>
/// Every other member is a safe id/numeric. <see cref="AttemptId"/> is deterministic so a retried generation dedupes
/// (AD-13). When the outcome is a pre-invocation failure (e.g. provider disabled / invalid context) the token counts are
/// <c>0</c>.
/// </para>
/// </remarks>
/// <param name="Outcome">The server-assembled outcome classification the aggregate decides on.</param>
/// <param name="AttemptId">The deterministic generation attempt identifier (reused across retries — AD-13).</param>
/// <param name="ProviderId">The safe provider identifier the attempt targeted (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The safe model identifier the attempt targeted (a reference, not a secret — AD-9).</param>
/// <param name="ProviderCapabilityVersion">The provider capability version backing the attempt.</param>
/// <param name="ContentSafetyPolicyVersion">The Content Safety Policy version evaluated against the content.</param>
/// <param name="GeneratedContent">The generated content (sensitive — non-null ONLY on <see cref="AgentGenerationOutcome.Succeeded"/>; AD-14).</param>
/// <param name="PromptTokenCount">The prompt/input token usage (<c>0</c> when the provider produced none).</param>
/// <param name="OutputTokenCount">The generated-output token usage (<c>0</c> when the provider produced none).</param>
public record AgentOutputGenerationResult(
    AgentGenerationOutcome Outcome,
    string AttemptId,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int ContentSafetyPolicyVersion,
    string? GeneratedContent,
    int PromptTokenCount,
    int OutputTokenCount);
