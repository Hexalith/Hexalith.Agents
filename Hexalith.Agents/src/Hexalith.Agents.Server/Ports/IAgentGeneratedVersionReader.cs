using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the posting orchestration uses to read the selected generated version to post — its
/// <c>VersionId</c> and <c>GeneratedContent</c> (Story 2.5; AC2; AD-3, AD-14). In V1 automatic mode this is the latest/only
/// generated version on the interaction's append-only <c>GeneratedVersions</c> history. The live binding to the
/// AgentInteraction read-model is deferred so the orchestration's decision logic stays fully unit-testable; the default DI
/// graph binds the fail-closed <see cref="DeferredAgentGeneratedVersionReader"/>.
/// </summary>
/// <remarks>
/// <b>Sensitive content (AD-14):</b> the returned <c>GeneratedContent</c> is conversation-derived content read ONLY here,
/// handed straight to the message poster, and NEVER placed on the posting command/result/event/state-posting-fields or
/// logs. The deferred default returns not-available (fail closed) so the default graph cannot read content and therefore
/// cannot post — content-bearing workflows stay disabled until the live read-model + protection are wired. On any failure
/// / not-found it returns <see cref="AgentGeneratedVersionReadResult.NotAvailable"/> so posting fails closed to
/// <c>AdapterFailure</c> (AD-12).
/// </remarks>
public interface IAgentGeneratedVersionReader
{
    /// <summary>Reads the selected generated version (id + content) to post, fail-closed (AC2).</summary>
    /// <param name="tenantId">The interaction's tenant scope (cross-tenant reads fail closed).</param>
    /// <param name="agentInteractionId">The interaction whose selected generated version is read.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The selected version's id + content, or a fail-closed not-available result.</returns>
    Task<AgentGeneratedVersionReadResult> ReadSelectedVersionAsync(string tenantId, string agentInteractionId, CancellationToken ct);

    /// <summary>Reads an exact generated/edited/regenerated version by id, fail-closed.</summary>
    /// <param name="tenantId">The interaction's tenant scope.</param>
    /// <param name="agentInteractionId">The interaction whose version is read.</param>
    /// <param name="versionId">The exact selected version id to read.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The exact version's id + content, or a fail-closed not-available result.</returns>
    Task<AgentGeneratedVersionReadResult> ReadVersionAsync(string tenantId, string agentInteractionId, string versionId, CancellationToken ct);
}
