using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Shared fixtures for the <c>AgentInteraction</c> aggregate and state-replay tests: a command-envelope builder
/// scoped to the <c>agent-interaction</c> domain, a valid request command with a populated AD-4 snapshot, a
/// success-event builder, and helpers to pre-build and advance interaction state through the production
/// <c>Apply</c> handlers (mirroring <see cref="AgentTestData"/>).
/// </summary>
internal static class AgentInteractionTestData
{
    internal const string InteractionId = "interaction-001";
    internal const string TenantId = "acme";
    internal const string AgentId = "hexa";
    internal const string CallerPartyId = "party-001";
    internal const string SourceConversationId = "conversation-001";
    internal const string Prompt = "Summarize the latest decisions in this thread, please.";
    internal const string IdempotencyKey = "idem-001";
    internal const string ClientCorrelationId = "client-corr-001";

    /// <summary>A valid sample AD-4 configuration snapshot (Automatic mode, openai/gpt-4o, V1 default context policy).</summary>
    internal static AgentInteractionSnapshot SampleSnapshot { get; } = new(
        ConfigurationVersion: 3,
        InstructionsVersion: 2,
        ResponseMode: AgentResponseMode.Automatic,
        ApproverPolicyVersion: 1,
        ProviderId: "openai",
        ModelId: "gpt-4o",
        ProviderCapabilityVersion: 1,
        ContentSafetyPolicyVersion: 1,
        AgentInteractionSnapshot.DefaultContextPolicyReference);

    internal static CommandEnvelope Envelope<T>(
        T command,
        string interactionId = InteractionId,
        string tenantId = TenantId,
        string actorUserId = "caller-user")
        where T : notnull
        => new(
            "msg-" + typeof(T).Name,
            tenantId,
            "agent-interaction",
            interactionId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-1",
            null,
            actorUserId,
            null);

    /// <summary>A valid server-assembled request command carrying the sample snapshot.</summary>
    /// <param name="agentId">The target Agent id.</param>
    /// <param name="sourceConversationId">The source Conversation reference.</param>
    /// <param name="callerPartyId">The caller Party reference.</param>
    /// <param name="prompt">The caller prompt.</param>
    /// <param name="idempotencyKey">The caller idempotency metadata.</param>
    /// <returns>The request command.</returns>
    internal static RequestAgentInteraction ValidRequest(
        string agentId = AgentId,
        string sourceConversationId = SourceConversationId,
        string callerPartyId = CallerPartyId,
        string prompt = Prompt,
        string idempotencyKey = IdempotencyKey)
        => new(agentId, sourceConversationId, callerPartyId, prompt, idempotencyKey, SampleSnapshot, ClientCorrelationId);

    /// <summary>Builds the success event for a request command (the same shape the aggregate emits).</summary>
    /// <param name="request">The request whose recorded event is built.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The success event.</returns>
    internal static InteractionRequested RequestedEvent(RequestAgentInteraction request, string interactionId = InteractionId)
        => new(
            interactionId,
            request.AgentId,
            request.CallerPartyId,
            request.SourceConversationId,
            request.Snapshot!,
            request.Prompt,
            request.IdempotencyKey);

    /// <summary>Builds interaction state by applying the success event for the given request.</summary>
    /// <param name="request">The request whose recorded event seeds the state.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The rehydrated interaction state.</returns>
    internal static AgentInteractionState StateWith(RequestAgentInteraction request, string interactionId = InteractionId)
    {
        var state = new AgentInteractionState();
        state.Apply(RequestedEvent(request, interactionId));
        return state;
    }

    // ===== Story 2.2 gate fixtures =====

    /// <summary>The gate precondition: requested interaction state (status <c>Requested</c>) for the sample request.</summary>
    /// <returns>The rehydrated requested interaction state, driven through the real <c>Apply(InteractionRequested)</c>.</returns>
    internal static AgentInteractionState StateRequested() => StateWith(ValidRequest());

