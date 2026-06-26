using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the posting orchestration uses to read an Agent's linked Party identity + posting-time validity
/// (Story 2.5; AC1; AD-7, AD-3). The <c>AgentInteractionSnapshot</c> only carries provider/model/versions/response mode —
/// NOT the Agent's <c>PartyId</c> — and AD-7 requires a posting-time Party-validity gate (the linked identity may have
/// changed/been disabled since request time), so this reads the linked id + valid/enabled/available state live from the
/// Agent read-model (<c>AgentState.PartyId</c>), never trusting a stale snapshot. The live binding is deferred so the
/// orchestration's decision logic stays fully unit-testable; the default DI graph binds the fail-closed
/// <see cref="DeferredAgentPartyReader"/>.
/// </summary>
/// <remarks>
/// On any failure / not-linked / disabled / not-found it returns a not-available outcome so posting fails closed to
/// <c>PartyIdentityUnavailable</c> rather than posting with an unverified author identity (AD-12). It returns the safe
/// stable Party reference only — never Party PII (AD-7).
/// </remarks>
public interface IAgentPartyReader
{
    /// <summary>Reads the Agent's linked Party identity + posting-time validity, fail-closed (AC1).</summary>
    /// <param name="tenantId">The Agent's tenant scope (cross-tenant reads fail closed).</param>
    /// <param name="agentId">The target Agent identifier whose linked Party identity is read.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The Agent's linked Party reference + validity, or a fail-closed not-available result.</returns>
    Task<AgentPartyReadResult> ReadAsync(string tenantId, string agentId, CancellationToken ct);
}
