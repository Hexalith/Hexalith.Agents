using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Closed deterministic seams corresponding to the existing deferred adapters; no local rate or output-status policy.</summary>
public sealed class DeferredAutomaticInteractionStepEffects : IAutomaticInteractionStepEffects
{
    /// <inheritdoc />
    public Task<AutomaticInteractionEffectResult> ExecuteAsync(AutomaticInteractionStep step, AgentInteractionState interaction, ConversationEffectLease? committedLease, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string record = step switch
        {
            AutomaticInteractionStep.RateAdmissionAuthorized or AutomaticInteractionStep.RateAdmissionRecorded or AutomaticInteractionStep.OpenInteractionLeaseRecorded => "OD-RATE-CONCURRENCY-CONSUMPTION-1",
            AutomaticInteractionStep.ApproverResolutionRecorded => "EXT-PARTIES-1",
            AutomaticInteractionStep.ContextReadRecorded => "EXT-TOKEN-1",
            AutomaticInteractionStep.PromptSafetyRecorded => "EXT-SAFETY-1",
            AutomaticInteractionStep.CapacityAdmissionRecorded => "EXT-TOPOLOGY-1",
            AutomaticInteractionStep.ProviderInvocationAuthorized => "OD-INITIAL-OUTPUT-SAFETY-STATUS-1",
            AutomaticInteractionStep.GeneratedVersionRecorded => "EXT-PROVIDER-1",
            AutomaticInteractionStep.ConversationMembershipRecorded or AutomaticInteractionStep.ConversationPostingRecorded => "EXT-CONV-AI-1",
            _ => "EXT-HOST-1",
        };
        return Task.FromResult(new AutomaticInteractionEffectResult(AutomaticInteractionOutcome.DependencyNotAvailable, record, FailureKind: step == AutomaticInteractionStep.GeneratedVersionRecorded ? AutomaticInteractionFailureKind.ProviderUnavailable : AutomaticInteractionFailureKind.Unknown));
    }
    /// <inheritdoc />
    public Task SettleAsync(AgentInteractionState interaction, CancellationToken ct) => throw new NotSupportedException("DependencyNotAvailable(EXT-HOST-1)");
}