    /// <summary>Builds one gate verdict.</summary>
    /// <param name="check">The gate check.</param>
    /// <param name="outcome">The fail-closed outcome.</param>
    /// <returns>The verdict.</returns>
    internal static AgentInvocationGateVerdict Verdict(AgentInteractionGateCheck check, AgentInteractionGateOutcome outcome)
        => new(check, outcome);

    /// <summary>All nine gate checks satisfied (the authorized path), one verdict per check in evaluation order.</summary>
    /// <returns>The all-satisfied verdict list.</returns>
    internal static IReadOnlyList<AgentInvocationGateVerdict> AllSatisfied() =>
    [
        Verdict(AgentInteractionGateCheck.TenantAccess, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.CallerPartyState, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.SourceConversationAccess, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.AgentLifecycle, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.AgentPartyIdentity, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.ProviderModelReadiness, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.ResponsePolicy, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.ContentSafetyPolicy, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.DependencyFreshness, AgentInteractionGateOutcome.Satisfied),
    ];

    /// <summary>The all-satisfied set with one check overridden to a (blocking) outcome.</summary>
    /// <param name="check">The check to override.</param>
    /// <param name="outcome">The overriding outcome.</param>
    /// <returns>The verdict list with exactly one overridden check.</returns>
    internal static IReadOnlyList<AgentInvocationGateVerdict> SatisfiedExcept(AgentInteractionGateCheck check, AgentInteractionGateOutcome outcome)
        => AllSatisfied().Select(v => v.Check == check ? v with { Outcome = outcome } : v).ToList();

    /// <summary>The gate command carrying the given verdicts for the sample interaction.</summary>
    /// <param name="verdicts">The server-assembled verdicts.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The gate command.</returns>
    internal static EvaluateAgentInteractionGate GateCommand(IReadOnlyList<AgentInvocationGateVerdict> verdicts, string interactionId = InteractionId)
        => new(interactionId, verdicts);

    // ===== Story 2.3 context fixtures =====

    /// <summary>The sample model context-window token limit (matches the OpenAI gpt-4o catalog entry used in the gate tests).</summary>
    internal const int ContextWindowTokenLimit = 128_000;

    /// <summary>The sample reserved-output token count (the catalog entry's max-output limit; AC2).</summary>
    internal const int ReservedOutputTokenCount = 16_000;

    /// <summary>The sample provider capability version backing the budget.</summary>
    internal const int ProviderCapabilityVersion = 1;

    /// <summary>The context precondition: an interaction whose gate passed (status <c>Authorized</c>) so context building may run.</summary>
    /// <returns>The rehydrated authorized interaction state, driven through the real <c>Apply</c> handlers.</returns>
    internal static AgentInteractionState StateAuthorized()
    {
        AgentInteractionState state = StateRequested();
        state.Apply(new AgentInteractionAuthorized(InteractionId));
        return state;
    }

    /// <summary>Builds a server-assembled context measurement with sane budget defaults.</summary>
    /// <param name="loadOutcome">The Conversations load classification.</param>
    /// <param name="fullContextTokenCount">The measured full-context token count.</param>
    /// <param name="messageCount">The visible message count.</param>
    /// <param name="contextWindowTokenLimit">The model context-window token limit.</param>
    /// <param name="reservedOutputTokenCount">The reserved output tokens.</param>
    /// <param name="providerCapabilityVersion">The provider capability version.</param>
    /// <param name="approvedBoundedBehavior">The approved bounded behavior, if any.</param>
    /// <returns>The context measurement.</returns>
    internal static AgentInteractionContextMeasurement Measurement(
        AgentInteractionContextLoadOutcome loadOutcome = AgentInteractionContextLoadOutcome.Loaded,
        int fullContextTokenCount = 1_000,
        int messageCount = 3,
        int contextWindowTokenLimit = ContextWindowTokenLimit,
        int reservedOutputTokenCount = ReservedOutputTokenCount,
        int providerCapabilityVersion = ProviderCapabilityVersion,
        AgentInteractionBoundedContextBehavior? approvedBoundedBehavior = null)
        => new(
            loadOutcome,
            fullContextTokenCount,
            messageCount,
            contextWindowTokenLimit,
            reservedOutputTokenCount,
            providerCapabilityVersion,
            AgentInteractionSnapshot.DefaultContextPolicyReference,
            approvedBoundedBehavior);

