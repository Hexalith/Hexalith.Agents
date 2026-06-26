using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Application orchestration for automatic posting of a generated Agent response (Story 2.5; AC1–AC4; AD-3, AD-6, AD-7,
/// AD-13, AD-14, AD-18). It IS the durable-owner posting step: it reads the Agent's linked Party identity + posting-time
/// validity, reads the selected generated version (content + version id), verifies the Agent's <c>AiAgent</c> membership in
/// the Source Conversation, appends the message authored by the Agent Party with a deterministic <c>MessageId</c>/idempotency
/// key, assembles a server-trusted <see cref="AgentResponsePostingResult"/> (safe ids only — no content), and dispatches the
/// <see cref="PostAgentResponse"/> command. The impure work happens here, outside the pure aggregate, and feeds back through
/// the single command (the AD-3 round-trip).
/// </summary>
/// <remarks>
/// <b>Conversations boundary (AD-6/AD-7):</b> posting and membership go ONLY through <see cref="IConversationResponsePoster"/>
/// (over the public <c>IConversationClient</c>); Agents never writes Conversation streams directly, and posting fails closed
/// if Party identity, membership, or the membership seam is missing/unavailable. <b>Sensitive content (AD-14):</b> the
/// generated content is read by <see cref="IAgentGeneratedVersionReader"/>, handed straight to the poster's append, and rides
/// into the aggregate NOWHERE — the dispatched command/result/event carry only safe ids. <b>Fail closed (FR-12):</b> every
/// dependency-class failure short-circuits to a content-free fail-closed result so the pure policy records
/// <c>PostingFailed</c> audit evidence and no Conversation Message is created. The returned status comes from the shared
/// <see cref="AgentResponsePostingPolicy"/> so it cannot drift from the aggregate's recorded decision.
/// </remarks>
public sealed class AgentInteractionPostingOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    // Reserved server-populated trust keys. This posting path repopulates none of them; they are stripped from
    // client-supplied extensions so a client cannot smuggle a forged admin/verdict onto the interaction stream.
    private static readonly string[] _reservedExtensionKeys =
    [
        "actor:agentsAdmin",
        "provider:selectionValidation",
        "approver:policyValidation",
        "party:linkValidation",
    ];

    private readonly IAgentPartyReader _partyReader;
    private readonly IAgentGeneratedVersionReader _versionReader;
    private readonly IConversationResponsePoster _poster;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionPostingOrchestrator"/> class.</summary>
    /// <param name="partyReader">The Agent-Party validity reader port (live binding deferred — fails closed).</param>
    /// <param name="versionReader">The selected-generated-version reader port (live binding deferred — fails closed).</param>
    /// <param name="poster">The Conversation posting + membership port (live binding conditional on a Conversations config section).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentInteractionPostingOrchestrator(
        IAgentPartyReader partyReader,
        IAgentGeneratedVersionReader versionReader,
        IConversationResponsePoster poster,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(partyReader);
        ArgumentNullException.ThrowIfNull(versionReader);
        ArgumentNullException.ThrowIfNull(poster);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _partyReader = partyReader;
        _versionReader = versionReader;
        _poster = poster;
        _dispatcher = dispatcher;
    }

    /// <summary>Reads + verifies membership + appends, assembles + dispatches the post command, and returns the safe decided outcome (AC1–AC4).</summary>
    /// <param name="request">The posting orchestration request (snapshot-recorded ids/mode + caller context).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe posting outcome (interaction id + decided status).</returns>
    public async Task<AgentInteractionPostingOutcomeResult> ExecuteAsync(AgentInteractionPostingRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // (1) Response-mode short-circuit (defensive — the durable owner only invokes this for automatic mode). A
        // Confirmation-mode interaction posts via Epic 3 approval (Story 3.5), so do NOT post here: the aggregate would
        // reject the command anyway (no state change), leaving the interaction Generated. Return that unchanged status
        // without dispatching.
        if (request.ResponseMode != AgentResponseMode.Automatic)
        {
            return new AgentInteractionPostingOutcomeResult(request.AgentInteractionId, AgentInteractionStatus.Generated);
        }

        // Assemble the server-trusted posting result from the impure steps (each fail-closed). The sensitive content is
        // used only transiently inside PostAsync (handed to the poster's append) and rides into the aggregate NOWHERE (AD-14).
        AgentResponsePostingResult result = await PostAsync(request, ct).ConfigureAwait(false);

        // Build the server-trusted post command — any client-supplied value is discarded. The interaction id mirrors the
        // envelope aggregate id (Story 2.1).
        var command = new PostAgentResponse(request.AgentInteractionId, result);

        // Build the envelope (AggregateId = interaction id, Domain = agent-interaction); strip reserved client trust
        // extensions (none repopulated on this path).
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            request.AgentInteractionId,
            nameof(PostAgentResponse),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        // Dispatch (live binding deferred), then return the safe outcome — status via the SHARED policy so the
        // orchestrator's reported status cannot drift from the aggregate's recorded decision (AD-3).
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
        return new AgentInteractionPostingOutcomeResult(request.AgentInteractionId, AgentResponsePostingPolicy.Decide(result));
    }

    // Assembles the server-trusted posting result: read Agent Party → read selected version → ensure membership → append.
    // Every step is fail-closed; the first failing step short-circuits to a content-free failure result so the pure policy
    // records a fail-closed audit and no Conversation Message is created (AD-6, AD-12).
    private async Task<AgentResponsePostingResult> PostAsync(AgentInteractionPostingRequest request, CancellationToken ct)
    {
        // (2) Read the Agent's linked Party identity + posting-time validity (the snapshot does NOT carry it — AD-7). Not
        // available/disabled/not-linked → PartyIdentityUnavailable.
        AgentPartyReadResult party = await ReadPartyAsync(request, ct).ConfigureAwait(false);
        if (party.Outcome != AgentPartyReadOutcome.Available || string.IsNullOrWhiteSpace(party.PartyId))
        {
            return FailedResult(AgentResponsePostingOutcome.PartyIdentityUnavailable, request, agentPartyId: string.Empty, versionId: string.Empty);
        }

        string agentPartyId = party.PartyId;

        // (3) Read the selected generated version (content + version id). Not available → AdapterFailure (cannot post
        // without the durable version; the aggregate precondition already guarantees Generated, so this only fails if the
        // content read is unavailable — e.g. the deferred reader / content protection unavailable; AD-14).
        AgentGeneratedVersionReadResult version = await ReadVersionAsync(request, ct).ConfigureAwait(false);
        if (version.Outcome != AgentGeneratedVersionReadOutcome.Available || version.GeneratedContent is null)
        {
            return FailedResult(AgentResponsePostingOutcome.AdapterFailure, request, agentPartyId, versionId: string.Empty);
        }

        string versionId = version.VersionId;

        // (4) Ensure the Agent is an AiAgent participant of the Source Conversation (verify; establish has no public seam).
        ConversationMembershipResult membership = await EnsureMembershipAsync(request, agentPartyId, ct).ConfigureAwait(false);
        if (membership.Outcome is not (ConversationMembershipOutcome.Present or ConversationMembershipOutcome.Established))
        {
            return FailedResult(MapMembershipFailure(membership.Outcome), request, agentPartyId, versionId);
        }

        // (5) Derive the deterministic message id + idempotency key from the interaction + selected version so a retry
        // reuses the same identity and Conversations dedupes the append (AD-13).
        string messageId = AgentResponsePostingIdentity.DeriveMessageId(request.AgentInteractionId, versionId);
        string idempotencyKey = AgentResponsePostingIdentity.DeriveIdempotencyKey(request.AgentInteractionId, versionId);

        // (6) Append the message authored by the Agent Party. The sensitive content stays inside the adapter (AD-14).
        ConversationAppendResult append = await AppendAsync(request, agentPartyId, version.GeneratedContent, messageId, idempotencyKey, ct).ConfigureAwait(false);

        // (7) Assemble the server-trusted result with the outcome + safe ids — never the content.
        return new AgentResponsePostingResult(
            MapAppendOutcome(append.Outcome),
            messageId,
            request.SourceConversationId,
            agentPartyId,
            versionId);
    }

    private async Task<AgentPartyReadResult> ReadPartyAsync(AgentInteractionPostingRequest request, CancellationToken ct)
    {
        try
        {
            return await _partyReader.ReadAsync(request.TenantId, request.AgentId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — never propagate the raw exception (AD-14: no stack traces/payloads leak).
            return AgentPartyReadResult.Unavailable;
        }
    }

    private async Task<AgentGeneratedVersionReadResult> ReadVersionAsync(AgentInteractionPostingRequest request, CancellationToken ct)
    {
        try
        {
            return await _versionReader.ReadSelectedVersionAsync(request.TenantId, request.AgentInteractionId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12, AD-14) — a version that cannot be read drives AdapterFailure, never a post without content.
            return AgentGeneratedVersionReadResult.NotAvailable;
        }
    }

    private async Task<ConversationMembershipResult> EnsureMembershipAsync(AgentInteractionPostingRequest request, string agentPartyId, CancellationToken ct)
    {
        try
        {
            var membershipRequest = new ConversationMembershipRequest(
                request.TenantId,
                request.SourceConversationId,
                agentPartyId,
                request.ActorUserId,
                request.CorrelationId);
            return await _poster.EnsureAiAgentParticipantAsync(membershipRequest, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — a membership check that throws fails closed (no raw error leak; AD-14).
            return ConversationMembershipResult.ConversationUnavailable;
        }
    }

    private async Task<ConversationAppendResult> AppendAsync(
        AgentInteractionPostingRequest request,
        string agentPartyId,
        string generatedContent,
        string messageId,
        string idempotencyKey,
        CancellationToken ct)
    {
        try
        {
            var appendRequest = new ConversationAppendRequest(
                request.TenantId,
                request.SourceConversationId,
                agentPartyId,
                generatedContent,
                messageId,
                idempotencyKey,
                request.CorrelationId);
            return await _poster.AppendAgentMessageAsync(appendRequest, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — the Conversations error text never crosses this boundary (AD-14).
            return ConversationAppendResult.AdapterFailure;
        }
    }

    // A content-free failure result — never carries the generated content (AD-14). Carries the safe ids attempted so far
    // (the deterministic message id derived from the known version id, empty for pre-version failures).
    private static AgentResponsePostingResult FailedResult(
        AgentResponsePostingOutcome outcome,
        AgentInteractionPostingRequest request,
        string agentPartyId,
        string versionId)
        => new(
            outcome,
            string.IsNullOrEmpty(versionId) ? string.Empty : AgentResponsePostingIdentity.DeriveMessageId(request.AgentInteractionId, versionId),
            request.SourceConversationId,
            agentPartyId,
            versionId);

    // Maps a non-success membership outcome to its fail-closed posting outcome. Unknown fails closed to MembershipUnavailable.
    private static AgentResponsePostingOutcome MapMembershipFailure(ConversationMembershipOutcome outcome) => outcome switch
    {
        ConversationMembershipOutcome.MembershipRejected => AgentResponsePostingOutcome.MembershipRejected,
        ConversationMembershipOutcome.ConversationUnavailable => AgentResponsePostingOutcome.ConversationUnavailable,
        ConversationMembershipOutcome.SeamUnavailable => AgentResponsePostingOutcome.MembershipUnavailable,
        _ => AgentResponsePostingOutcome.MembershipUnavailable,
    };

    // Maps the append outcome to its posting outcome. Posted → Posted; everything else fails closed coarsely.
    private static AgentResponsePostingOutcome MapAppendOutcome(ConversationAppendOutcome outcome) => outcome switch
    {
        ConversationAppendOutcome.Posted => AgentResponsePostingOutcome.Posted,
        ConversationAppendOutcome.PostRejected => AgentResponsePostingOutcome.PostRejected,
        ConversationAppendOutcome.ConversationUnavailable => AgentResponsePostingOutcome.ConversationUnavailable,
        _ => AgentResponsePostingOutcome.AdapterFailure,
    };

    // Copies the client-supplied extensions with the reserved trust keys removed; repopulates none (the posting path
    // carries no admin/verdict extension). Returns null when nothing benign remains so the envelope carries no empty map.
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
