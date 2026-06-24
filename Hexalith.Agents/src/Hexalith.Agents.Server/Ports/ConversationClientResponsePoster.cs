using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Commands;
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
/// <b>Membership establish has no public seam.</b> <c>IConversationClient</c> does not expose
/// <c>AddParticipantAsync</c> and no API route maps <c>AddParticipantCommand</c> (verified — Conversations commit
/// <c>46df0cd</c>). Therefore, when the Agent is not already a participant, this returns
/// <see cref="ConversationMembershipOutcome.SeamUnavailable"/> (fail closed). When Conversations exposes a public
/// <c>AddParticipantAsync(AddParticipantCommand,…)</c> (AD-6 / proposed Story 2.0a), wire the establish path at the marked
/// extension point below to return <see cref="ConversationMembershipOutcome.Established"/> — no Agents contract change is
/// required.
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
            if (!details.Freshness.AllowsTrustBearingDecision())
            {
                return ConversationMembershipResult.ConversationUnavailable;
            }

            var agentParty = new PartyId(request.AgentPartyId);
            bool present = details.Participants.Any(p =>
                p.ParticipantPartyId == agentParty && p.ParticipantType == ParticipantType.AiAgent);

            // Present → proceed. Absent → there is no public membership-establish seam today, so fail closed (AD-6, AD-7).
            // Extension point (AD-6 / Story 2.0a): when IConversationClient exposes AddParticipantAsync, call it here for
            // the absent case and return ConversationMembershipResult with ConversationMembershipOutcome.Established.
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

            ConversationClientResult<ConversationCommandAcceptedResult> result = await _client.AppendMessageAsync(command, ct).ConfigureAwait(false);

            // Conversations dedupes on the caller-supplied MessageId/IdempotencyKey, so a retry is a safe no-op (AD-13).
            return result.IsSuccess
                ? new ConversationAppendResult(ConversationAppendOutcome.Posted)
                : new ConversationAppendResult(ConversationAppendOutcome.PostRejected);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — the Conversations error text never crosses this boundary (AD-14).
            return ConversationAppendResult.AdapterFailure;
        }
    }
}
