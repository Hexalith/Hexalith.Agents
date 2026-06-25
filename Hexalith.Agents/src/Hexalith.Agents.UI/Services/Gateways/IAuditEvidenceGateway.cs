using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side read seam for the support-safe audit-evidence surface (Story 4.3 AC1, AC2, AC3; AD-15). The audit page depends
/// only on this abstraction and the public Story 4.2 evidence view contracts bundled in <see cref="AuditEvidenceResult"/>;
/// it never touches <c>Hexalith.Agents.Server</c>, EventStore streams, provider SDKs, or aggregate internals. Mirrors
/// <see cref="IProposalDetailGateway"/>; the request is the single safe Agent Call id and the result is the fail-closed
/// wrapper, so a non-success outcome carries no record identity (AD-12, AD-14). Content-bearing audit stays blocked —
/// the evidence is metadata-only (Story 4.2 AC4; OQ-8).
/// </summary>
/// <remarks>
/// The live implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents audit read-model path and is
/// deferred to the operational read-model/topology work (AD-16), mirroring the deferred-binding convention. The bUnit
/// component tests substitute this seam with NSubstitute, so the <see cref="DeferredAuditEvidenceGateway"/> placeholder is
/// never exercised in tests but keeps the DI graph complete.
/// </remarks>
public interface IAuditEvidenceGateway
{
    /// <summary>
    /// Reads the authorized support-safe <see cref="AuditEvidenceResult"/> for one Agent Call. Returns a structured
    /// fail-closed result; the bundled evidence views are <see langword="null"/> on every non-success outcome
    /// (denied/unavailable/not-found never disclose a record; AD-12, AD-14).
    /// </summary>
    /// <param name="agentInteractionId">The deterministic Agent Call identifier whose audit evidence is read.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed audit-evidence read result.</returns>
    Task<AuditEvidenceResult> GetEvidenceAsync(string agentInteractionId, CancellationToken cancellationToken);
}
