using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.AgentInteraction;

using Hexalith.Conversations.Client;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Live <see cref="IConversationContextReader"/> over the Conversations public seam
/// <c>IConversationClient.GetConversationAsync(GetConversationQuery)</c> (Story 2.3; AC1, AC2, AC3; AD-6, AD-12,
/// AD-14). It maps the Agents opaque tenant/conversation strings into the Conversations value types, reads the
/// authorized tenant-scoped timeline, and maps the result to a fail-closed <see cref="ConversationContextReadResult"/>:
/// <c>Hidden</c>/<c>Redacted</c> → <see cref="AgentInteractionContextLoadOutcome.Unauthorized"/>,
/// <c>Unavailable</c>/<c>Rebuilding</c> → <see cref="AgentInteractionContextLoadOutcome.Unavailable"/>, not-trust-bearing
/// freshness → <see cref="AgentInteractionContextLoadOutcome.Stale"/>, visible + trust-bearing →
/// <see cref="AgentInteractionContextLoadOutcome.Loaded"/> with the ordered visible messages.
/// </summary>
/// <remarks>
/// Registered only behind a <c>Conversations</c> config section (the default graph uses
/// <see cref="DeferredConversationContextReader"/> and fails closed). The whole call is wrapped in fail-closed exception
/// handling so a transport/reader fault returns <see cref="ConversationContextReadResult.Unavailable"/> and NEVER
/// propagates the raw exception or its message (AD-14). The loaded message text is sensitive and is carried only on the
/// transient <see cref="ConversationContextMessage"/> for in-memory token measurement — never persisted or logged here.
/// </remarks>
public sealed class ConversationClientContextReader : IConversationContextReader
{
    // The authorized read requires a non-empty correlation id; a stable safe constant is used since the port carries no
    // per-call trace id (no PII, no content — AD-14). The live binding can thread a real correlation id when finalized.
    private const string ContextReadCorrelationId = "agents-conversation-context-read";

    private readonly IConversationClient _client;

    /// <summary>Initializes a new instance of the <see cref="ConversationClientContextReader"/> class.</summary>
    /// <param name="client">The Conversations public typed client.</param>
    public ConversationClientContextReader(IConversationClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public async Task<ConversationContextReadResult> ReadAsync(string tenantId, string sourceConversationId, string callerPartyId, string callerPrincipalId, CancellationToken ct)
    {
        try
        {
            var query = new GetConversationQuery(
                SchemaVersion.Current,
                new TenantId(tenantId),
                callerPrincipalId,
                ContextReadCorrelationId,
                new ConversationId(sourceConversationId));

            ConversationClientResult<ConversationDetailResult> result = await _client.GetConversationAsync(query, ct).ConfigureAwait(false);

            return result is { IsSuccess: true, Value: { } detail }
                ? Map(detail)
                : ConversationContextReadResult.Unavailable;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — never propagate the raw exception or its message (AD-14: no payload/stack-trace leak).
            return ConversationContextReadResult.Unavailable;
        }
    }

    // Maps the Conversations detail result to the safe load classification (AD-6/AD-12 vocabulary).
    private static ConversationContextReadResult Map(ConversationDetailResult detail)
    {
        if (detail.Details is not { } details)
        {
            // No details: Forbidden/Redacted is an access denial; any other no-details state (Unavailable/Rebuilding) is
            // a degraded read. Both fail closed; Unauthorized and Unavailable are coarse, so cross-tenant existence never
            // leaks (AC1).
            bool unauthorized = detail.FreshnessState == ProjectionTrustState.Forbidden
                || detail.FreshnessState == ProjectionTrustState.Redacted;
            return unauthorized
                ? new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Unauthorized, null, 0, false)
                : ConversationContextReadResult.Unavailable;
        }

        // Details present but not trust-bearing (stale/degraded freshness) → cannot be loaded fresh enough (AC3).
        if (!details.Freshness.AllowsTrustBearingDecision())
        {
            return new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Stale, null, 0, false);
        }

        // Authorized + fresh → loaded; project the ordered visible timeline text for in-memory measurement only (AD-14).
        List<ConversationContextMessage> messages = details.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ConversationContextMessage(m.Text, m.CreatedAt))
            .ToList();

        return new ConversationContextReadResult(AgentInteractionContextLoadOutcome.Loaded, messages, messages.Count, true);
    }
}
