using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>One pure mapping for durable phase events and the safe orchestration result.</summary>
internal static class AutomaticInteractionPolicy
{
    /// <summary>Maps one accepted phase and its atomic Provider dispositions.</summary>
    internal static IReadOnlyList<IEventPayload> Evaluate(AutomaticInteractionFact fact) => Compute(fact).Events;
    /// <summary>Reports the same accepted phase outcome.</summary>
    internal static AutomaticInteractionOutcome Decide(AutomaticInteractionFact fact) => Compute(fact).Outcome;
    private static (IReadOnlyList<IEventPayload> Events, AutomaticInteractionOutcome Outcome) Compute(AutomaticInteractionFact fact)
    {
        IEventPayload item = fact.Step switch
        {
            AutomaticInteractionStep.WorkflowStartCheckpoint => new WorkflowStartCheckpoint(fact),
            AutomaticInteractionStep.RateAdmissionAuthorized => new RateAdmissionAuthorized(fact),
            AutomaticInteractionStep.OpenInteractionLeaseAuthorized => new OpenInteractionLeaseAuthorized(fact),
            AutomaticInteractionStep.RateAdmissionRecorded => new RateAdmissionRecorded(fact),
            AutomaticInteractionStep.OpenInteractionLeaseRecorded => new OpenInteractionLeaseRecorded(fact),
            AutomaticInteractionStep.ApproverResolutionRecorded => new ApproverResolutionRecorded(fact),
            AutomaticInteractionStep.ContextReadRecorded => new ContextReadRecorded(fact),
            AutomaticInteractionStep.BudgetReservationAuthorized => new BudgetReservationAuthorized(fact),
            AutomaticInteractionStep.BudgetReservationRecorded => new BudgetReservationRecorded(fact),
            AutomaticInteractionStep.PromptSafetyRecorded => new PromptSafetyRecorded(fact),
            AutomaticInteractionStep.ConversationMembershipRecorded => new ConversationMembershipRecorded(fact),
            AutomaticInteractionStep.AgentCallAccepted => new AgentCallAccepted(fact),
            AutomaticInteractionStep.CapacityAdmissionAuthorized => new CapacityAdmissionAuthorized(fact),
            AutomaticInteractionStep.CapacityAdmissionRecorded => new CapacityAdmissionRecorded(fact),
            AutomaticInteractionStep.ProviderInvocationAuthorized => new ProviderInvocationAuthorized(fact),
            AutomaticInteractionStep.ProviderInvocationNotAuthorized => new ProviderInvocationNotAuthorized(fact),
            AutomaticInteractionStep.BudgetDispositionDecided => new BudgetDispositionDecided(fact),
            AutomaticInteractionStep.PreProviderCapacityDispositionDecided => new PreProviderCapacityDispositionDecided(fact),
            AutomaticInteractionStep.GenerationFailed => new GenerationFailed(fact),
            AutomaticInteractionStep.GeneratedVersionRecorded => new GeneratedVersionRecorded(fact),
            AutomaticInteractionStep.ConversationPostingRecorded => new ConversationPostingRecorded(fact),
            AutomaticInteractionStep.ConversationPostingSuppressedByDeletion => new ConversationPostingSuppressedByDeletion(fact),
            AutomaticInteractionStep.StartSuppressedByDeletion => new StartSuppressedByDeletion(fact),
            AutomaticInteractionStep.InteractionPreAcceptanceFailed => new InteractionPreAcceptanceFailed(fact),
            _ => throw new ArgumentOutOfRangeException(nameof(fact)),
        };
        List<IEventPayload> events = [item];
        if (fact.Step is AutomaticInteractionStep.ProviderInvocationAuthorized or AutomaticInteractionStep.ProviderInvocationNotAuthorized)
        {
            bool invoked = fact.Step == AutomaticInteractionStep.ProviderInvocationAuthorized;
            events.Add(new BudgetDispositionDecided(fact with { Step = AutomaticInteractionStep.BudgetDispositionDecided, Outcome = invoked ? AutomaticInteractionOutcome.InvocationSettlement : AutomaticInteractionOutcome.NotInvoked, Revision = fact.Revision + 1 }));
            if (!invoked) { events.Add(new PreProviderCapacityDispositionDecided(fact with { Step = AutomaticInteractionStep.PreProviderCapacityDispositionDecided, Outcome = AutomaticInteractionOutcome.CancelNoInvocation, Revision = fact.Revision + 2 })); }
        }
        if (fact.Step is AutomaticInteractionStep.PreProviderCapacityDispositionDecided or AutomaticInteractionStep.InteractionPreAcceptanceFailed)
        {
            events.Add(new BudgetDispositionDecided(fact with { Step = AutomaticInteractionStep.BudgetDispositionDecided,
                Outcome = AutomaticInteractionOutcome.NotInvoked, Revision = fact.Revision + 1 }));
        }
        return (events, fact.Outcome);
    }
}
