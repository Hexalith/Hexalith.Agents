using System;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, replay-safe aggregate for one tenant-scoped Agent Call (<c>AgentInteraction</c>) (AD-2, AD-3) — a distinct
/// boundary from <c>Agent</c> and <c>ProviderCatalog</c>. Story 2.1 implements the request-creation step only: it
/// records the AD-4 configuration snapshot frozen at request time and the caller's prompt, deduplicates re-issued
/// calls by their deterministic identity (AD-13), and rejects structurally-invalid requests. Authorization and
/// dependency readiness (Story 2.2), Conversation context (2.3), generation (2.4), and posting (2.5) attach to this
/// same aggregate later.
/// </summary>
/// <remarks>
/// <para>
/// <b>Pure aggregate, side effects outside (AD-3).</b> The single static
/// <c>Handle(command, state, envelope) -&gt; DomainResult</c> (discovered by the EventStore client by convention)
/// emits events only. The deterministic interaction id and the Agent configuration snapshot arrive pre-assembled in
/// the command/envelope from the Server request orchestration; the aggregate performs no provider call, no
/// Conversations/Parties/Tenants read, no Dapr, no HTTP, no logging/telemetry, no <c>DateTimeOffset.UtcNow</c>, and
/// no <c>Guid.NewGuid</c>. Request time is the EventStore event-metadata timestamp, server-stamped at persist.
/// </para>
/// <para>
/// <b>No ambient triggers, no side effects (AC3).</b> Only this explicit <see cref="RequestAgentInteraction"/>
/// command creates an interaction; Conversation state changes never do. <b>Sensitive content (AD-14):</b> the raw
/// prompt lives only on the durable <see cref="InteractionRequested"/> event and <c>AgentInteractionState</c> —
/// never on a rejection, the status view/reference, logs, telemetry, or audit summaries.
/// </para>
/// </remarks>
[EventStoreDomain("agent-interaction")]
public class AgentInteractionAggregate : EventStoreAggregate<AgentInteractionState>
{
    /// <summary>Handles creation (or idempotent re-creation) of the Agent Call request record (AC1, AC2, AC4).</summary>
    /// <param name="command">The request command (server-populated Agent id + AD-4 snapshot; caller prompt/references).</param>
    /// <param name="state">The current interaction state (null/never-requested before the first request).</param>
    /// <param name="envelope">The command envelope (carries the deterministic interaction id and the tenant scope).</param>
    /// <returns>The domain result (success event, typed rejection, or a deterministic no-op).</returns>
    public static DomainResult Handle(RequestAgentInteraction command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string interactionId = envelope.AggregateId;

        // Pure structural validation: required caller/source/prompt fields + a usable server-assembled snapshot
        // (AC1, AC4). A not-available snapshot (pre-activation Agent) surfaces here as MissingAgentSnapshot — never a
        // cross-aggregate read. The classification never echoes the prompt or any caller value (AD-14).
        AgentInteractionRequestValidationStatus? invalid = AgentInteractionRequestPolicy.Validate(
            command.AgentId,
            command.CallerPartyId,
            command.SourceConversationId,
            command.Prompt,
            command.Snapshot);
        if (invalid is { } status)
        {
            return DomainResult.Rejection([new InvalidAgentInteractionRequestRejection(interactionId, status)]);
        }

        // Idempotent duplicate handling (AD-13): re-issuing the same call on the same deterministic id is a no-op;
        // a conflicting payload on that id is rejected and never silently mutates the recorded request. Record
        // value-equality is not relied upon — the request and snapshot scalars are compared explicitly (ordinal).
        if (state is { IsRequested: true })
        {
            return RequestMatchesExisting(state, command)
                ? DomainResult.NoOp()
                : DomainResult.Rejection([new AgentInteractionAlreadyRequestedRejection(interactionId)]);
        }

        // Snapshot is non-null here (validation guaranteed a usable snapshot). The prompt is recorded on the durable
        // success event only (AD-14). No wall-clock field — request time is the EventStore event metadata (AD-3).
        return DomainResult.Success([
            new InteractionRequested(
                interactionId,
                command.AgentId,
                command.CallerPartyId,
                command.SourceConversationId,
                command.Snapshot!,
                command.Prompt,
                command.IdempotencyKey),
        ]);
    }

