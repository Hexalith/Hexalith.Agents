using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Deterministic authorize/effect/result seams; effects use immutable ids and exact lookup on lost acknowledgement.</summary>
public interface IAutomaticInteractionStepEffects
{
    /// <summary>Executes or looks up only the named phase under its durable authorization; no reservation authorizes I/O.</summary>
    Task<AutomaticInteractionEffectResult> ExecuteAsync(AutomaticInteractionStep step, AgentInteractionState interaction, ConversationEffectLease? committedLease, CancellationToken ct);
    /// <summary>Reconciles/aborts the exact rate, open, Budget and capacity preparations from recorded dispositions, idempotently.</summary>
    Task SettleAsync(AgentInteractionState interaction, CancellationToken ct);
}
