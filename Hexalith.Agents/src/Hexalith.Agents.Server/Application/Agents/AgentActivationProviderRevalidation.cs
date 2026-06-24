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
/// Activation orchestration that re-validates all of the Agent's recorded dependency gates against their live state
/// before dispatching <c>ActivateAgent</c> (Story 1.5 AC2 "or activate"; Story 1.6 AC3; AD-3, AD-9, AD-12). It
/// re-reads the catalog for the recorded Provider/model and populates the trusted <c>provider:selectionValidation</c>
/// verdict, and — when the recorded Response Mode is Confirmation and a policy is present — resolves the recorded
/// Approver Policy sources through <see cref="IApproverPolicyResolver"/> and populates the trusted
/// <c>approver:policyValidation</c> verdict. Activation is one command / one envelope carrying all trusted dependency
/// verdicts, so the aggregate's gates can clear for a genuinely-ready agent and fail closed otherwise.
/// </summary>
/// <remarks>
/// Same trust model as the selection orchestration: <c>actor:agentsAdmin</c>, <c>provider:selectionValidation</c>,
/// and <c>approver:policyValidation</c> are server-populated only; client-supplied reserved keys are stripped and
/// repopulated from trusted sources. When no selection is recorded the provider verdict stays <c>Unknown</c>; in
/// Automatic mode or with no configured approver source the approver verdict stays <c>Unknown</c> (the aggregate's
/// gate ignores it for Automatic). The live reader / resolver / dispatcher bindings are deferred (mirroring Story
/// 1.2/1.4/1.5).
/// </remarks>
public sealed class AgentActivationProviderRevalidation
{
    private const string AgentDomain = "agent";

    /// <summary>The server-populated approver-policy verdict extension key (client-stripped).</summary>
    internal const string ApproverPolicyValidationExtensionKey = "approver:policyValidation";

    private static readonly string[] _reservedExtensionKeys =
    [
        AgentProviderSelectionOrchestrator.AgentAdminExtensionKey,
        AgentProviderSelectionOrchestrator.ProviderSelectionValidationExtensionKey,
        ApproverPolicyValidationExtensionKey,
    ];

    private readonly IProviderCatalogReader _catalogReader;
    private readonly IApproverPolicyResolver _approverPolicyResolver;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentActivationProviderRevalidation"/> class.</summary>
    /// <param name="catalogReader">The provider-catalog read port (live binding deferred).</param>
    /// <param name="approverPolicyResolver">The approver-policy resolution port (live binding deferred).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentActivationProviderRevalidation(
        IProviderCatalogReader catalogReader,
        IApproverPolicyResolver approverPolicyResolver,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(catalogReader);
        ArgumentNullException.ThrowIfNull(approverPolicyResolver);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _catalogReader = catalogReader;
        _approverPolicyResolver = approverPolicyResolver;
        _dispatcher = dispatcher;
    }

    /// <summary>Authorizes, re-validates the recorded dependency gates, and dispatches <c>ActivateAgent</c> with the verdicts (AC2; AC3).</summary>
    /// <param name="request">The sanitized activation request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The activation outcome (denied, or dispatched with the re-validated verdicts).</returns>
    public async Task<AgentActivationRevalidationOutcome> ExecuteAsync(AgentActivationRevalidationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Authorize — fail closed before any read or dispatch (AD-12).
        if (!request.IsAgentsAdmin)
        {
            return AgentActivationRevalidationOutcome.Denied();
        }

        // Re-read the catalog for the recorded selection (if any) and compute the fail-closed provider verdict. With
        // no recorded selection there is nothing to re-validate — the aggregate fails closed with MissingProviderSelection.
        ProviderSelectionValidationStatus providerVerdict = ProviderSelectionValidationStatus.Unknown;
        if (request.SelectedProviderId is not null && request.SelectedModelId is not null)
        {
            ProviderCatalogEntryReadResult read = await _catalogReader
                .GetEntryAsync(request.TenantId, request.SelectedProviderId, request.SelectedModelId, ct)
                .ConfigureAwait(false);
            providerVerdict = ProviderSelectionVerdict.Evaluate(read);
        }

        // Re-resolve the recorded Approver Policy only in Confirmation mode with at least one configured source.
        // Automatic mode needs no approver policy, and an empty Confirmation policy fails closed at the aggregate's
        // MissingApproverPolicy gate regardless of any verdict — so the verdict stays Unknown in those cases.
        ApproverPolicyValidationStatus approverVerdict = ApproverPolicyValidationStatus.Unknown;
        if (request.ResponseMode == AgentResponseMode.Confirmation
            && request.RecordedApproverPolicy is { Sources.Count: > 0 } policy)
        {
            ApproverPolicyResolutionResult resolution = await _approverPolicyResolver
                .ResolveAsync(request.TenantId, policy, ct)
                .ConfigureAwait(false);
            approverVerdict = ApproverPolicyVerdict.Evaluate(policy, resolution);
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
            BuildTrustedExtensions(request.ClientSuppliedExtensions, providerVerdict, approverVerdict));

        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);

        return AgentActivationRevalidationOutcome.FromDispatch(providerVerdict, approverVerdict);
    }

    private static Dictionary<string, string> BuildTrustedExtensions(
        IReadOnlyDictionary<string, string>? clientSupplied,
        ProviderSelectionValidationStatus providerVerdict,
        ApproverPolicyValidationStatus approverVerdict)
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
        extensions[AgentProviderSelectionOrchestrator.ProviderSelectionValidationExtensionKey] = providerVerdict.ToString();
        extensions[ApproverPolicyValidationExtensionKey] = approverVerdict.ToString();
        return extensions;
    }
}
