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
