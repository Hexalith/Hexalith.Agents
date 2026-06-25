using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Deferred placeholder for <see cref="IAuditEvidenceGateway"/> that keeps the DI graph complete and the UI project
/// buildable before the live audit read-model path is wired. Returns the fail-closed
/// <see cref="AuditEvidenceResult.NotAuthorized"/> result, so a host that has not yet bound the real read path renders
/// the permission-denied surface rather than fabricated evidence — deny, disclose nothing (AD-12), exactly as
/// <see cref="DeferredProposalDetailGateway"/> does.
/// </summary>
/// <remarks>
/// The real implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents audit read-model and lands with the
/// operational read-model/topology work (AD-16). The bUnit tests substitute the gateway with NSubstitute, so this
/// placeholder is never exercised in tests.
/// </remarks>
public sealed class DeferredAuditEvidenceGateway : IAuditEvidenceGateway
{
    /// <inheritdoc />
    public Task<AuditEvidenceResult> GetEvidenceAsync(string agentInteractionId, CancellationToken cancellationToken)
        => Task.FromResult(AuditEvidenceResult.NotAuthorized());
}
