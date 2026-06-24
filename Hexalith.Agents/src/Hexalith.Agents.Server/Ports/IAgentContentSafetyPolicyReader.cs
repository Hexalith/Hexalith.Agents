using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the generation orchestration uses to read an Agent's <em>effective</em> Content Safety Policy
/// body for the interaction's response mode (Story 2.4; AC2; FR-26; AD-3). The <c>AgentInteractionSnapshot</c> only
/// carries <c>ContentSafetyPolicyVersion</c>; the evaluator needs the policy body (categories + failure handling + audit
/// treatment) which lives on <c>AgentState.ContentSafety</c>. The live binding to the Agent read-model is deferred so the
/// orchestration's decision logic stays fully unit-testable; the default DI graph binds the fail-closed
/// <see cref="DeferredAgentContentSafetyPolicyReader"/>.
/// </summary>
/// <remarks>
/// The reader resolves the effective policy = the mode-specific override (<c>AutomaticModePolicy</c>/
/// <c>ConfirmationModePolicy</c> per <paramref name="mode"/>) if present, else <c>ActivePolicy</c> (FR-26). On any
/// failure / not-found it returns <see cref="AgentContentSafetyPolicyReadResult.NotAvailable"/> so generation fails
/// closed to <c>PolicyFailure</c> rather than skipping the safety gate (AD-12). It returns the safe policy body only —
/// never a secret (AD-14).
/// </remarks>
public interface IAgentContentSafetyPolicyReader
{
    /// <summary>Reads the effective Content Safety Policy for the Agent and response mode, fail-closed (AC2).</summary>
    /// <param name="tenantId">The Agent's tenant scope (cross-tenant reads fail closed).</param>
    /// <param name="agentId">The target Agent identifier whose content-safety configuration is read.</param>
    /// <param name="mode">The interaction's response mode (selects the mode-specific override, if any).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The effective policy + its version, or a fail-closed not-available result.</returns>
    Task<AgentContentSafetyPolicyReadResult> ReadAsync(string tenantId, string agentId, AgentResponseMode mode, CancellationToken ct);
}
