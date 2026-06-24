using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IAgentGenerationProvider"/> registered by default so the Server DI graph is complete and
/// compiles cleanly while the live provider-SDK adapter stays deferred/adapter-local (Stack: "Provider SDK Deferred;
/// adapter-local when selected"). It is never exercised by this story's unit tests, which substitute the seam.
/// </summary>
/// <remarks>
/// This placeholder <b>fails closed by returning</b> <see cref="AgentGenerationProviderResult.Unavailable"/> and never
/// throws, so content-bearing generation stays disabled until a live provider adapter is wired: an accidental production
/// call yields a clean <c>GenerationFailed(ProviderUnavailable)</c> rather than a runtime fault or an unprotected model
/// invocation (AD-12, AD-14; FR-21).
/// </remarks>
public sealed class DeferredAgentGenerationProvider : IAgentGenerationProvider
{
    /// <inheritdoc />
    public Task<AgentGenerationProviderResult> GenerateAsync(AgentGenerationProviderRequest request, CancellationToken ct)
        => Task.FromResult(AgentGenerationProviderResult.Unavailable);
}
