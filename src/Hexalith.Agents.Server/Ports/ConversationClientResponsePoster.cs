using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Live <see cref="IConversationResponsePoster"/> over the Conversations public client (Story 2.5; AC1, AC2, AC3; AD-6,
/// AD-7, AD-12, AD-13, AD-14). It maps the Agents opaque tenant/conversation/party strings into the Conversations value
/// types, verifies the Agent's <c>AiAgent</c> membership via <c>IConversationClient.GetConversationAsync</c> (only over a
/// trust-bearing projection — a stale/degraded read fails closed), and appends the message authored by the Agent Party via
/// <c>IConversationClient.AppendMessageAsync</c> with a deterministic <c>MessageId</c>/idempotency key.
/// </summary>
/// <remarks>
/// <para>
/// Registered only behind a <c>Conversations</c> config section (the default graph uses
/// <see cref="DeferredConversationResponsePoster"/> and fails closed). Every operation is wrapped in fail-closed exception
/// handling so a transport/reader fault returns a fail-closed outcome and NEVER propagates the raw exception or its
/// message (AD-14). The appended <c>Text</c> is sensitive and stays inside this adapter — it is never returned, persisted,
/// or logged here (AD-14).
/// </para>
/// <para>
/// This adapter verifies existing AI membership. Enabling membership creation requires the separately qualified owner binding.
/// </para>
/// </remarks>
public sealed class ConversationClientResponsePoster : IConversationResponsePoster
{
    private readonly IConversationClient _client;

    /// <summary>Initializes a new instance of the <see cref="ConversationClientResponsePoster"/> class.</summary>
    /// <param name="client">The Conversations public typed client.</param>
    public ConversationClientResponsePoster(IConversationClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public async Task<ConversationMembershipResult> EnsureAiAgentParticipantAsync(ConversationMembershipRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var query = new GetConversationQuery(
                SchemaVersion.Current,
                new TenantId(request.TenantId),
                request.ActorPrincipalId,
                request.CorrelationId,
                new ConversationId(request.SourceConversationId));

            ConversationClientResult<ConversationDetailResult> result = await _client.GetConversationAsync(query, ct).ConfigureAwait(false);

            if (result is not { IsSuccess: true, Value.Details: { } details })
            {
                // Missing / unauthorized / hidden conversation read (no details) → fail closed (coarse, so existence never leaks; AC1).
                return ConversationMembershipResult.ConversationUnavailable;
            }

            // A visible-but-not-trust-bearing (stale/degraded freshness) read must NOT authorize membership from a stale
            // participant list — membership is a trust-bearing decision (AD-7), and a revoked-but-not-yet-projected removal
            // would otherwise read as Present and post. Fail closed, mirroring ConversationClientContextReader (Task 7:
            // "stale conversation → ConversationUnavailable").
            if (details.TenantId != query.TenantId || details.ConversationId != query.ConversationId
                || result.Value.FreshnessState != details.Freshness.FreshnessState
                || !details.Freshness.AllowsTrustBearingDecision())
            {
                return ConversationMembershipResult.ConversationUnavailable;
            }

            var agentParty = new PartyId(request.AgentPartyId);
            bool present = details.Participants.Any(p =>
                p.ParticipantPartyId == agentParty && p.ParticipantType == ParticipantType.AiAgent);

            return present
                ? new ConversationMembershipResult(ConversationMembershipOutcome.Present)
                : ConversationMembershipResult.SeamUnavailable;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — never propagate the raw exception or its message (AD-14: no payload/stack-trace leak).
            return ConversationMembershipResult.ConversationUnavailable;
        }
    }

    /// <inheritdoc />
    public async Task<ConversationAppendResult> AppendAgentMessageAsync(ConversationAppendRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var authorPartyId = new PartyId(request.AuthorPartyId);
            var metadata = new ConversationCommandMetadata(
                SchemaVersion.Current,
                new TenantId(request.TenantId),
                authorPartyId,
                request.CorrelationId,
                CausationId: null,
                IdempotencyKey: request.IdempotencyKey);

            var command = new AppendMessageCommand(
                metadata,
                new ConversationId(request.SourceConversationId),
                new MessageId(request.MessageId),
                authorPartyId,
                request.Text);

            var lookup = new ConversationAgentReadQuery(metadata.TenantId, command.ConversationId, command.MessageId);
            ConversationAppendResult? observed = await ObserveMessageAsync(lookup, authorPartyId, request, ct).ConfigureAwait(false);
            if (observed is not null)
            {
                return observed;
            }

            // Only a current source observation of absence permits submission, including on replay.
            ConversationClientResult<ConversationCommandAcceptedResult> result = await _client.AppendMessageAsync(command, ct).ConfigureAwait(false);
            observed = await ObserveMessageAsync(lookup, authorPartyId, request, ct).ConfigureAwait(false);
            if (observed is not null)
            {
                return observed;
            }

            // Only an explicit, non-retryable validation refusal plus an authoritative absence is conclusive.
            return result.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity
                && result.Error is { Errors.Count: > 0 } rejection
                && rejection.Errors.All(error => error.Category == ConversationErrorCategory.Validation && !error.IsRetryable)
                    ? new ConversationAppendResult(ConversationAppendOutcome.PostRejected)
                    : ConversationAppendResult.AwaitingConfirmation;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A lost acknowledgement cannot distinguish failure from an already-persisted append.
            return ConversationAppendResult.AwaitingConfirmation;
        }
    }

    // A null return is an authoritative absence, the only result that permits an idempotent append.
    private async Task<ConversationAppendResult?> ObserveMessageAsync(ConversationAgentReadQuery query, PartyId author, ConversationAppendRequest request, CancellationToken ct)
    {
        ConversationAgentReadResult observation = await _client.GetAgentConversationAsync(query, ct).ConfigureAwait(false);
        if (observation.TenantId != query.TenantId || observation.ConversationId != query.ConversationId
            || observation.SourceContractVersion != 1 || observation.SourceRevision is not >= 0
            || observation.ObservedAt is null || string.IsNullOrWhiteSpace(observation.ObservationId)
            || !observation.AgentParticipantPresent)
        {
            return ConversationAppendResult.AwaitingConfirmation;
        }

        if (observation.Outcome == ConversationAgentsOutcome.Absent && observation.Messages is null or { Count: 0 })
        {
            return null;
        }

        return observation.Outcome == ConversationAgentsOutcome.Available
            && observation.Messages is [var message]
            && message.MessageId == query.MessageId && message.AuthorPartyId == author
            && !message.Deleted && !message.Redacted && message.EditedAt is null
            && string.Equals(message.Text, request.Text, StringComparison.Ordinal)
            && (message.Provenance is null || (message.Provenance.AiGenerated
                && string.Equals(message.Provenance.AgentCallTraceReference, request.AgentCallTraceReference, StringComparison.Ordinal)))
                ? new ConversationAppendResult(ConversationAppendOutcome.Posted)
                : ConversationAppendResult.AwaitingConfirmation;
    }
}
