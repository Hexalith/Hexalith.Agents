using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Application orchestration for the deterministic, system-policy expiry of a pending Proposed Agent Reply (Story 3.6; AC3;
/// AD-3, AD-5, AD-13, AD-18). It implements the expiry <em>decision + transition</em>: it reads the recorded
/// <c>ExpiresAt</c> (falling back fail-closed to <see cref="IProposalExpiryPolicyReader"/> for the tenant + Agent), compares
/// it to a <b>trusted evaluation "now" supplied on the request</b> (never <see cref="DateTimeOffset.UtcNow"/> in the hot
/// path, never read inside the aggregate — AD-3), and dispatches <see cref="ExpireProposedAgentReply"/> ONLY when the expiry
/// has elapsed. Expiry requires no approver authorization (system policy) but stays tenant-scoped (FR-19).
/// </summary>
/// <remarks>
/// <b>Scope (AD-18):</b> this ships the deterministic transition only. The automatic <em>firing trigger</em> (a durable
/// sweep / reminder / hosted service) and the live <see cref="IProposalExpiryPolicyReader"/> / dispatcher bindings are Epic 4
/// — the module has no scheduler, and creating its first one is out of scope. The default graph keeps the
/// <c>DeferredProposalExpiryPolicyReader</c> (fail-closed no-expiry) and a Deferred dispatcher, mirroring every prior Epic-3
/// orchestrator. Determinism (AC3) is proven by unit tests with a fixed injected timestamp. The returned status comes from
/// the shared <see cref="AgentProposalExpiryPolicy"/> so it cannot drift from the aggregate's recorded decision (AD-3).
/// </remarks>
public sealed class AgentInteractionProposalExpiryOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    private static readonly string[] _reservedExtensionKeys =
    [
        "actor:agentsAdmin",
        "provider:selectionValidation",
        "approver:policyValidation",
        "party:linkValidation",
    ];

    private readonly IProposalExpiryPolicyReader _expiryReader;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionProposalExpiryOrchestrator"/> class.</summary>
    /// <param name="expiryReader">The reused proposal-expiry policy reader (live binding deferred — fails closed to no-expiry).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentInteractionProposalExpiryOrchestrator(
        IProposalExpiryPolicyReader expiryReader,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(expiryReader);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _expiryReader = expiryReader;
        _dispatcher = dispatcher;
    }

    /// <summary>Deterministically evaluates the recorded expiry against the trusted timestamp and dispatches the expire command only when elapsed (AC3).</summary>
    /// <param name="request">The expiry orchestration request (ids + proposal sub-state + recorded ExpiresAt + the trusted evaluation timestamp).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe expiry outcome (Expired + decided status when elapsed; a deterministic no-transition outcome otherwise).</returns>
    public async Task<AgentInteractionProposalExpiryOutcomeResult> ExecuteAsync(AgentInteractionProposalExpiryRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // (a) Structural sub-state guard (AC3, AC4): only a pending proposal can expire — a terminal/non-pending proposal is
        // a deterministic no-transition with no dispatch.
        if (!IsExpirable(request.ProposalState))
        {
            return new AgentInteractionProposalExpiryOutcomeResult(AgentProposalExpiryOutcome.ProposalNotPending, AgentInteractionStatus.Unknown);
        }

        // (b) Resolve the effective recorded expiry: prefer the snapshotted ExpiresAt on the request; when absent, read it
        // fail-closed via the expiry policy reader (the deferred default returns no expiry — AD-18).
        string? expiresAt = request.ExpiresAt;
        if (string.IsNullOrWhiteSpace(expiresAt))
        {
            ProposalExpiryPolicyResult policy = await ReadExpiryAsync(request, ct).ConfigureAwait(false);
            expiresAt = policy.ExpiresAt;
        }

        // (c) No configured/parseable expiry → unbounded proposal, no transition (fail closed to no-expiry).
        if (string.IsNullOrWhiteSpace(expiresAt) || !TryParseExpiry(expiresAt, out DateTimeOffset expiry))
        {
            return new AgentInteractionProposalExpiryOutcomeResult(AgentProposalExpiryOutcome.NoExpiryPolicy, AgentInteractionStatus.Unknown);
        }

        // (d) Expiry not yet reached against the trusted "now" → the proposal stays pending, no transition (deterministic).
        if (request.EvaluationTimestamp < expiry)
        {
            return new AgentInteractionProposalExpiryOutcomeResult(AgentProposalExpiryOutcome.ExpiryNotReached, AgentInteractionStatus.Unknown);
        }

        // (e) Elapsed → assemble the safe expiry result (the recorded expiry is the authority) and dispatch one command.
        var result = new AgentProposalExpiryResult(
            AgentProposalExpiryOutcome.Expired,
            request.ProposalId,
            request.SourceConversationId,
            expiresAt);
        await DispatchAsync(request, result, ct).ConfigureAwait(false);
        return new AgentInteractionProposalExpiryOutcomeResult(AgentProposalExpiryOutcome.Expired, AgentProposalExpiryPolicy.Decide(result));
    }

    private static bool IsExpirable(ProposedAgentReplyState state)
        => state is ProposedAgentReplyState.Pending
            or ProposedAgentReplyState.Edited
            or ProposedAgentReplyState.Regenerated;

    // Parses the recorded ISO-8601 expiry timestamp invariantly and round-trip-aware. A garbage/unparseable value fails
    // closed (no transition) rather than throwing — the recorded expiry is the sole authority and must be unambiguous.
    private static bool TryParseExpiry(string expiresAt, out DateTimeOffset expiry)
        => DateTimeOffset.TryParse(
            expiresAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal,
            out expiry);

    // Reads the optional configured expiry fail-closed (no-expiry on any fault); a genuine cancellation propagates (the
    // catch deliberately excludes OperationCanceledException). The deferred default returns ProposalExpiryPolicyResult.None.
    private async Task<ProposalExpiryPolicyResult> ReadExpiryAsync(AgentInteractionProposalExpiryRequest request, CancellationToken ct)
    {
        try
        {
            return await _expiryReader.ReadAsync(request.TenantId, request.AgentId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProposalExpiryPolicyResult.None;
        }
    }

    private async Task DispatchAsync(AgentInteractionProposalExpiryRequest request, AgentProposalExpiryResult result, CancellationToken ct)
    {
        var command = new ExpireProposedAgentReply(request.AgentInteractionId, result);
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            request.AgentInteractionId,
            nameof(ExpireProposedAgentReply),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
    }

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
