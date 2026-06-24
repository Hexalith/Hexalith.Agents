using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Application orchestration for configuring an Agent's Approver Policy (Story 1.6 AC2, AC4; AD-3, AD-12). It
/// authorizes the actor, builds the <see cref="ConfigureAgentApproverPolicy"/> command carrying only the safe policy
/// value, and dispatches it with the <b>server-populated</b> trusted <c>actor:agentsAdmin</c> extension. Storing the
/// policy never resolves its sources (structural validation is in the aggregate; source resolution is the activation
/// concern), so there is no dependency verdict here (AD-3).
/// </summary>
/// <remarks>
/// <b>Trust model (CRITICAL):</b> the reserved extension keys (<c>actor:agentsAdmin</c>, and the activation-path
/// verdict keys) are server-populated only. Any client-supplied value for them is stripped at this entry point; only
/// <c>actor:agentsAdmin</c> is repopulated from the trusted authorization decision. Binding
/// <see cref="IAgentCommandDispatcher"/> to the live command gateway is deferred (mirroring Story 1.4/1.5).
/// </remarks>
public sealed class AgentApproverPolicyOrchestrator
{
    private const string AgentDomain = "agent";

    private static readonly string[] _reservedExtensionKeys =
    [
        AgentProviderSelectionOrchestrator.AgentAdminExtensionKey,
        AgentProviderSelectionOrchestrator.ProviderSelectionValidationExtensionKey,
        AgentActivationProviderRevalidation.ApproverPolicyValidationExtensionKey,
    ];

    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentApproverPolicyOrchestrator"/> class.</summary>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentApproverPolicyOrchestrator(IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
    }

    /// <summary>Authorizes and dispatches the approver-policy configuration command (AC2, AC4).</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (denied, or dispatched).</returns>
    public async Task<AgentApproverPolicyOutcome> ExecuteAsync(AgentApproverPolicyRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Authorize — fail closed before any dispatch (AD-12).
        if (!request.IsAgentsAdmin)
        {
            return AgentApproverPolicyOutcome.Denied();
        }

        var command = new ConfigureAgentApproverPolicy(request.Policy);
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            AgentDomain,
            request.AgentId,
            nameof(ConfigureAgentApproverPolicy),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);

        return AgentApproverPolicyOutcome.FromDispatch();
    }

    // Copies the client-supplied extensions with the reserved keys removed, then repopulates only the trusted
    // actor:agentsAdmin key — a config command carries no dependency verdict, so no verdict key is repopulated.
    private static Dictionary<string, string> BuildTrustedExtensions(IReadOnlyDictionary<string, string>? clientSupplied)
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
        return extensions;
    }
}
