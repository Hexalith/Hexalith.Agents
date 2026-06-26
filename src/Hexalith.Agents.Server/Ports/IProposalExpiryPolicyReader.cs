using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the proposal-creation orchestration uses to read the optional Proposed-Agent-Reply expiry metadata
/// "where configured" (Story 3.1; AC1; AD-3). It returns an optional ISO-8601 expiry timestamp the orchestration records on
/// the proposal evidence; <see langword="null"/> means no expiry policy is configured. The live expiry-policy binding AND
/// expiry <em>enforcement</em> (transition to an <c>Expired</c> terminal state) are deferred to Story 3.6 — this story only
/// records the optional metadata. The default DI graph binds the fail-closed <see cref="DeferredProposalExpiryPolicyReader"/>.
/// </summary>
/// <remarks>
/// The reader/result are server-internal, not public contracts. The default returns no expiry (<see langword="null"/>) so
/// the default graph records no expiry metadata until the live policy binding is wired. The orchestration wraps the read
/// fail-closed (returning no expiry on any failure) so a reader fault never blocks an otherwise-valid proposal (AD-12).
/// </remarks>
public interface IProposalExpiryPolicyReader
{
    /// <summary>Reads the optional proposal expiry metadata for the tenant/Agent, fail-closed to no expiry (AC1).</summary>
    /// <param name="tenantId">The interaction's tenant scope (cross-tenant reads fail closed).</param>
    /// <param name="agentId">The Agent whose configured expiry policy is read.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The optional expiry metadata, or no-expiry when unconfigured/unavailable.</returns>
    Task<ProposalExpiryPolicyResult> ReadAsync(string tenantId, string agentId, CancellationToken ct);
}

/// <summary>
/// Server-internal result of reading the optional proposal expiry metadata (Story 3.1; AC1). It carries only a safe
/// optional ISO-8601 expiry timestamp — never any generated content, provider/Conversations detail, or secret (AD-14).
/// </summary>
/// <param name="ExpiresAt">The optional ISO-8601 expiry timestamp (<see langword="null"/> when no expiry policy is configured).</param>
public sealed record ProposalExpiryPolicyResult(string? ExpiresAt)
{
    /// <summary>Gets the no-expiry result (the deferred default) — no expiry metadata is recorded.</summary>
    public static ProposalExpiryPolicyResult None { get; } = new((string?)null);
}