    /// <summary>A loaded measurement whose full context fits the budget (available = 112000) → ContextReady(Full).</summary>
    /// <returns>The fitting measurement.</returns>
    internal static AgentInteractionContextMeasurement FullFitsMeasurement() => Measurement(fullContextTokenCount: 1_000);

    /// <summary>A loaded measurement whose full context exceeds the budget with no approved bounded behavior → ContextBlocked(ExceedsModelBudget).</summary>
    /// <returns>The oversized measurement.</returns>
    internal static AgentInteractionContextMeasurement OversizedMeasurement() => Measurement(fullContextTokenCount: 200_000);

    /// <summary>A loaded, oversized measurement with an approved bounded behavior that fits → ContextReady(Bounded).</summary>
    /// <param name="boundedLimit">The approved bounded-context token limit.</param>
    /// <returns>The bounded-approved measurement.</returns>
    internal static AgentInteractionContextMeasurement BoundedApprovedMeasurement(int boundedLimit = 50_000)
        => Measurement(
            fullContextTokenCount: 200_000,
            approvedBoundedBehavior: new AgentInteractionBoundedContextBehavior("bounded-conversation-test-v1", boundedLimit));

    /// <summary>The context command carrying the given measurement for the sample interaction.</summary>
    /// <param name="measurement">The server-assembled measurement.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The context command.</returns>
    internal static BuildAgentInteractionContext ContextCommand(AgentInteractionContextMeasurement measurement, string interactionId = InteractionId)
        => new(interactionId, measurement);

    /// <summary>
    /// Applies every event of a <see cref="DomainResult"/> to the supplied state through the aggregate's typed
    /// <c>Apply</c> methods — the same production replay handlers the EventStore state-store invokes. The success
    /// event advances state; rejection events are replay-safe no-ops (they must not mutate state).
    /// </summary>
    /// <param name="state">The interaction state to advance in place.</param>
    /// <param name="result">The domain result whose events are applied in order.</param>
    internal static void ApplyAll(AgentInteractionState state, DomainResult result)
    {
        foreach (IEventPayload payload in result.Events)
        {
            switch (payload)
            {
                case InteractionRequested e: state.Apply(e); break;
                case AgentInteractionAuthorized e: state.Apply(e); break;
                case AgentInteractionGateFailed e: state.Apply(e); break;
                case InvalidAgentInteractionRequestRejection e: state.Apply(e); break;
                case AgentInteractionAlreadyRequestedRejection e: state.Apply(e); break;
                case AgentInteractionGateNotEvaluableRejection e: state.Apply(e); break;
                case AgentInteractionContextReady e: state.Apply(e); break;
                case AgentInteractionContextBlocked e: state.Apply(e); break;
                case AgentInteractionContextNotBuildableRejection e: state.Apply(e); break;
                default: throw new InvalidOperationException($"Unhandled event type '{payload.GetType().Name}' in test apply dispatch.");
            }
        }
    }

    /// <summary>
    /// Drives one command end-to-end through the real aggregate pipeline — JSON-serialized command envelope →
    /// reflection dispatch in <c>AgentInteractionAggregate.ProcessAsync</c> → typed handler → events — then applies
    /// the resulting events to <paramref name="state"/> so the next command sees the evolved state.
    /// </summary>
    /// <typeparam name="TCommand">The command type (drives the dispatch lookup and payload round-trip).</typeparam>
    /// <param name="aggregate">The aggregate under test.</param>
    /// <param name="state">The threaded interaction state, advanced in place.</param>
    /// <param name="command">The command to process.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The domain result of processing the command.</returns>
    internal static async Task<DomainResult> ProcessAndApplyAsync<TCommand>(
        AgentInteractionAggregate aggregate,
        AgentInteractionState state,
        TCommand command,
        string interactionId = InteractionId)
        where TCommand : notnull
    {
        DomainResult result = await aggregate.ProcessAsync(Envelope(command, interactionId), state);
        ApplyAll(state, result);
        return result;
    }
}
