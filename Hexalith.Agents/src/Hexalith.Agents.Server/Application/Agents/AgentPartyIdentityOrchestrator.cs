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
/// Application orchestration for linking/replacing an Agent's (<c>hexa</c>) Party identity (AC1, AC2, AC3; AD-3,
/// AD-12). It authorizes the actor, resolves the Parties verdict through <see cref="IAgentPartyDirectory"/>
/// (validate-existing or provision-new), builds the aggregate command carrying only the stable <c>PartyId</c>, and
/// dispatches it with the <b>server-populated</b> trusted extensions <c>actor:agentsAdmin</c> and
/// <c>party:linkValidation</c>. The Parties side effect runs here, outside the pure aggregate, and its result
/// returns to the aggregate through the trusted command extension (the AD-3 round-trip).
/// </summary>
/// <remarks>
/// <b>Trust model (CRITICAL):</b> both reserved extension keys are server-populated only. Any client-supplied
/// value for them is stripped at this entry point and repopulated from the trusted authorization decision and the
/// port's verdict, so a client can never assert <c>party:linkValidation=Valid</c> to bypass Parties validation.
/// The verdict is always dispatched (even non-<c>Valid</c>) so the aggregate's independent rejection is auditable;
/// that aggregate-side rejection (not this orchestration) is the security guarantee against direct-gateway calls.
/// Binding <see cref="IAgentCommandDispatcher"/> to the live DAPR/EventStore gateway is deferred.
/// </remarks>
public sealed class AgentPartyIdentityOrchestrator
{
    private const string AgentDomain = "agent";

    /// <summary>The server-populated Agents-admin authorization extension key (client-stripped).</summary>
    internal const string AgentAdminExtensionKey = "actor:agentsAdmin";

    /// <summary>The server-populated Parties-validation verdict extension key (client-stripped).</summary>
    internal const string PartyLinkValidationExtensionKey = "party:linkValidation";

    private static readonly string[] _reservedExtensionKeys =
    [
        AgentAdminExtensionKey,
        PartyLinkValidationExtensionKey,
    ];

    private readonly IAgentPartyDirectory _directory;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentPartyIdentityOrchestrator"/> class.</summary>
    /// <param name="directory">The Parties validation/provisioning port.</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentPartyIdentityOrchestrator(IAgentPartyDirectory directory, IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _directory = directory;
        _dispatcher = dispatcher;
    }

    /// <summary>Authorizes, resolves the Parties verdict, and dispatches the link/replace command (AC1, AC2, AC3).</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (denied, or dispatched with the computed verdict).</returns>
    public async Task<AgentPartyLinkOutcome> ExecuteAsync(AgentPartyLinkRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // (1) Authorize — fail closed before any Parties call or dispatch (AD-12).
        if (!request.IsAgentsAdmin)
        {
            return AgentPartyLinkOutcome.Denied();
        }

        // (2) Resolve the verdict + stable Party id via the port (validate-existing or provision-new). The Parties
        // PII never crosses this boundary — only { Status, PartyId } return (AC1; AD-7).
        AgentPartyValidationResult validation = request.Source == AgentPartyLinkSource.ProvisionNewParty
            ? await _directory.ProvisionAgentPartyAsync(
                request.TenantId,
                new AgentPartyProvisioningRequest(request.AgentId, request.OrganizationLabel ?? DefaultOrganizationLabel(request.AgentId)),
                ct).ConfigureAwait(false)
            : await _directory.ValidateExistingPartyAsync(request.TenantId, RequireExistingPartyId(request), ct).ConfigureAwait(false);

        // The id carried on the command: the validated/provisioned id, else the requested id (so a non-Valid
        // verdict still produces an auditable, well-formed rejected command).
        string partyId = validation.PartyId ?? request.PartyId ?? string.Empty;

        // (3) Build the aggregate command (Link or Replace) — payload carries only the stable PartyId.
        object command = request.Operation == AgentPartyLinkOperation.Replace
            ? new ReplaceAgentPartyIdentity(partyId)
            : new LinkAgentPartyIdentity(partyId);

        // (4) Build the envelope with server-populated trusted extensions (client reserved keys stripped).
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            AgentDomain,
            request.AgentId,
            command.GetType().Name,
            JsonSerializer.SerializeToUtf8Bytes(command, command.GetType()),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions, validation.Status));

        // (5) Dispatch — always feed the computed verdict (even non-Valid) so the aggregate records an auditable
        // AgentPartyIdentityLinkRejected; the aggregate's independent non-Valid rejection is the security guarantee.
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);

        return AgentPartyLinkOutcome.FromDispatch(validation.Status, partyId);
    }

    private static string DefaultOrganizationLabel(string agentId) => $"Agent {agentId}";

    private static string RequireExistingPartyId(AgentPartyLinkRequest request)
        => !string.IsNullOrWhiteSpace(request.PartyId)
            ? request.PartyId
            : throw new ArgumentException("PartyId is required when linking an existing Party identity.", nameof(request));

    // Copies the client-supplied extensions with the reserved keys removed, then repopulates the reserved keys
    // from trusted sources only — guaranteeing a client cannot forge actor:agentsAdmin or party:linkValidation.
    private static Dictionary<string, string> BuildTrustedExtensions(
        IReadOnlyDictionary<string, string>? clientSupplied,
        PartyLinkValidationStatus verdict)
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
        extensions[PartyLinkValidationExtensionKey] = verdict.ToString();
        return extensions;
    }
}