    /// <summary>
    /// Evaluates the invocation authorization + dependency-readiness gate from server-assembled verdicts and records the
    /// terminal decision (AC1–AC4; FR-20, FR-21; AD-3, AD-12, AD-13).
    /// </summary>
    /// <param name="command">The gate command carrying the server-assembled per-check verdicts (client values discarded upstream).</param>
    /// <param name="state">The current interaction state (must be requested before the gate can run).</param>
    /// <param name="envelope">The command envelope (carries the interaction id and the tenant scope).</param>
    /// <returns>The domain result (the authorized/failed outcome event, a not-evaluable rejection, or an idempotent no-op).</returns>
    public static DomainResult Handle(EvaluateAgentInteractionGate command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string interactionId = envelope.AggregateId;

        // State precondition: the gate can only evaluate an interaction whose request was recorded. A gate command on a
        // never-requested stream is a structural rejection (no state change), not a recorded decision (AD-12). The
        // positive pattern binds the non-null requested state so the gate logic runs only over a recorded interaction.
        if (state is { IsRequested: true } requested)
        {
            // No verdicts means the gate has nothing to decide — fail closed as a structural rejection rather than
            // silently authorizing (a zero-blocker set must come from a real, populated evaluation, never an empty one).
            if (command.Verdicts is null or { Count: 0 })
            {
                return DomainResult.Rejection([
                    new AgentInteractionGateNotEvaluableRejection(interactionId, AgentInteractionGateNotEvaluableReason.NoVerdictsProvided),
                ]);
            }

            // Idempotent terminal gate (AD-13): the decision is recorded once and is terminal. A re-issued gate command
            // on an already-gated interaction is a clean no-op — the aggregate never silently flips a recorded decision.
            if (requested.Status is AgentInteractionStatus.Authorized or AgentInteractionStatus.Denied or AgentInteractionStatus.Blocked)
            {
                return DomainResult.NoOp();
            }

            // Pure evaluation (AD-3): emit the authorized/failed outcome only. The verdicts arrive pre-assembled from
            // trusted server reads (the gate orchestration); the aggregate's sole job is the blockers → decision math.
            return AgentInvocationGatePolicy.Evaluate(interactionId, command.Verdicts);
        }

        return DomainResult.Rejection([
            new AgentInteractionGateNotEvaluableRejection(interactionId, AgentInteractionGateNotEvaluableReason.InteractionNotRequested),
        ]);
    }

    /// <summary>
    /// Builds the Conversation context within safe bounds from the server-assembled measurement and records the terminal
    /// context decision (AC1–AC4; FR-9; AD-3, AD-11, AD-12, AD-13).
    /// </summary>
    /// <param name="command">The context command carrying the server-assembled measurement (client values discarded upstream).</param>
    /// <param name="state">The current interaction state (must be Authorized before context can be built).</param>
    /// <param name="envelope">The command envelope (carries the interaction id and the tenant scope).</param>
    /// <returns>The domain result (the context-ready/blocked outcome event, a not-buildable rejection, or an idempotent no-op).</returns>
    public static DomainResult Handle(BuildAgentInteractionContext command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string interactionId = envelope.AggregateId;

        // State precondition: context can only be built on a recorded interaction. A context command on a never-requested
        // stream is a structural rejection (no state change), not a recorded decision (AD-12). The positive pattern binds
        // the non-null requested state so the context logic runs only over a recorded interaction.
        if (state is { IsRequested: true } requested)
        {
            // Idempotent terminal context (AD-13): the decision is recorded once and is terminal. A re-issued context
            // command on an already-decided interaction is a clean no-op — the aggregate never silently flips a recorded
            // ContextReady/ContextBlocked decision.
            if (requested.Status is AgentInteractionStatus.ContextReady or AgentInteractionStatus.ContextBlocked)
            {
                return DomainResult.NoOp();
            }

            // Authorization precondition (AD-11): context must never be built on a call that has not cleared the gate. A
            // Requested/Denied/Blocked interaction is a structural rejection (no state change) — distinct from a recorded
            // context-blocked decision, which only ever follows an Authorized interaction.
            if (requested.Status != AgentInteractionStatus.Authorized)
            {
                return DomainResult.Rejection([
                    new AgentInteractionContextNotBuildableRejection(interactionId, AgentInteractionContextNotBuildableReason.InteractionNotAuthorized),
                ]);
            }

            // Pure evaluation (AD-3): emit the context-ready/blocked outcome only. The measurement arrives pre-assembled
            // from trusted server reads (the context orchestration); the aggregate's sole job is the budget → decision math.
            return AgentInteractionContextPolicy.Evaluate(interactionId, command.Measurement);
        }

        return DomainResult.Rejection([
            new AgentInteractionContextNotBuildableRejection(interactionId, AgentInteractionContextNotBuildableReason.InteractionNotRequested),
        ]);
    }

