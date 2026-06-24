using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Application orchestration for the invocation authorization + dependency-readiness gate (Story 2.2; AC1–AC4; AD-3,
/// AD-12). It reads every gate dependency through its port (each fail-closed on throw/not-available), maps each to one
/// safe <see cref="AgentInvocationGateVerdict"/> in <see cref="AgentInteractionGateCheck"/> evaluation order, assembles
/// the server-trusted <see cref="EvaluateAgentInteractionGate"/> command, and dispatches it through
/// <see cref="IAgentCommandDispatcher"/>. The impure dependency reads happen here, outside the pure aggregate, and feed
/// back through the command (the AD-3 round-trip).
/// </summary>
/// <remarks>
/// <b>No side effects (AC2):</b> the orchestration only READS dependency state — it MUST NOT call any provider adapter
/// (provider readiness is a catalog/projection read, never a model invocation), MUST NOT post to Conversations
/// (<c>AppendMessageAsync</c>), and MUST NOT create a Proposed Agent Reply. <b>Trust model (AC3):</b> the verdicts on
/// the dispatched command are server-read — any client-supplied verdict is discarded (the request carries none) and the
/// reserved trust extension keys are stripped from the client-supplied envelope extensions. <b>Fail closed (FR-21):</b>
/// any reader that throws or returns not-available maps to <see cref="AgentInteractionGateOutcome.Unavailable"/>; with
/// all live bindings deferred every verdict fails closed and the gate decides <see cref="AgentInteractionStatus.Denied"/>.
/// The returned status is computed by the shared <see cref="AgentInvocationGatePolicy"/> so it cannot drift from the
/// aggregate's recorded decision.
/// </remarks>
public sealed class AgentInteractionGateOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    // Reserved server-populated trust keys. This gate path repopulates none of them; they are stripped from
    // client-supplied extensions so a client cannot smuggle a forged admin/verdict onto the interaction stream.
    private static readonly string[] _reservedExtensionKeys =
    [
        "actor:agentsAdmin",
        "provider:selectionValidation",
        "approver:policyValidation",
        "party:linkValidation",
    ];

    private readonly ITenantAccessReader _tenantAccessReader;
    private readonly IConversationAccessReader _conversationAccessReader;
    private readonly IAgentInvocationReadinessReader _readinessReader;
    private readonly IAgentPartyDirectory _partyDirectory;
    private readonly IProviderCatalogReader _providerCatalogReader;
    private readonly IApproverPolicyResolver _approverPolicyResolver;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionGateOrchestrator"/> class.</summary>
    /// <param name="tenantAccessReader">The tenant-access read port (live binding deferred).</param>
    /// <param name="conversationAccessReader">The Source Conversation access read port (live binding deferred to Story 2.3).</param>
    /// <param name="readinessReader">The Agent current-readiness read port (live binding deferred).</param>
    /// <param name="partyDirectory">The reused Parties directory (caller + Agent Party validation).</param>
    /// <param name="providerCatalogReader">The reused provider-catalog reader (provider/model readiness).</param>
    /// <param name="approverPolicyResolver">The reused approver-policy resolver (Confirmation-mode response policy).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentInteractionGateOrchestrator(
        ITenantAccessReader tenantAccessReader,
        IConversationAccessReader conversationAccessReader,
        IAgentInvocationReadinessReader readinessReader,
        IAgentPartyDirectory partyDirectory,
        IProviderCatalogReader providerCatalogReader,
        IApproverPolicyResolver approverPolicyResolver,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(tenantAccessReader);
        ArgumentNullException.ThrowIfNull(conversationAccessReader);
        ArgumentNullException.ThrowIfNull(readinessReader);
        ArgumentNullException.ThrowIfNull(partyDirectory);
        ArgumentNullException.ThrowIfNull(providerCatalogReader);
        ArgumentNullException.ThrowIfNull(approverPolicyResolver);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _tenantAccessReader = tenantAccessReader;
        _conversationAccessReader = conversationAccessReader;
        _readinessReader = readinessReader;
        _partyDirectory = partyDirectory;
        _providerCatalogReader = providerCatalogReader;
        _approverPolicyResolver = approverPolicyResolver;
        _dispatcher = dispatcher;
    }

    /// <summary>Reads every dependency, assembles + dispatches the gate command, and returns the safe decided outcome (AC1–AC4).</summary>
    /// <param name="request">The gate orchestration request (snapshot-recorded ids + caller context).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe gate outcome (interaction id + decided status).</returns>
    public async Task<AgentInteractionGateOutcomeResult> ExecuteAsync(AgentInteractionGateRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // (1) Read every dependency through its port — each fail-closed on throw/not-available (AD-12; FR-21). No
        // provider adapter call, no Conversations post, no proposal anywhere on this path (AC2).
        TenantAccessReadResult tenant = await ReadTenantAccessAsync(request, ct).ConfigureAwait(false);
        AgentPartyValidationResult callerParty = await ValidatePartyAsync(request.TenantId, request.CallerPartyId, ct).ConfigureAwait(false);
        ConversationAccessReadResult conversation = await ReadConversationAccessAsync(request, ct).ConfigureAwait(false);
        AgentInvocationReadiness readiness = await ReadReadinessAsync(request, ct).ConfigureAwait(false);
        AgentInteractionGateOutcome agentPartyIdentity = await ReadAgentPartyIdentityAsync(request.TenantId, readiness, ct).ConfigureAwait(false);
        AgentInteractionGateOutcome providerReadiness = await ReadProviderReadinessAsync(request, ct).ConfigureAwait(false);
        AgentInteractionGateOutcome responsePolicy = await ResolveResponsePolicyAsync(request.TenantId, readiness, ct).ConfigureAwait(false);

        // (2) Map each read to exactly one verdict, in AgentInteractionGateCheck enum (evaluation) order.
        var verdicts = new List<AgentInvocationGateVerdict>
        {
            new(AgentInteractionGateCheck.TenantAccess, tenant.Outcome),
            new(AgentInteractionGateCheck.CallerPartyState, MapPartyVerdict(callerParty.Status)),
            new(AgentInteractionGateCheck.SourceConversationAccess, conversation.Outcome),
            new(AgentInteractionGateCheck.AgentLifecycle, MapLifecycle(readiness)),
            new(AgentInteractionGateCheck.AgentPartyIdentity, agentPartyIdentity),
            new(AgentInteractionGateCheck.ProviderModelReadiness, providerReadiness),
            new(AgentInteractionGateCheck.ResponsePolicy, responsePolicy),
            new(AgentInteractionGateCheck.ContentSafetyPolicy, MapContentSafety(readiness)),
            new(AgentInteractionGateCheck.DependencyFreshness, MapFreshness(tenant, conversation, readiness)),
        };

        // (3) Assemble the server-trusted gate command — the verdicts are server-read; any client-supplied verdict is
        // discarded (the request carries none). The interaction id mirrors the envelope aggregate id (Story 2.1).
        var command = new EvaluateAgentInteractionGate(request.AgentInteractionId, verdicts);

        // (4) Build the envelope (AggregateId = interaction id, Domain = agent-interaction); strip reserved client
        // trust extensions (none repopulated on this path).
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            request.AgentInteractionId,
            nameof(EvaluateAgentInteractionGate),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        // (5) Dispatch (live binding deferred). (6) Return the safe outcome — status via the SHARED policy so the
        // orchestrator's reported status cannot drift from the aggregate's recorded decision (AD-12).
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
        return new AgentInteractionGateOutcomeResult(request.AgentInteractionId, AgentInvocationGatePolicy.Decide(verdicts));
    }

    private async Task<TenantAccessReadResult> ReadTenantAccessAsync(AgentInteractionGateRequest request, CancellationToken ct)
    {
        try
        {
            return await _tenantAccessReader.ReadAsync(request.TenantId, request.ActorUserId, request.CallerPartyId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — never propagate the raw exception (AD-14: no stack traces/payloads leak).
            return TenantAccessReadResult.Unavailable;
        }
    }

    private async Task<ConversationAccessReadResult> ReadConversationAccessAsync(AgentInteractionGateRequest request, CancellationToken ct)
    {
        try
        {
            return await _conversationAccessReader.ReadAsync(request.TenantId, request.SourceConversationId, request.CallerPartyId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ConversationAccessReadResult.Unavailable;
        }
    }

    private async Task<AgentInvocationReadiness> ReadReadinessAsync(AgentInteractionGateRequest request, CancellationToken ct)
    {
        try
        {
            return await _readinessReader.ReadAsync(request.TenantId, request.AgentId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return AgentInvocationReadiness.NotAvailable;
        }
    }

    private async Task<AgentPartyValidationResult> ValidatePartyAsync(string tenantId, string partyId, CancellationToken ct)
    {
        try
        {
            return await _partyDirectory.ValidateExistingPartyAsync(tenantId, partyId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null);
        }
    }

    // AgentPartyIdentity (AD-7): the Agent's linked Party must be present and valid. Needs a readable Agent with a
    // linked Party id, then validates it through the reused directory.
    private async Task<AgentInteractionGateOutcome> ReadAgentPartyIdentityAsync(string tenantId, AgentInvocationReadiness readiness, CancellationToken ct)
    {
        if (!readiness.IsAvailable)
        {
            return AgentInteractionGateOutcome.Unavailable;
        }

        if (!readiness.HasPartyIdentity || string.IsNullOrWhiteSpace(readiness.PartyId))
        {
            return AgentInteractionGateOutcome.Missing;
        }

        AgentPartyValidationResult agentParty = await ValidatePartyAsync(tenantId, readiness.PartyId, ct).ConfigureAwait(false);
        return MapPartyVerdict(agentParty.Status);
    }

    // ProviderModelReadiness (AD-9): the snapshot-recorded provider/model entry must be CURRENTLY enabled/ready in the
    // catalog. The snapshot tells us WHICH provider/model; the catalog read tells us if it is ready now.
    private async Task<AgentInteractionGateOutcome> ReadProviderReadinessAsync(AgentInteractionGateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderId) || string.IsNullOrWhiteSpace(request.ModelId))
        {
            return AgentInteractionGateOutcome.Missing;
        }

        try
        {
            ProviderCatalogEntryReadResult result = await _providerCatalogReader
                .GetEntryAsync(request.TenantId, request.ProviderId, request.ModelId, ct)
                .ConfigureAwait(false);
            return MapProviderReadiness(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return AgentInteractionGateOutcome.Unavailable;
        }
    }

    // ResponsePolicy (AD-8): the CURRENT Response Mode must be set; a Confirmation-mode Agent additionally needs a
    // resolvable approver policy. Automatic needs no approver. Uses current readiness (AD-12), not the snapshot mode.
    private async Task<AgentInteractionGateOutcome> ResolveResponsePolicyAsync(string tenantId, AgentInvocationReadiness readiness, CancellationToken ct)
    {
        if (!readiness.IsAvailable)
        {
            return AgentInteractionGateOutcome.Unavailable;
        }

        switch (readiness.ResponseMode)
        {
            case AgentResponseMode.Automatic:
                return AgentInteractionGateOutcome.Satisfied;
            case AgentResponseMode.Confirmation:
                return await ResolveApproverPolicyAsync(tenantId, readiness.ApproverPolicy, ct).ConfigureAwait(false);
            case AgentResponseMode.Unknown:
            default:
                return AgentInteractionGateOutcome.Missing;
        }
    }

    private async Task<AgentInteractionGateOutcome> ResolveApproverPolicyAsync(string tenantId, AgentApproverPolicy? policy, CancellationToken ct)
    {
        if (policy is null)
        {
            // Confirmation mode with no approver policy has nothing to confirm with — fail closed (AC3; AD-8).
            return AgentInteractionGateOutcome.Missing;
        }

        try
        {
            ApproverPolicyResolutionResult result = await _approverPolicyResolver.ResolveAsync(tenantId, policy, ct).ConfigureAwait(false);
            return MapApproverPolicy(ApproverPolicyVerdict.Evaluate(policy, result));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return AgentInteractionGateOutcome.Unavailable;
        }
    }

    // Maps a Parties verdict to a gate outcome (1:1 — both share the missing/disabled/ambiguous/unavailable/unauthorized
    // vocabulary); an unknown verdict fails closed to Unavailable (AD-12).
    private static AgentInteractionGateOutcome MapPartyVerdict(PartyLinkValidationStatus status) => status switch
    {
        PartyLinkValidationStatus.Valid => AgentInteractionGateOutcome.Satisfied,
        PartyLinkValidationStatus.Missing => AgentInteractionGateOutcome.Missing,
        PartyLinkValidationStatus.Disabled => AgentInteractionGateOutcome.Disabled,
        PartyLinkValidationStatus.Ambiguous => AgentInteractionGateOutcome.Ambiguous,
        PartyLinkValidationStatus.Unauthorized => AgentInteractionGateOutcome.Unauthorized,
        _ => AgentInteractionGateOutcome.Unavailable,
    };

    private static AgentInteractionGateOutcome MapLifecycle(AgentInvocationReadiness readiness)
    {
        if (!readiness.IsAvailable)
        {
            return AgentInteractionGateOutcome.Unavailable;
        }

        return readiness.Lifecycle switch
        {
            AgentLifecycleStatus.Active => AgentInteractionGateOutcome.Satisfied,
            AgentLifecycleStatus.Disabled => AgentInteractionGateOutcome.Disabled,
            // Draft / Unknown: the Agent has not passed activation — not callable (AD-12).
            _ => AgentInteractionGateOutcome.Missing,
        };
    }

    // A successful read with a selectable entry is satisfied; a non-selectable entry is disabled; a missing entry is
    // Missing; a degraded (null entry) or not-authorized read fails closed to Unavailable (AC3 — no cross-tenant leak).
    private static AgentInteractionGateOutcome MapProviderReadiness(ProviderCatalogEntryReadResult result) => result switch
    {
        { Status: ProviderCatalogInspectionStatus.Success, Entry: { IsSelectableForNewActiveUse: true } } => AgentInteractionGateOutcome.Satisfied,
        { Status: ProviderCatalogInspectionStatus.Success, Entry: { IsSelectableForNewActiveUse: false } } => AgentInteractionGateOutcome.Disabled,
        { Status: ProviderCatalogInspectionStatus.EntryNotFound } => AgentInteractionGateOutcome.Missing,
        _ => AgentInteractionGateOutcome.Unavailable,
    };

    private static AgentInteractionGateOutcome MapApproverPolicy(ApproverPolicyValidationStatus status) => status switch
    {
        ApproverPolicyValidationStatus.Valid => AgentInteractionGateOutcome.Satisfied,
        ApproverPolicyValidationStatus.Incomplete => AgentInteractionGateOutcome.Missing,
        ApproverPolicyValidationStatus.Missing => AgentInteractionGateOutcome.Missing,
        ApproverPolicyValidationStatus.Disabled => AgentInteractionGateOutcome.Disabled,
        ApproverPolicyValidationStatus.Ambiguous => AgentInteractionGateOutcome.Ambiguous,
        ApproverPolicyValidationStatus.Unauthorized => AgentInteractionGateOutcome.Unauthorized,
        _ => AgentInteractionGateOutcome.Unavailable,
    };

    private static AgentInteractionGateOutcome MapContentSafety(AgentInvocationReadiness readiness)
    {
        if (!readiness.IsAvailable)
        {
            return AgentInteractionGateOutcome.Unavailable;
        }

        return readiness.HasActiveContentSafetyPolicy
            ? AgentInteractionGateOutcome.Satisfied
            : AgentInteractionGateOutcome.Missing;
    }

    // DependencyFreshness aggregates the freshness of the three consulted projection reads (the reused ports fold their
    // own staleness into their verdicts). Any behind-threshold projection fails closed to Stale (AD-12).
    private static AgentInteractionGateOutcome MapFreshness(TenantAccessReadResult tenant, ConversationAccessReadResult conversation, AgentInvocationReadiness readiness)
        => tenant.IsFresh && conversation.IsFresh && readiness.IsFresh
            ? AgentInteractionGateOutcome.Satisfied
            : AgentInteractionGateOutcome.Stale;

    // Copies the client-supplied extensions with the reserved trust keys removed; repopulates none (the gate path
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
