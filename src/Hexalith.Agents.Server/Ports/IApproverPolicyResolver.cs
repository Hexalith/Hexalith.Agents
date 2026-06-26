using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the activation re-validation orchestration uses to resolve each configured approver source
/// of a Confirmation-mode Agent's Approver Policy against its dependency (Story 1.6; AD-3, AD-8, AD-12). A
/// <c>PredefinedParty</c> source resolves through the existing <see cref="IAgentPartyDirectory"/> (reuse — no second
/// Parties port), a <c>TenantRole</c> source through the local Tenants projection, and a <c>ConversationOwner</c>
/// source through the V1 facilitator-resolver availability check. The implementation returns ONLY safe per-source
/// outcomes — never Party/Tenant/Conversation PII, secrets, or provider SDK types (AD-7, AD-9, AD-14).
/// </summary>
/// <remarks>
/// Keeping this a port (rather than resolving inline) preserves the AD-3 round-trip: the pure aggregate receives the
/// aggregated <see cref="ApproverPolicyValidationStatus"/> verdict through a trusted command extension, never by
/// reading a projection itself. The live Tenants-projection / Conversations-facilitator / Parties bindings are
/// deferred (mirroring Story 1.2/1.4/1.5) so the verdict logic stays fully unit-testable here.
/// </remarks>
public interface IApproverPolicyResolver
{
    /// <summary>Resolves every configured approver source against its dependency and returns safe per-source outcomes (AC3).</summary>
    /// <param name="tenantId">The Agent's tenant scope (cross-tenant sources surface as <c>Unauthorized</c>).</param>
    /// <param name="policy">The configured Approver Policy (safe references only).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe per-source resolution result (no PII, no secrets).</returns>
    Task<ApproverPolicyResolutionResult> ResolveAsync(string tenantId, AgentApproverPolicy policy, CancellationToken ct);
}
