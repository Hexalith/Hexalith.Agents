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
/// Application orchestration for Confirmation-mode creation of a Proposed Agent Reply (Story 3.1; AC1–AC4; AD-3, AD-5, AD-6,
/// AD-13, AD-14, AD-18). It IS the durable-owner Confirmation-mode step — the counterpart to
/// <see cref="AgentInteractionPostingOrchestrator"/>: it reads the selected generated version (for its authoritative version
/// id ONLY), reads the optional expiry metadata, derives a deterministic <c>ProposalId</c>, assembles a server-trusted
/// <see cref="AgentProposalCreationResult"/> (safe ids only — no content), and dispatches the
/// <see cref="CreateProposedAgentReply"/> command. The impure work happens here, outside the pure aggregate, and feeds back
/// through the single command (the AD-3 round-trip).
/// </summary>
/// <remarks>
/// <b>Conversations boundary (AD-6):</b> a Proposed Agent Reply is NEVER a Conversation Message — this path makes NO
/// Conversations write and reads NO Party identity/membership (those are posting/approval concerns, Stories 2.5/3.5). AC2
/// holds by construction. <b>Sensitive content (AD-14):</b> the version reader returns the generated content, but this
/// orchestrator DELIBERATELY IGNORES it and uses only the version <em>id</em> — the content's sole durable home stays the
/// Story 2.4 generated version. <b>Fail closed (FR-21):</b> every dependency read is wrapped fail-closed so the pure policy
/// records <c>ProposalCreationFailed</c> audit evidence and no approvable proposal exists. The returned status comes from
/// the shared <see cref="AgentProposalCreationPolicy"/> so it cannot drift from the aggregate's recorded decision.
/// </remarks>
public sealed class AgentInteractionProposalOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    // Reserved server-populated trust keys. This proposal path repopulates none of them; they are stripped from
    // client-supplied extensions so a client cannot smuggle a forged admin/verdict onto the interaction stream.
    private static readonly string[] _reservedExtensionKeys =
    [
        "actor:agentsAdmin",
        "provider:selectionValidation",
        "approver:policyValidation",
        "party:linkValidation",
    ];

    private readonly IAgentGeneratedVersionReader _versionReader;
    private readonly IProposalExpiryPolicyReader _expiryReader;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionProposalOrchestrator"/> class.</summary>
    /// <param name="versionReader">The selected-generated-version reader port — reused for its authoritative version id only (live binding deferred — fails closed).</param>
    /// <param name="expiryReader">The optional proposal expiry-policy reader port (live binding deferred to Story 3.6 — returns no expiry).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentInteractionProposalOrchestrator(
        IAgentGeneratedVersionReader versionReader,
        IProposalExpiryPolicyReader expiryReader,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(versionReader);
        ArgumentNullException.ThrowIfNull(expiryReader);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _versionReader = versionReader;
        _expiryReader = expiryReader;
        _dispatcher = dispatcher;
    }

    /// <summary>Reads the selected version id + optional expiry, assembles + dispatches the create command, and returns the safe decided outcome (AC1–AC4).</summary>
    /// <param name="request">The proposal orchestration request (snapshot-recorded ids/mode + policy-snapshot versions + caller context).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe proposal outcome (interaction id + decided status).</returns>
    public async Task<AgentInteractionProposalOutcomeResult> ExecuteAsync(AgentInteractionProposalRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // (1) Response-mode short-circuit (defensive — the durable owner only invokes this for Confirmation mode). An
        // Automatic-mode interaction posts via Story 2.5, so do NOT create a proposal here: the aggregate would reject the
        // command anyway (no state change), leaving the interaction Generated. Return that unchanged status without dispatching.
        if (request.ResponseMode != AgentResponseMode.Confirmation)
        {
            return new AgentInteractionProposalOutcomeResult(request.AgentInteractionId, AgentInteractionStatus.Generated);
        }

        // Assemble the server-trusted creation result from the impure steps (each fail-closed). The generated content read
        // alongside the version id is DELIBERATELY IGNORED and rides into the aggregate NOWHERE (AD-14).
        AgentProposalCreationResult result = await CreateAsync(request, ct).ConfigureAwait(false);

        // Build the server-trusted create command — any client-supplied value is discarded. The interaction id mirrors the
        // envelope aggregate id (Story 2.1).
        var command = new CreateProposedAgentReply(request.AgentInteractionId, result);

        // Build the envelope (AggregateId = interaction id, Domain = agent-interaction); strip reserved client trust
        // extensions (none repopulated on this path).
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            request.AgentInteractionId,
            nameof(CreateProposedAgentReply),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        // Dispatch (live binding deferred), then return the safe outcome — status via the SHARED policy so the
        // orchestrator's reported status cannot drift from the aggregate's recorded decision (AD-3).
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
        return new AgentInteractionProposalOutcomeResult(request.AgentInteractionId, AgentProposalCreationPolicy.Decide(result));
    }

    // Assembles the server-trusted creation result: read selected version (id only) → read optional expiry → derive id →
    // assemble. Every step is fail-closed; a failing version read short-circuits to a content-free failure result so the
    // pure policy records a fail-closed audit and no approvable proposal exists (AD-12).
    private async Task<AgentProposalCreationResult> CreateAsync(AgentInteractionProposalRequest request, CancellationToken ct)
    {
        // (2) Read the selected generated version. The aggregate precondition already guarantees Generated, so this only
        // fails if the read is unavailable (e.g. the deferred reader / content protection unavailable). CRITICAL (AD-14):
        // use ONLY version.VersionId — the GeneratedContent from the read is DELIBERATELY IGNORED; the content's sole
        // durable home stays the Story 2.4 AgentOutputGenerated event/state, and the proposal references the version id only.
        AgentGeneratedVersionReadResult version = await ReadVersionAsync(request, ct).ConfigureAwait(false);
        if (version.Outcome != AgentGeneratedVersionReadOutcome.Available)
        {
            return FailedResult(AgentProposalCreationOutcome.GeneratedVersionUnavailable, request, versionId: string.Empty);
        }

        string versionId = version.VersionId;

        // (3) Read the optional expiry metadata (null when no expiry policy is configured — AC1). Fail-closed to no expiry.
        ProposalExpiryPolicyResult expiry = await ReadExpiryAsync(request, ct).ConfigureAwait(false);
        string? expiresAt = expiry.ExpiresAt;

        // (4) Derive the deterministic proposal id from the interaction + selected version so a retry reuses the same id and
        // the aggregate's terminal no-op dedupes the creation (AD-13). ExpiresAt is excluded from the identity.
        string proposalId = AgentProposalIdentity.DeriveProposalId(request.AgentInteractionId, versionId);

        // (5) Assemble the server-trusted result with the success outcome + safe ids — never the content.
        return new AgentProposalCreationResult(
            AgentProposalCreationOutcome.Created,
            proposalId,
            request.SourceConversationId,
            versionId,
            request.ApproverPolicyVersion,
            request.ContentSafetyPolicyVersion,
            expiresAt);
    }

    private async Task<AgentGeneratedVersionReadResult> ReadVersionAsync(AgentInteractionProposalRequest request, CancellationToken ct)
    {
        try
        {
            return await _versionReader.ReadSelectedVersionAsync(request.TenantId, request.AgentInteractionId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12, AD-14) — a version that cannot be read drives GeneratedVersionUnavailable, never a proposal.
            return AgentGeneratedVersionReadResult.NotAvailable;
        }
    }

    private async Task<ProposalExpiryPolicyResult> ReadExpiryAsync(AgentInteractionProposalRequest request, CancellationToken ct)
    {
        try
        {
            return await _expiryReader.ReadAsync(request.TenantId, request.AgentId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — an expiry read that throws records no expiry rather than blocking an otherwise-valid proposal.
            return ProposalExpiryPolicyResult.None;
        }
    }

    // A content-free failure result — never carries the generated content (AD-14). Carries the safe ids attempted so far
    // (the deterministic proposal id derived from the known version id, empty for pre-version failures) plus the
    // policy-snapshot versions from the request; no expiry on a failure.
    private static AgentProposalCreationResult FailedResult(
        AgentProposalCreationOutcome outcome,
        AgentInteractionProposalRequest request,
        string versionId)
        => new(
            outcome,
            string.IsNullOrEmpty(versionId) ? string.Empty : AgentProposalIdentity.DeriveProposalId(request.AgentInteractionId, versionId),
            request.SourceConversationId,
            versionId,
            request.ApproverPolicyVersion,
            request.ContentSafetyPolicyVersion,
            ExpiresAt: null);

    // Copies the client-supplied extensions with the reserved trust keys removed; repopulates none (the proposal path
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
