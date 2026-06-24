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
/// Application orchestration for defining an Agent's Content Safety Policy (Story 1.7 AC1; AD-3, AD-12). It authorizes
/// the actor, builds the <see cref="ConfigureAgentContentSafetyPolicy"/> command carrying only the safe configuration
/// value, and dispatches it with the <b>server-populated</b> trusted <c>actor:agentsAdmin</c> extension. Content-safety
/// configuration is self-contained Agent state, so — like the response-mode and approver-policy orchestrations — there
/// is <em>no</em> dependency verdict to compute or attach (AD-3).
/// </summary>
/// <remarks>
/// <para>
/// <b>Trust model (CRITICAL):</b> the reserved extension keys (<c>actor:agentsAdmin</c>, and the activation-path
/// verdict keys <c>provider:selectionValidation</c> / <c>approver:policyValidation</c>) are server-populated only. Any
/// client-supplied value for them is stripped at this entry point; only <c>actor:agentsAdmin</c> is repopulated from
/// the trusted authorization decision, so a client can neither forge admin nor smuggle an activation verdict onto a
/// configuration command. No new reserved key is introduced — content-safety configuration carries no verdict. Binding
/// <see cref="IAgentCommandDispatcher"/> to the live command gateway is deferred (mirroring Story 1.4/1.5/1.6).
/// </para>
/// <para>
/// <b>Authorization ("release operator", AC1/AC2):</b> AC1/AC2 name "an authorized administrator <em>or release
/// operator</em>." V1 gates this through the same transitional, server-populated <c>actor:agentsAdmin</c> extension
/// every other config command uses (the aggregate's XML-doc states the authorization model is transitional). The
/// production authorization model will additionally recognize a distinct <b>release-operator</b> role authorized to
/// define Content Safety Policy and perform launch validation; folding it under the single fail-closed admin gate for
/// V1 is deliberate, and the release-operator-only principal arrives with the real authz model / FR-28 launch-readiness
/// work (Epic 4). No second transitional extension key is introduced here.
/// </para>
/// </remarks>
public sealed class AgentContentSafetyPolicyOrchestrator
{
    private const string AgentDomain = "agent";

    private static readonly string[] _reservedExtensionKeys =
    [
        AgentProviderSelectionOrchestrator.AgentAdminExtensionKey,
        AgentProviderSelectionOrchestrator.ProviderSelectionValidationExtensionKey,
        AgentActivationProviderRevalidation.ApproverPolicyValidationExtensionKey,
    ];

    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentContentSafetyPolicyOrchestrator"/> class.</summary>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentContentSafetyPolicyOrchestrator(IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
    }

    /// <summary>Authorizes and dispatches the content-safety configuration command (AC1).</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (denied, or dispatched).</returns>
    public async Task<AgentContentSafetyPolicyOutcome> ExecuteAsync(AgentContentSafetyPolicyRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Authorize — fail closed before any dispatch (AD-12).
        if (!request.IsAgentsAdmin)
        {
            return AgentContentSafetyPolicyOutcome.Denied();
        }

        var command = new ConfigureAgentContentSafetyPolicy(request.Configuration);
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            AgentDomain,
            request.AgentId,
            nameof(ConfigureAgentContentSafetyPolicy),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);

        return AgentContentSafetyPolicyOutcome.FromDispatch();
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
