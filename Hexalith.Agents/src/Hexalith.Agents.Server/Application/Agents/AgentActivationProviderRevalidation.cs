using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Minimal activation orchestration that re-validates the Agent's recorded Provider/model against the live catalog
/// before dispatching <c>ActivateAgent</c> (Story 1.5 AC2 "or activate"; AD-3, AD-9, AD-12). Story 1.3 dispatched
/// <c>ActivateAgent</c> directly; this step adds the catalog read + trusted <c>provider:selectionValidation</c>
/// verdict so the aggregate's provider gate can clear for a genuinely-ready selection — and fail closed
/// (<see cref="AgentActivationBlocker.ProviderUnavailable"/>) for a selected-but-not-ready one.
/// </summary>
/// <remarks>
/// Same trust model as the selection orchestration: <c>actor:agentsAdmin</c> and <c>provider:selectionValidation</c>
/// are server-populated only; client-supplied reserved keys are stripped and repopulated from trusted sources. When
/// no selection is recorded, no catalog read happens and the verdict stays <c>Unknown</c> — the aggregate blocks
/// with <see cref="AgentActivationBlocker.MissingProviderSelection"/> regardless of the verdict. The live reader /
/// dispatcher bindings are deferred (mirroring Story 1.2/1.4).
/// </remarks>
public sealed class AgentActivationProviderRevalidation
{
    private const string AgentDomain = "agent";

    private static readonly string[] _reservedExtensionKeys =
    [
        AgentProviderSelectionOrchestrator.AgentAdminExtensionKey,
        AgentProviderSelectionOrchestrator.ProviderSelectionValidationExtensionKey,
    ];

    private readonly IProviderCatalogReader _catalogReader;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentActivationProviderRevalidation"/> class.</summary>
    /// <param name="catalogReader">The provider-catalog read port (live binding deferred).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentActivationProviderRevalidation(IProviderCatalogReader catalogReader, IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(catalogReader);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _catalogReader = catalogReader;
        _dispatcher = dispatcher;
    }

    /// <summary>Authorizes, re-validates the recorded selection, and dispatches <c>ActivateAgent</c> with the verdict (AC2).</summary>
    /// <param name="request">The sanitized activation request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The activation outcome (denied, or dispatched with the re-validated provider verdict).</returns>
    public async Task<AgentActivationRevalidationOutcome> ExecuteAsync(AgentActivationRevalidationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Authorize — fail closed before any catalog read or dispatch (AD-12).
        if (!request.IsAgentsAdmin)
        {
            return AgentActivationRevalidationOutcome.Denied();
        }

        // Re-read the catalog for the recorded selection (if any) and compute the fail-closed verdict. With no
        // recorded selection there is nothing to re-validate — the aggregate fails closed with MissingProviderSelection.
        ProviderSelectionValidationStatus verdict = ProviderSelectionValidationStatus.Unknown;
        if (request.SelectedProviderId is not null && request.SelectedModelId is not null)
        {
            ProviderCatalogEntryReadResult read = await _catalogReader
                .GetEntryAsync(request.TenantId, request.SelectedProviderId, request.SelectedModelId, ct)
                .ConfigureAwait(false);
            verdict = ProviderSelectionVerdict.Evaluate(read);
        }

        var command = new ActivateAgent();
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            AgentDomain,
            request.AgentId,
            nameof(ActivateAgent),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions, verdict));

        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);

        return AgentActivationRevalidationOutcome.FromDispatch(verdict);
    }

    private static Dictionary<string, string> BuildTrustedExtensions(
        IReadOnlyDictionary<string, string>? clientSupplied,
        ProviderSelectionValidationStatus verdict)
    {
        var extensions = new Dictionary<string, string>(StringComparer.Ordinal);

        if (clientSupplied is not null)
        {
            foreach ((string key, string value) in clientSupplied)
            {
                if (Array.IndexOf(_reservedExtensionKeys, key) < 0)
                {
                    extensions[key] = value;
                }
            }
        }

        extensions[AgentProviderSelectionOrchestrator.AgentAdminExtensionKey] = "true";
        extensions[AgentProviderSelectionOrchestrator.ProviderSelectionValidationExtensionKey] = verdict.ToString();
        return extensions;
    }
}
