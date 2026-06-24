using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal result exposing a governed Agent's CURRENT readiness for the invocation gate (Story 2.2; AC1;
/// AD-3, AD-12). Unlike Story 2.1's <c>AgentConfigurationSnapshot</c> — which freezes config at request time — this
/// reads live readiness at gate time (AD-12 readiness is "current dependency availability"). It carries ONLY safe
/// references and enums needed for the readiness-class checks — never a secret, provider SDK type, prompt, or
/// Conversation content (AD-9, AD-14).
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type (mirroring <c>AgentConfigurationSnapshot</c>). A
/// <see cref="NotAvailable"/> result models an unreadable/degraded Agent read: the orchestration maps every dependent
/// readiness check to a fail-closed <c>Unavailable</c> verdict (AD-12). <see cref="PartyId"/>/<see cref="ProviderId"/>/
/// <see cref="ModelId"/> are opaque references — never PII or secrets.
/// </remarks>
/// <param name="IsAvailable">Whether the Agent readiness could be read at all (a degraded/unreadable read is not available).</param>
/// <param name="Lifecycle">The Agent's current lifecycle status (the <see cref="AgentInteractionGateCheck.AgentLifecycle"/> input).</param>
/// <param name="HasPartyIdentity">Whether the Agent has a linked Party identity (the <see cref="AgentInteractionGateCheck.AgentPartyIdentity"/> input; AD-7).</param>
/// <param name="PartyId">The Agent's linked Party reference (opaque, not PII), or <see langword="null"/> when none.</param>
/// <param name="ResponseMode">The Agent's current Response Mode (the <see cref="AgentInteractionGateCheck.ResponsePolicy"/> input).</param>
/// <param name="HasActiveContentSafetyPolicy">Whether an active Content Safety Policy is configured (the <see cref="AgentInteractionGateCheck.ContentSafetyPolicy"/> input; FR-12).</param>
/// <param name="ProviderId">The Agent's selected provider reference (opaque, not a secret — AD-9), or <see langword="null"/> when none.</param>
/// <param name="ModelId">The Agent's selected model reference (opaque, not a secret — AD-9), or <see langword="null"/> when none.</param>
/// <param name="ApproverPolicy">The Agent's configured Approver Policy (safe references/enums only — AD-7, AD-14), used by the Confirmation-mode <see cref="AgentInteractionGateCheck.ResponsePolicy"/> resolution; <see langword="null"/> when none / not Confirmation.</param>
/// <param name="IsFresh">Whether the consulted Agent read-model is within its freshness threshold (the <see cref="AgentInteractionGateCheck.DependencyFreshness"/> input).</param>
public sealed record AgentInvocationReadiness(
    bool IsAvailable,
    AgentLifecycleStatus Lifecycle,
    bool HasPartyIdentity,
    string? PartyId,
    AgentResponseMode ResponseMode,
    bool HasActiveContentSafetyPolicy,
    string? ProviderId,
    string? ModelId,
    AgentApproverPolicy? ApproverPolicy,
    bool IsFresh)
{
    /// <summary>Gets the fail-closed not-available readiness (the deferred default) — every readiness field at its non-ready sentinel.</summary>
    public static AgentInvocationReadiness NotAvailable { get; } = new(
        IsAvailable: false,
        AgentLifecycleStatus.Unknown,
        HasPartyIdentity: false,
        PartyId: null,
        AgentResponseMode.Unknown,
        HasActiveContentSafetyPolicy: false,
        ProviderId: null,
        ModelId: null,
        ApproverPolicy: null,
        IsFresh: false);
}
