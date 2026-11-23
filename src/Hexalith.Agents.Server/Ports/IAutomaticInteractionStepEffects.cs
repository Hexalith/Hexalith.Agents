using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Deterministic authorize/effect/result seams; effects use immutable ids and exact lookup on lost acknowledgement.</summary>
public interface IAutomaticInteractionStepEffects
{
    /// <summary>Executes or looks up only the named phase under its durable authorization; no reservation authorizes I/O.</summary>
    /// <remarks>A capacity lookup returns <c>CancelNoInvocation</c> with the allocator's exact admission ID and
    /// no-invocation record reference. The lookup verifies absence of invocation. Release follows the recorded disposition.</remarks>
    Task<AutomaticInteractionEffectResult> ExecuteAsync(AutomaticInteractionStep step, AgentInteractionState interaction, ConversationEffectLease? committedLease, CancellationToken ct);
    /// <summary>Reconciles/aborts the exact rate, open, Budget and capacity preparations from recorded dispositions, idempotently.</summary>
    Task SettleAsync(AgentInteractionState interaction, CancellationToken ct);

    /// <summary>Reconciles an older cancellation with fresh evidence for its exact allocator admission.</summary>
    Task SettleAsync(AgentInteractionState interaction, AutomaticInteractionEffectResult noInvocationEvidence, CancellationToken ct)
        => throw new NotSupportedException("HistoricalCapacitySettlementUnavailable");
}
