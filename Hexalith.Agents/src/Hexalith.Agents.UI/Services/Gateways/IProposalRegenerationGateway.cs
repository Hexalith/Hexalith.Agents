using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side write seam for an authorized Approver's regeneration of a pending Proposed Agent Reply (Story 3.4; AC1, AC3,
/// AC4; AD-15). The regenerate control depends only on this abstraction and the public display contracts; it never touches
/// <c>Hexalith.Agents.Server</c>, EventStore streams, provider SDKs, or aggregate internals. Mirrors
/// <see cref="IProposalEditGateway"/>.
/// </summary>
/// <remarks>
/// The live implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents regeneration orchestration (which
/// re-invokes the provider behind its adapter) and is deferred to the dedicated Agents read-model / BFF story (Epic 4,
/// 4.1/4.3), mirroring the deferred-binding convention. The bUnit component tests substitute this seam with NSubstitute, so
/// the <see cref="DeferredProposalRegenerationGateway"/> placeholder is never exercised in tests but keeps the DI graph
/// complete. The method returns a fail-closed result wrapper so a non-success outcome carries no version identity and never
/// throws on the happy path; a genuine cancellation propagates (AD-12, AD-14). No request or result ever carries content —
/// regeneration re-invokes the provider server-side; the regenerated content lives only on the durable version history (AD-14).
/// </remarks>
public interface IProposalRegenerationGateway
{
    /// <summary>
    /// Submits the authorized regeneration of a pending Proposed Agent Reply. Returns a structured fail-closed result; the
    /// <see cref="ProposalRegenerationResult.RegeneratedVersionId"/> is non-null only on
    /// <see cref="ProposalRegenerationStatus.Regenerated"/>.
    /// </summary>
    /// <param name="request">The safe regeneration inputs (ids only — no content; AD-14).</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight regeneration.</param>
    /// <returns>The fail-closed proposal-regeneration result.</returns>
    Task<ProposalRegenerationResult> RegenerateProposalAsync(ProposalRegenerationRequest request, CancellationToken cancellationToken);
}
