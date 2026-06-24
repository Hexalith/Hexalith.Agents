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
/// Application orchestration for selecting a Provider/model for an Agent (<c>hexa</c>) (Story 1.5; AC1, AC2, AC4;
/// AD-3, AD-9, AD-12). It authorizes the actor, reads the governed <c>ProviderCatalog</c> through
/// <see cref="IProviderCatalogReader"/>, computes the fail-closed <see cref="ProviderSelectionValidationStatus"/>
/// verdict and captures the entry's capability version, builds the <see cref="SelectAgentProviderModel"/> command
/// carrying only the safe ids + version, and dispatches it with the <b>server-populated</b> trusted extensions
/// <c>actor:agentsAdmin</c> and <c>provider:selectionValidation</c>. The catalog read runs here, outside the pure
/// aggregate, and its verdict returns to the aggregate through the trusted command extension (the AD-3 round-trip).
/// </summary>
/// <remarks>
/// <b>Trust model (CRITICAL):</b> both reserved extension keys are server-populated only. Any client-supplied value
/// for them is stripped at this entry point and repopulated from the trusted authorization decision and the catalog
/// read, so a client can never assert <c>provider:selectionValidation=Valid</c> to bypass catalog validation. The
/// verdict is always dispatched (even non-<c>Valid</c>) so the aggregate's independent rejection is auditable; that
/// aggregate-side rejection (not this orchestration) is the security guarantee against direct-gateway calls. No
/// provider SDK call or credential access occurs — only the safe catalog projection is read. Binding
/// <see cref="IProviderCatalogReader"/> and <see cref="IAgentCommandDispatcher"/> to the live read-model / command
/// gateway is deferred (mirroring Story 1.2/1.4).
/// </remarks>
public sealed class AgentProviderSelectionOrchestrator
{
    private const string AgentDomain = "agent";

    /// <summary>The server-populated Agents-admin authorization extension key (client-stripped).</summary>
    internal const string AgentAdminExtensionKey = "actor:agentsAdmin";

    /// <summary>The server-populated provider-readiness verdict extension key (client-stripped).</summary>
    internal const string ProviderSelectionValidationExtensionKey = "provider:selectionValidation";

    private static readonly string[] _reservedExtensionKeys =
    [
        AgentAdminExtensionKey,
        ProviderSelectionValidationExtensionKey,
    ];

    private readonly IProviderCatalogReader _catalogReader;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentProviderSelectionOrchestrator"/> class.</summary>
    /// <param name="catalogReader">The provider-catalog read port (live binding deferred).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentProviderSelectionOrchestrator(IProviderCatalogReader catalogReader, IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(catalogReader);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _catalogReader = catalogReader;
        _dispatcher = dispatcher;
    }

    /// <summary>Authorizes, computes the provider-readiness verdict, and dispatches the selection command (AC1, AC2, AC4).</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (denied, or dispatched with the computed verdict + captured version).</returns>
    public async Task<AgentProviderSelectionOutcome> ExecuteAsync(AgentProviderSelectionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // (1) Authorize — fail closed before any catalog read or dispatch (AD-12).
        if (!request.IsAgentsAdmin)
        {
            return AgentProviderSelectionOutcome.Denied();
        }

        // (2) Read the governed catalog entry (safe projection only — no secret value crosses the boundary).
        ProviderCatalogEntryReadResult read = await _catalogReader
            .GetEntryAsync(request.TenantId, request.ProviderId, request.ModelId, ct)
            .ConfigureAwait(false);

        // (3) Compute the deterministic fail-closed verdict, and (4) capture the entry's capability version.
        ProviderSelectionValidationStatus verdict = ProviderSelectionVerdict.Evaluate(read);
        int capabilityVersion = read.Entry?.CapabilityVersion ?? 0;

        // (5) Build the aggregate command — payload carries only the safe ids + captured version.
        var command = new SelectAgentProviderModel(request.ProviderId, request.ModelId, capabilityVersion);

        // (6) Build the envelope with server-populated trusted extensions (client reserved keys stripped).
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            AgentDomain,
            request.AgentId,
            nameof(SelectAgentProviderModel),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions, verdict));

        // (7) Dispatch — always feed the computed verdict (even non-Valid) so the aggregate records an auditable
        // AgentProviderModelSelectionRejected; the aggregate's independent non-Valid rejection is the security guarantee.
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);

        return AgentProviderSelectionOutcome.FromDispatch(verdict, capabilityVersion);
    }

    // Copies the client-supplied extensions with the reserved keys removed, then repopulates the reserved keys from
    // trusted sources only — guaranteeing a client cannot forge actor:agentsAdmin or provider:selectionValidation.
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

        extensions[AgentAdminExtensionKey] = "true";
        extensions[ProviderSelectionValidationExtensionKey] = verdict.ToString();
        return extensions;
    }
}
