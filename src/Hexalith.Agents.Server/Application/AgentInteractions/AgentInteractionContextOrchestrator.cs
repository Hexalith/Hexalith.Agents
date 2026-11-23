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
/// Application orchestration for Conversation context building (Story 2.3; AC1–AC4; AD-3, AD-11, AD-12). It reads the
/// authorized Source Conversation content, admits the model budget, measures the exact payload, resolves the context
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
/// <c>ContextBlocked</c> decision. The returned status predicts the outcome using the supplied snapshot. The
/// aggregate independently evaluates its persisted snapshot when processing the command.
/// </remarks>
public sealed class AgentInteractionContextOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    // Reserved server-populated trust keys. This context path repopulates none of them; they are stripped from
    // client-supplied extensions so a client cannot smuggle a forged admin/verdict onto the interaction stream.
    private static readonly string[] _reservedExtensionKeys =
    [
        AgentSetupTrustedExtensions.AgentAdministrator,
        AgentSetupTrustedExtensions.ProviderSelectionValidation,
        AgentSetupTrustedExtensions.ApproverPolicyValidation,
        AgentSetupTrustedExtensions.PartyLinkValidation,
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
        var envelope = new AgentCommandIntent(
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

        // Submission precedes the aggregate decision. This local evaluation uses the caller's snapshot.
        ct.ThrowIfCancellationRequested();
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
        return new AgentInteractionContextOutcomeResult(request.AgentInteractionId,
            AgentInteractionContextPolicy.Decide(measurement, request.ProviderCapabilityVersion));
    }

    private async Task<AgentInteractionContextMeasurement> AssembleMeasurementAsync(AgentInteractionContextRequest request, CancellationToken ct)
    {
        (string? payload, int messageCount, AgentInteractionContextLoadOutcome outcome) = await GenerationContext.ReadPayloadAsync(
            _contextReader, request.TenantId, request.SourceConversationId, request.CallerPartyId, request.ActorUserId, request.CorrelationId, ct)
            .ConfigureAwait(false);
        AgentInteractionContextMeasurement measurement = new(
            outcome,
            FullContextTokenCount: 0,
            messageCount,
            ContextWindowTokenLimit: 0,
            ReservedOutputTokenCount: 0,
            ProviderCapabilityVersion: 0,
            request.ContextPolicyReference,
            ContextPolicyResolution.Resolve(request.ContextPolicyReference));
        if (payload is null)
        {
            return measurement;
        }

        ct.ThrowIfCancellationRequested();
        GenerationCatalogRead catalog = await GenerationCatalogRead.ReadAsync(
            _providerCatalogReader, request.TenantId, request.ProviderId, request.ModelId, request.ProviderCapabilityVersion, ct)
            .ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        if (catalog.Entry is not { } entry)
        {
            return measurement;
        }

        ConversationContextTokenMeasurement tokens = await GenerationContext.MeasureAsync(_tokenMeasurer, payload, entry, ct).ConfigureAwait(false);
        return tokens.IsAvailable
            ? measurement with
            {
                FullContextTokenCount = tokens.TokenCount,
                ContextWindowTokenLimit = entry.ContextWindowTokenLimit,
                ReservedOutputTokenCount = entry.MaxOutputTokenLimit,
                ProviderCapabilityVersion = entry.CapabilityVersion,
            }
            : measurement;
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
            if (!Hexalith.Agents.Contracts.Security.AgentTrustedExtensionKeys.IsReserved(key) && Array.IndexOf(_reservedExtensionKeys, key) < 0)
            {
                extensions[key] = value;
            }
        }

        return extensions.Count > 0 ? extensions : null;
    }
}
