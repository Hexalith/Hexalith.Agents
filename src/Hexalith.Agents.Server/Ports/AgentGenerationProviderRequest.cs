namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal request to the provider-generation port (Story 2.4; AC1; AD-9, AD-14). It carries ONLY safe inputs
/// the adapter needs to invoke the selected Provider/model — never a provider SDK type, credential, or raw payload (the
/// adapter resolves credentials behind its own boundary).
/// </summary>
/// <remarks>
/// <see cref="ContextPayload"/> is the assembled model input — the re-read Source Conversation content (whose latest
/// message is the caller's prompt) — sensitive conversation-derived content held ONLY transiently in memory for the
/// invocation and never persisted, put on a command/event/view, or logged (AD-14). <see cref="AttemptId"/> is deterministic so the adapter
/// can dedupe a retried invocation (AD-13). The timeout/retry budget comes from the Provider catalog entry.
/// </remarks>
/// <param name="ProviderId">The selected stable provider identifier (a reference, not a secret — AD-9).</param>
/// <param name="ModelId">The selected stable model identifier (a reference, not a secret — AD-9).</param>
/// <param name="ProviderCapabilityVersion">The provider capability version backing the invocation.</param>
/// <param name="ContextPayload">The assembled model input (the re-read Source Conversation content) — sensitive, in-memory only (AD-14).</param>
/// <param name="MaxOutputTokenLimit">The configured max-output token limit (the generation budget).</param>
/// <param name="RequestTimeoutMilliseconds">The bounded per-request timeout from the catalog timeout policy.</param>
/// <param name="MaxRetries">The bounded retry count from the catalog timeout policy.</param>
/// <param name="AttemptId">The deterministic generation attempt identifier (reused across retries — AD-13).</param>
public sealed record AgentGenerationProviderRequest(
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    string ContextPayload,
    int MaxOutputTokenLimit,
    int RequestTimeoutMilliseconds,
    int MaxRetries,
    string AttemptId);
