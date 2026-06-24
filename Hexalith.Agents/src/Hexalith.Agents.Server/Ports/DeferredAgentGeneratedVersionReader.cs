using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IAgentGeneratedVersionReader"/> registered by default so the Server DI graph is complete and
/// compiles cleanly while the live binding to the AgentInteraction read-model (<c>AgentInteractionState.GeneratedVersions</c>)
/// stays deferred (mirroring the other deferred readers). It is never exercised by this story's unit tests, which
/// substitute the seam.
/// </summary>
/// <remarks>
/// This placeholder <b>fails closed by returning</b> <see cref="AgentGeneratedVersionReadResult.NotAvailable"/>: with no
/// content resolvable, the default graph cannot read the generated content and therefore cannot post — posting fails
/// closed to <c>AdapterFailure</c>. This keeps content-bearing workflows disabled until the live read-model + content
/// protection are wired (AD-14: content-bearing workflows stay disabled until protection is wired).
/// </remarks>
public sealed class DeferredAgentGeneratedVersionReader : IAgentGeneratedVersionReader
{
    /// <inheritdoc />
    public Task<AgentGeneratedVersionReadResult> ReadSelectedVersionAsync(string tenantId, string agentInteractionId, CancellationToken ct)
        => Task.FromResult(AgentGeneratedVersionReadResult.NotAvailable);

    /// <inheritdoc />
    public Task<AgentGeneratedVersionReadResult> ReadVersionAsync(string tenantId, string agentInteractionId, string versionId, CancellationToken ct)
        => Task.FromResult(AgentGeneratedVersionReadResult.NotAvailable);
}
