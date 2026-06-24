using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Application orchestration for Conversation context building (Story 2.3; AC1–AC4; AD-3, AD-11, AD-12). It reads the
/// authorized Source Conversation content, measures its token count, reads the model budget, resolves the context
/// policy, assembles a server-trusted <see cref="AgentInteractionContextMeasurement"/>, and dispatches the
/// <see cref="BuildAgentInteractionContext"/> command. The impure reads happen here, outside the pure aggregate, and
/// feed back through the command (the AD-3 round-trip).
/// </summary>
/// <remarks>
/// <b>No side effects (AC3):</b> the orchestration only READS (content, tokens, catalog budget) and dispatches the
/// context command — it MUST NOT call any provider adapter (generation is Story 2.4), MUST NOT post to Conversations
/// (<c>AppendMessageAsync</c> — posting is Story 2.5), and MUST NOT create a Proposed Agent Reply. <b>Sensitive content
/// (AD-14):</b> the raw conversation text is used only transiently for measurement and then discarded — it is never put
/// on the command/result, never persisted, never logged. <b>Trust model:</b> any client-supplied measurement is
/// discarded (the request carries none) and reserved trust extension keys are stripped from the client extensions.
/// <b>Fail closed (FR-21):</b> any reader/measurer/catalog that throws or returns not-available drives a
/// <c>ContextBlocked</c> decision. The returned status is computed by the shared <see cref="AgentInteractionContextPolicy"/>
/// so it cannot drift from the aggregate's recorded decision.
/// </remarks>
public sealed class AgentInteractionContextOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    // Reserved server-populated trust keys. This context path repopulates none of them; they are stripped from
    // client-supplied extensions so a client cannot smuggle a forged admin/verdict onto the interaction stream.
    private static readonly string[] _reservedExtensionKeys =
    [
        "actor:agentsAdmin",
        "provider:selectionValidation",
        "approver:policyValidation",
        "party:linkValidation",
    ];

    private readonly IConversationContextReader _contextReader;
    private readonly IConversationContextTokenMeasurer _tokenMeasurer;
    private readonly IProviderCatalogReader _providerCatalogReader;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionContextOrchestrator"/> class.</summary>
    /// <param name="contextReader">The Source Conversation content read port (live binding conditional on a Conversations config section).</param>
    /// <param name="tokenMeasurer">The token-measurement port (live binding deferred — no tokenizer bound).</param>
    /// <param name="providerCatalogReader">The reused provider-catalog reader (model budget source).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentInteractionContextOrchestrator(
        IConversationContextReader contextReader,
        IConversationContextTokenMeasurer tokenMeasurer,
        IProviderCatalogReader providerCatalogReader,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(contextReader);
        ArgumentNullException.ThrowIfNull(tokenMeasurer);
        ArgumentNullException.ThrowIfNull(providerCatalogReader);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _contextReader = contextReader;
        _tokenMeasurer = tokenMeasurer;
        _providerCatalogReader = providerCatalogReader;
        _dispatcher = dispatcher;
    }

    /// <summary>Reads + measures the context, assembles + dispatches the context command, and returns the safe decided outcome (AC1–AC4).</summary>
    /// <param name="request">The context orchestration request (snapshot-recorded ids/policy + caller context).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe context outcome (interaction id + decided status).</returns>
    public async Task<AgentInteractionContextOutcomeResult> ExecuteAsync(AgentInteractionContextRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Assemble the server-trusted measurement from the impure reads (each fail-closed). The raw conversation text is
        // used only transiently inside AssembleMeasurementAsync and is never carried onto the measurement (AD-14).
        AgentInteractionContextMeasurement measurement = await AssembleMeasurementAsync(request, ct).ConfigureAwait(false);

        // Build the server-trusted context command — any client-supplied measurement is discarded (the request carries
        // none). The interaction id mirrors the envelope aggregate id (Story 2.1).
        var command = new BuildAgentInteractionContext(request.AgentInteractionId, measurement);

        // Build the envelope (AggregateId = interaction id, Domain = agent-interaction); strip reserved client trust
        // extensions (none repopulated on this path).
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            request.AgentInteractionId,
            nameof(BuildAgentInteractionContext),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        // Dispatch (live binding deferred), then return the safe outcome — status via the SHARED policy so the
        // orchestrator's reported status cannot drift from the aggregate's recorded decision (AD-3).
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
        return new AgentInteractionContextOutcomeResult(request.AgentInteractionId, AgentInteractionContextPolicy.Decide(measurement));
    }

    // Assembles the server-trusted measurement: read content → (if loaded) measure tokens + read budget → resolve
    // policy. Every read is fail-closed; a non-loaded read, an unavailable measurement, or an untrustworthy budget all
    // produce a measurement the pure policy maps to ContextBlocked.
    private async Task<AgentInteractionContextMeasurement> AssembleMeasurementAsync(AgentInteractionContextRequest request, CancellationToken ct)
    {
        ConversationContextReadResult read = await ReadContextAsync(request, ct).ConfigureAwait(false);
        AgentInteractionBoundedContextBehavior? approvedBoundedBehavior = ContextPolicyResolution.Resolve(request.ContextPolicyReference);

        // (1) Not loaded → assemble a measurement with that load outcome and 0 numerics (skip measurement/budget — the
        // policy maps it to ContextBlocked). The raw messages, if any, are not carried.
        if (read.Outcome != AgentInteractionContextLoadOutcome.Loaded || read.Messages is null)
        {
            AgentInteractionContextLoadOutcome loadOutcome = read.Outcome == AgentInteractionContextLoadOutcome.Loaded
                ? AgentInteractionContextLoadOutcome.Unavailable // loaded but no messages payload is a degraded read
                : read.Outcome;
            return new AgentInteractionContextMeasurement(
                loadOutcome,
                FullContextTokenCount: 0,
                MessageCount: 0,
                ContextWindowTokenLimit: 0,
                ReservedOutputTokenCount: 0,
                ProviderCapabilityVersion: 0,
                request.ContextPolicyReference,
                approvedBoundedBehavior);
        }

        // (2) Measure tokens (fail-closed → NotAvailable) and read the budget (fail-closed → zeroed).
        ConversationContextTokenMeasurement tokens = await MeasureTokensAsync(read.Messages, request, ct).ConfigureAwait(false);
        ModelBudget budget = await ReadModelBudgetAsync(request, ct).ConfigureAwait(false);

        // (3) Token measurement unavailable → drive ModelBudgetUnavailable by zeroing the window (the policy blocks on a
        // non-positive ContextWindowTokenLimit). The conversation text is now discarded.
        if (!tokens.IsAvailable)
        {
            return new AgentInteractionContextMeasurement(
                AgentInteractionContextLoadOutcome.Loaded,
                FullContextTokenCount: 0,
                read.MessageCount,
                ContextWindowTokenLimit: 0,
                ReservedOutputTokenCount: 0,
                ProviderCapabilityVersion: 0,
                request.ContextPolicyReference,
                approvedBoundedBehavior);
        }

        // (4) Loaded + measured: assemble the trusted measurement (token COUNT only, never raw text — AD-14).
        return new AgentInteractionContextMeasurement(
            AgentInteractionContextLoadOutcome.Loaded,
            tokens.TokenCount,
            read.MessageCount,
            budget.ContextWindowTokenLimit,
            budget.ReservedOutputTokenCount,
            budget.ProviderCapabilityVersion,
            request.ContextPolicyReference,
            approvedBoundedBehavior);
    }

    private async Task<ConversationContextReadResult> ReadContextAsync(AgentInteractionContextRequest request, CancellationToken ct)
    {
        try
        {
            // The Conversations authorized read keys on a security principal — pass ActorUserId as callerPrincipalId.
            return await _contextReader
                .ReadAsync(request.TenantId, request.SourceConversationId, request.CallerPartyId, request.ActorUserId, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — never propagate the raw exception (AD-14: no stack traces/payloads leak).
            return ConversationContextReadResult.Unavailable;
        }
    }

    private async Task<ConversationContextTokenMeasurement> MeasureTokensAsync(IReadOnlyList<ConversationContextMessage> messages, AgentInteractionContextRequest request, CancellationToken ct)
    {
        try
        {
            return await _tokenMeasurer.MeasureAsync(messages, request.ProviderId, request.ModelId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ConversationContextTokenMeasurement.NotAvailable;
        }
    }

    // Reads the model budget from the reused provider catalog. Any failure / missing / disabled / not-text-capable /
    // invalid-limits read returns a zeroed budget so the pure policy blocks with ModelBudgetUnavailable (fail closed).
    private async Task<ModelBudget> ReadModelBudgetAsync(AgentInteractionContextRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderId) || string.IsNullOrWhiteSpace(request.ModelId))
        {
            return ModelBudget.Unavailable;
        }

        try
        {
            ProviderCatalogEntryReadResult result = await _providerCatalogReader
                .GetEntryAsync(request.TenantId, request.ProviderId, request.ModelId, ct)
                .ConfigureAwait(false);

            return result is { Status: ProviderCatalogInspectionStatus.Success, Entry: { SupportsTextGeneration: true } entry }
                ? new ModelBudget(entry.ContextWindowTokenLimit, entry.MaxOutputTokenLimit, entry.CapabilityVersion)
                : ModelBudget.Unavailable;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ModelBudget.Unavailable;
        }
    }

    // Copies the client-supplied extensions with the reserved trust keys removed; repopulates none (the context path
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

    // The safe model-budget read result. Zeroed (Unavailable) values drive the pure policy to ModelBudgetUnavailable.
    private readonly record struct ModelBudget(int ContextWindowTokenLimit, int ReservedOutputTokenCount, int ProviderCapabilityVersion)
    {
        public static ModelBudget Unavailable { get; } = new(0, 0, 0);
    }
}
