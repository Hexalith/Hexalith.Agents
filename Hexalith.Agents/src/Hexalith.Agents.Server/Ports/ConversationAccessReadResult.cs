using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal result of reading the caller's access to the Source Conversation for the invocation gate
/// (Story 2.2; AC1, AC2, AC3; AD-6, AD-12). It carries ONLY the fail-closed <see cref="Outcome"/> and a coarse
/// <see cref="IsFresh"/> freshness flag — never Conversation content, participant PII, a stream name, or any record
/// that would reveal cross-tenant existence (AC3; AD-14).
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type (mirroring <c>AgentPartyValidationResult</c>). The
/// implementation wraps the public Conversations authorized read seam <c>IConversationClient.GetConversationAsync</c>
/// (caller participates with sufficient role AND conversation loaded fresh); <see cref="IsFresh"/> feeds the separate
/// <see cref="AgentInteractionGateCheck.DependencyFreshness"/> check. Story 2.3 wires the live read.
/// </remarks>
/// <param name="Outcome">The fail-closed conversation-access outcome (only <see cref="AgentInteractionGateOutcome.Satisfied"/> permits the call).</param>
/// <param name="IsFresh">Whether the consulted Conversation read is within its freshness threshold.</param>
public sealed record ConversationAccessReadResult(AgentInteractionGateOutcome Outcome, bool IsFresh)
{
    /// <summary>Gets the fail-closed not-available result (the deferred default) — unavailable and not fresh.</summary>
    public static ConversationAccessReadResult Unavailable { get; } = new(AgentInteractionGateOutcome.Unavailable, false);
}
