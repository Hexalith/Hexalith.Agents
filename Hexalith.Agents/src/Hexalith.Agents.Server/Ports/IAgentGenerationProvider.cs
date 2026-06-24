using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the generation orchestration uses to invoke the selected Provider/model — the FIRST real model
/// invocation seam in the module (Story 2.4; AC1, AC3; AD-9, AD-18). The live provider-SDK adapter stays adapter-local
/// and deferred (Stack: "Provider SDK Deferred; adapter-local when selected"); the default DI graph binds the
/// fail-closed <see cref="DeferredAgentGenerationProvider"/>.
/// </summary>
/// <remarks>
/// Provider SDK types, credentials, raw payloads, and provider-specific errors stay BEHIND this boundary (AD-9, AD-14):
/// <see cref="AgentGenerationProviderRequest"/> carries only safe inputs (ids, capability version, the assembled model
/// input held in memory, and the timeout/retry budget) and <see cref="AgentGenerationProviderResult"/> carries only safe
/// outputs (a coarse outcome, the generated content held in memory, and token usage). Keeping this a port preserves the
/// AD-3 round-trip: the pure aggregate never invokes a provider; the outcome reaches it only through the trusted command.
/// </remarks>
public interface IAgentGenerationProvider
{
    /// <summary>Invokes the selected Provider/model and maps the result to a safe, fail-closed outcome (AC1, AC3).</summary>
    /// <param name="request">The safe generation request (ids, capability version, in-memory model input, timeout/retry budget, deterministic attempt id).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe generation result (outcome + optional in-memory generated content + token usage).</returns>
    Task<AgentGenerationProviderResult> GenerateAsync(AgentGenerationProviderRequest request, CancellationToken ct);
}
