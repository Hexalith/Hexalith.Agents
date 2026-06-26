using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal result of reading an Agent's effective Content Safety Policy for generation (Story 2.4; AC2; AD-12,
/// AD-14). It carries the fail-closed <see cref="IsAvailable"/> flag, the resolved effective <see cref="Policy"/> body
/// (present only on an available read), and its <see cref="PolicyVersion"/>.
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type. <see cref="Policy"/> is <see langword="null"/> for any
/// not-available read, and the orchestration treats a not-available read as <c>PolicyFailure</c> (fail closed) rather
/// than skipping the safety gate. The policy body carries only safe governance descriptors — never a secret (AD-14).
/// </remarks>
/// <param name="IsAvailable">Whether the effective policy was resolved.</param>
/// <param name="Policy">The resolved effective Content Safety Policy (non-null only when available), or <see langword="null"/>.</param>
/// <param name="PolicyVersion">The effective policy version (<c>0</c> when not available).</param>
public sealed record AgentContentSafetyPolicyReadResult(
    bool IsAvailable,
    AgentContentSafetyPolicy? Policy,
    int PolicyVersion)
{
    /// <summary>Gets the fail-closed not-available result (the deferred default) — no policy resolved.</summary>
    public static AgentContentSafetyPolicyReadResult NotAvailable { get; } = new(false, null, 0);
}