    /// <summary>
    /// Records the terminal generation decision from the server-assembled outcome (AC1–AC4; FR-9, FR-10, FR-12; AD-3,
    /// AD-5, AD-9, AD-13). The orchestrator performs the impure provider invocation + content-safety gate and returns the
    /// outcome through <see cref="GenerateAgentOutput.Result"/>; the aggregate's sole job is the outcome → event math.
    /// </summary>
    /// <param name="command">The generate command carrying the server-assembled generation outcome (client values discarded upstream).</param>
    /// <param name="state">The current interaction state (must be ContextReady before output can be generated).</param>
    /// <param name="envelope">The command envelope (carries the interaction id and the tenant scope).</param>
    /// <returns>The domain result (the generated/failed outcome event, a not-generatable rejection, or an idempotent no-op).</returns>
    public static DomainResult Handle(GenerateAgentOutput command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string interactionId = envelope.AggregateId;

        // State precondition: output can only be generated on a recorded interaction. A generate command on a
        // never-requested stream is a structural rejection (no state change), not a recorded decision (AD-12). The
        // positive pattern binds the non-null requested state so the generation logic runs only over a recorded interaction.
        if (state is { IsRequested: true } requested)
        {
            // Idempotent terminal generation (AD-13): the decision is recorded once and is terminal. Re-dispatching a
            // generate command after a terminal Generated/GenerationFailed/SafetyFailed outcome is a clean no-op that
            // preserves version history — the aggregate never silently flips a recorded decision or appends a duplicate
            // version (AC4).
            if (requested.Status is AgentInteractionStatus.Generated or AgentInteractionStatus.GenerationFailed or AgentInteractionStatus.SafetyFailed)
            {
                return DomainResult.NoOp();
            }

            // Context-ready precondition (AD-11): generation must never run before Conversation context is built within
            // safe bounds. Any other status (Requested/Authorized/Denied/Blocked/ContextBlocked) is a structural rejection
            // (no state change) — distinct from a recorded generation-failed decision, which only ever follows ContextReady.
            if (requested.Status != AgentInteractionStatus.ContextReady)
            {
                return DomainResult.Rejection([
                    new AgentOutputNotGeneratableRejection(interactionId, AgentOutputNotGeneratableReason.ContextNotReady),
                ]);
            }

            // Pure evaluation (AD-3): emit the generated/failed outcome only. The result arrives pre-assembled from the
            // trusted generation orchestration; the aggregate's sole job is the outcome → event/status math.
            return AgentOutputGenerationPolicy.Evaluate(interactionId, command.Result);
        }

        return DomainResult.Rejection([
            new AgentOutputNotGeneratableRejection(interactionId, AgentOutputNotGeneratableReason.InteractionNotRequested),
        ]);
    }

    // By-value comparison of a re-issued request against the recorded one (AD-13). Strings are compared ordinally;
    // the snapshot scalars are compared explicitly so a re-derived-id collision with a different configuration is
    // surfaced as a conflict rather than a silent no-op.
    private static bool RequestMatchesExisting(AgentInteractionState existing, RequestAgentInteraction command)
        => string.Equals(existing.AgentId, command.AgentId, StringComparison.Ordinal)
            && string.Equals(existing.CallerPartyId, command.CallerPartyId, StringComparison.Ordinal)
            && string.Equals(existing.SourceConversationId, command.SourceConversationId, StringComparison.Ordinal)
            && string.Equals(existing.Prompt, command.Prompt, StringComparison.Ordinal)
            && string.Equals(existing.IdempotencyKey, command.IdempotencyKey, StringComparison.Ordinal)
            && SnapshotsEqual(existing.Snapshot, command.Snapshot);

    private static bool SnapshotsEqual(AgentInteractionSnapshot? a, AgentInteractionSnapshot? b)
    {
        if (a is null || b is null)
        {
            return a is null && b is null;
        }

        return a.ConfigurationVersion == b.ConfigurationVersion
            && a.InstructionsVersion == b.InstructionsVersion
            && a.ResponseMode == b.ResponseMode
            && a.ApproverPolicyVersion == b.ApproverPolicyVersion
            && string.Equals(a.ProviderId, b.ProviderId, StringComparison.Ordinal)
            && string.Equals(a.ModelId, b.ModelId, StringComparison.Ordinal)
            && a.ProviderCapabilityVersion == b.ProviderCapabilityVersion
            && a.ContentSafetyPolicyVersion == b.ContentSafetyPolicyVersion
            && string.Equals(a.ContextPolicyReference, b.ContextPolicyReference, StringComparison.Ordinal);
    }
}
