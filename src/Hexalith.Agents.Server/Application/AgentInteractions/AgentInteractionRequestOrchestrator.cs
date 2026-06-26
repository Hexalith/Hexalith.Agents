using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Application orchestration for requesting an Agent Call (<c>AgentInteraction</c>) from a Source Conversation
/// (Story 2.1; AC1, AC2, AC3, AC4; AD-3, AD-4, AD-13). It (1) derives the deterministic interaction id, (2) reads
/// the trusted AD-4 Agent configuration snapshot through <see cref="IAgentConfigurationSnapshotReader"/>, (3)
/// assembles the server-trusted <see cref="RequestAgentInteraction"/> command with that snapshot, (4) builds the
/// <see cref="CommandEnvelope"/> keyed by the interaction id under the <c>agent-interaction</c> domain, (5)
/// dispatches it through <see cref="IAgentCommandDispatcher"/>, and (6) returns a safe
/// <see cref="AgentInteractionReference"/>. The impure id derivation and snapshot read happen here, outside the pure
/// aggregate, and feed back through the command (the AD-3 round-trip).
/// </summary>
/// <remarks>
/// <b>No side effects (AC3):</b> the snapshot read touches only the in-module Agent configuration — there is NO
/// provider adapter call and NO <c>IConversationClient</c>/Parties call anywhere on this path, and no ambient
/// subscription creates an interaction from Conversation state. <b>Trust model:</b> the Agent id and snapshot on the
/// dispatched command are server-populated — any client-supplied snapshot is ignored and the reserved trust
/// extension keys are stripped from the client-supplied envelope extensions (none are repopulated; this request path
/// carries no admin/verdict). Binding <see cref="IAgentConfigurationSnapshotReader"/> to the live read-model and
/// <see cref="IAgentCommandDispatcher"/> to the live command gateway is deferred (mirroring Story 1.2/1.4/1.5).
/// </remarks>
public sealed class AgentInteractionRequestOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    // Reserved server-populated trust keys used elsewhere in the module. This request path repopulates none of them;
    // they are stripped from client-supplied extensions so a client cannot smuggle a forged admin/verdict onto the
    // interaction stream. Benign (non-reserved) client extensions pass through.
    private static readonly string[] _reservedExtensionKeys =
    [
        "actor:agentsAdmin",
        "provider:selectionValidation",
        "approver:policyValidation",
        "party:linkValidation",
    ];

    private readonly IAgentConfigurationSnapshotReader _snapshotReader;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionRequestOrchestrator"/> class.</summary>
    /// <param name="snapshotReader">The Agent configuration snapshot read port (live binding deferred).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentInteractionRequestOrchestrator(IAgentConfigurationSnapshotReader snapshotReader, IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(snapshotReader);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _snapshotReader = snapshotReader;
        _dispatcher = dispatcher;
    }

    /// <summary>Derives the id, reads the snapshot, assembles + dispatches the request, and returns a safe reference (AC1, AC2).</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (safe reference + snapshot-availability/dispatch flags).</returns>
    public async Task<AgentInteractionRequestOutcome> ExecuteAsync(AgentInteractionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // (1) Derive the deterministic interaction id (AD-13) — re-issuing the same call yields the same id.
        string interactionId = AgentInteractionIdentity.Derive(
            request.TenantId,
            request.AgentId,
            request.SourceConversationId,
            request.CallerPartyId,
            request.IdempotencyKey);

        // (2) Read the trusted AD-4 Agent configuration snapshot (no provider/Conversations/Parties call — AD-3, AC3).
        AgentConfigurationSnapshot read = await _snapshotReader
            .ReadAsync(request.TenantId, request.AgentId, ct)
            .ConfigureAwait(false);

        // (3) Assemble the server-trusted snapshot. Any client-supplied snapshot is ignored; the V1 context-policy
        // reference is stamped from the single shared default so the orchestrator and tests cannot drift (Story 2.3
        // replaces it). A not-available read yields a null snapshot → the aggregate fails closed with MissingAgentSnapshot.
        AgentInteractionSnapshot? snapshot = read is { IsAvailable: true, Snapshot: { } populated }
            ? populated with { ContextPolicyReference = AgentInteractionSnapshot.DefaultContextPolicyReference }
            : null;

        // (4) Build the server-trusted command — Agent id + snapshot are server-populated; caller references/prompt/
        // idempotency come from the sanitized request.
        var command = new RequestAgentInteraction(
            request.AgentId,
            request.SourceConversationId,
            request.CallerPartyId,
            request.Prompt,
            request.IdempotencyKey,
            snapshot,
            request.ClientCorrelationId);

        // (5) Build the envelope (AggregateId = deterministic interaction id, Domain = agent-interaction). Reserved
        // trust extensions are stripped; none are repopulated on this request path.
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            interactionId,
            nameof(RequestAgentInteraction),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        // (6) Dispatch (live binding deferred). Even a not-available snapshot is dispatched so the aggregate records
        // an auditable MissingAgentSnapshot rejection (mirroring the provider-selection always-dispatch pattern).
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);

        // (7) Return the safe status reference — never a stream name or provider detail (AC2).
        bool available = snapshot is not null;
        var reference = new AgentInteractionReference(
            interactionId,
            available ? AgentInteractionStatus.Requested : AgentInteractionStatus.Unknown);
        return new AgentInteractionRequestOutcome(reference, available, Dispatched: true);
    }

    // Copies the client-supplied extensions with the reserved trust keys removed; repopulates none (the request path
    // carries no admin/verdict). Returns null when nothing benign remains so the envelope carries no empty map.
    private static Dictionary<string, string>? BuildTrustedExtensions(IReadOnlyDictionary<string, string>? clientSupplied)
    {
        if (clientSupplied is null)
        {
            return null;
        }

        var extensions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string key, string value) in clientSupplied)
        {
            if (Array.IndexOf(_reservedExtensionKeys, key) < 0)
            {
                extensions[key] = value;
            }
        }

        return extensions.Count > 0 ? extensions : null;
    }
}
