using System;
using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Agent;

/// <summary>
/// Pure, replay-safe aggregate for the tenant-scoped governed Agent <c>hexa</c> (AD-2, AD-3). It owns identity
/// metadata, Agent Instructions, lifecycle, and configuration version — a distinct boundary from
/// <c>ProviderCatalog</c> and the future <c>AgentInteraction</c>. Static
/// <c>Handle(command, state, envelope) -&gt; DomainResult</c> methods (discovered by the EventStore client by
/// convention) decide success events, typed rejection events, or a deterministic no-op. The aggregate performs no
/// I/O, no provider/secret-store/Dapr access, and no wall-clock reads; actor identity comes from the envelope.
/// </summary>
/// <remarks>
/// <para>
/// <b>Authorization is transitional (AD-12):</b> until the full Agents authorization story lands, administration
/// is gated by a trusted, server-populated command-envelope extension (<c>actor:agentsAdmin</c>), patterned after
/// the Tenants <c>actor:globalAdmin</c> and Story 1.2's <c>actor:agentsProviderAdmin</c> extensions. Client-
/// provided reserved extensions must be stripped by the command entry point and never trusted here. Replace this
/// gate when the Agents authorization model exists.
/// </para>
/// <para>
/// <b>Activation is partial in V1:</b> the activation gate enforces only this story's required fields (display
/// name present; Agent Instructions present and valid). Later stories (1.4–1.7) add party-identity, provider/
/// model, response/approver, and content-safety gates onto this same aggregate by appending activation blockers
/// — never by reshaping these events.
/// </para>
/// <para>
/// <b>Sensitive content (AD-14):</b> the Agent Instructions text lives only on the create/update success events
/// and durable state. It never appears on a rejection (including activation-blocked and administration-denied),
/// the status view, logs, telemetry, or audit summaries. Content-bearing Agents events must adopt the EventStore
/// payload-protection/redaction conventions before production use (tracked by AD-14); this story keeps
/// instructions in plaintext within the durable event for local/dev only and invents no bespoke encryption here.
/// </para>
/// </remarks>
[EventStoreDomain("agent")]
public class AgentAggregate : EventStoreAggregate<AgentState>
{
    // SECURITY: server-populated only (patterned after Tenants' "actor:globalAdmin" and Story 1.2's
    // "actor:agentsProviderAdmin"). The command entry point strips client-provided reserved extensions and
    // repopulates this key from trusted claims only.
    private const string AgentAdminExtensionKey = "actor:agentsAdmin";

    // SECURITY: server-populated only, identical trust model to AgentAdminExtensionKey. The Parties
    // validation/provisioning runs in the Server orchestration (AD-3) and its verdict is fed back here through
    // this trusted extension. The command entry point strips any client-supplied value and repopulates it from
    // the orchestration's port result. A direct client command carries no trusted verdict, so it parses to
    // Unknown and is rejected — the aggregate's independent non-Valid rejection is the security guarantee (AC2).
    private const string PartyLinkValidationExtensionKey = "party:linkValidation";

    /// <summary>Handles creation (or idempotent re-creation) of the governed Agent record.</summary>
    /// <param name="command">The create command.</param>
    /// <param name="state">The current Agent state (null/never-created before the first creation).</param>
    /// <param name="envelope">The command envelope (carries the Agent id and the trusted authorization extension).</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(CreateAgent command, AgentState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string agentId = envelope.AggregateId;

        if (!IsAgentAdmin(envelope))
        {
            return Denied(agentId, envelope, nameof(CreateAgent));
        }

        string tenantId = command.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return Invalid(agentId, "TenantId is required.");
        }

        string displayName = NormalizeRequiredText(command.DisplayName);
        string? description = NormalizeOptionalText(command.Description);
        string instructions = NormalizeRequiredText(command.Instructions);

        string? reason = AgentConfigurationPolicy.ValidateStorableInput(displayName, description, instructions);
        if (reason is not null)
        {
            return Invalid(agentId, reason);
        }

        if (state is { IsCreated: true })
        {
            // Exact-duplicate re-create is a deterministic no-op; a conflicting payload is rejected and never
            // mutates state silently.
            return CreateMatchesExisting(state, tenantId, displayName, description, instructions)
                ? DomainResult.NoOp()
                : DomainResult.Rejection([new AgentAlreadyExistsRejection(agentId)]);
        }

        int instructionsVersion = AgentConfigurationPolicy.HasInstructions(instructions) ? 1 : 0;
        return DomainResult.Success([
            new AgentCreated(
                agentId,
                tenantId,
                displayName,
                description,
                instructions,
                ConfigurationVersion: 1,
                instructionsVersion),
        ]);
    }

    /// <summary>Handles a safe-metadata/instructions update of an existing Agent.</summary>
    /// <param name="command">The update command.</param>
    /// <param name="state">The current Agent state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(UpdateAgentConfiguration command, AgentState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string agentId = envelope.AggregateId;

        if (!IsAgentAdmin(envelope))
        {
            return Denied(agentId, envelope, nameof(UpdateAgentConfiguration));
        }

        if (state is null || !state.IsCreated)
        {
            return DomainResult.Rejection([new AgentNotFoundRejection(agentId)]);
        }

        string displayName = NormalizeRequiredText(command.DisplayName);
        string? description = NormalizeOptionalText(command.Description);
        string instructions = NormalizeRequiredText(command.Instructions);

        string? reason = AgentConfigurationPolicy.ValidateStorableInput(displayName, description, instructions);
        if (reason is not null)
        {
            return Invalid(agentId, reason);
        }

        bool displayNameChanged = !string.Equals(state.DisplayName, displayName, StringComparison.Ordinal);
        bool descriptionChanged = !string.Equals(state.Description, description, StringComparison.Ordinal);
        bool instructionsChanged = !string.Equals(state.Instructions, instructions, StringComparison.Ordinal);

        // An update that changes nothing is a deterministic no-op.
        if (!displayNameChanged && !descriptionChanged && !instructionsChanged)
        {
            return DomainResult.NoOp();
        }

        int instructionsVersion = instructionsChanged ? state.InstructionsVersion + 1 : state.InstructionsVersion;
        return DomainResult.Success([
            new AgentConfigurationUpdated(
                agentId,
                displayName,
                description,
                instructions,
                instructionsChanged,
                ConfigurationVersion: state.ConfigurationVersion + 1,
                instructionsVersion),
        ]);
    }

    /// <summary>Handles an activation request, re-evaluating this story's activation gates (AC2).</summary>
    /// <param name="command">The activate command.</param>
    /// <param name="state">The current Agent state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(ActivateAgent command, AgentState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string agentId = envelope.AggregateId;

        if (!IsAgentAdmin(envelope))
        {
            return Denied(agentId, envelope, nameof(ActivateAgent));
        }

        if (state is null || !state.IsCreated)
        {
            return DomainResult.Rejection([new AgentNotFoundRejection(agentId)]);
        }

        if (state.Lifecycle == AgentLifecycleStatus.Active)
        {
            return DomainResult.Rejection([
                new AgentLifecycleStateAlreadySetRejection(
                    agentId,
                    AgentLifecycleStatus.Active,
                    AgentLifecycleStatus.Active,
                    nameof(ActivateAgent)),
            ]);
        }

        // Reactivating a Disabled agent re-runs the same gates (AC2): activation is never a blind flip to active.
        // Party identity is a distinct readiness gate (1.4 AC4) — activation fails closed until a valid Party is linked.
        IReadOnlyList<AgentActivationBlocker> blockers =
            AgentConfigurationPolicy.ComputeActivationBlockers(
                state.DisplayName,
                state.Instructions,
                state.PartyId is not null);
        return blockers.Count > 0
            ? DomainResult.Rejection([new AgentActivationBlockedRejection(agentId, blockers)])
            : DomainResult.Success([new AgentActivated(agentId)]);
    }

    /// <summary>Handles disabling an existing Agent (lifecycle flag flip only; history preserved, AC3).</summary>
    /// <param name="command">The disable command.</param>
    /// <param name="state">The current Agent state.</param>
    /// <param name="envelope">The command envelope.</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(DisableAgent command, AgentState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string agentId = envelope.AggregateId;

        if (!IsAgentAdmin(envelope))
        {
            return Denied(agentId, envelope, nameof(DisableAgent));
        }

        if (state is null || !state.IsCreated)
        {
            return DomainResult.Rejection([new AgentNotFoundRejection(agentId)]);
        }

        return state.Lifecycle == AgentLifecycleStatus.Disabled
            ? DomainResult.Rejection([
                new AgentLifecycleStateAlreadySetRejection(
                    agentId,
                    AgentLifecycleStatus.Disabled,
                    AgentLifecycleStatus.Disabled,
                    nameof(DisableAgent))])
            : DomainResult.Success([new AgentDisabled(agentId)]);
    }

    /// <summary>Handles linking the Agent's single active Party identity (AC1, AC2, AC3; FR-2).</summary>
    /// <param name="command">The link command (carries only the stable <c>PartyId</c>).</param>
    /// <param name="state">The current Agent state.</param>
    /// <param name="envelope">The command envelope (carries the trusted authorization and Parties-validation extensions).</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(LinkAgentPartyIdentity command, AgentState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string agentId = envelope.AggregateId;

        if (!IsAgentAdmin(envelope))
        {
            return Denied(agentId, envelope, nameof(LinkAgentPartyIdentity));
        }

        if (state is null || !state.IsCreated)
        {
            return DomainResult.Rejection([new AgentNotFoundRejection(agentId)]);
        }

        // AC2 fail-closed: anything other than a trusted Valid verdict (missing/disabled/ambiguous/unavailable/
        // unauthorized — or an absent/unparseable verdict from a direct-gateway call) rejects the link and changes
        // no state. The Parties dependency is never called from here (AD-3); the verdict is plain trusted data.
        PartyLinkValidationStatus validation = ReadPartyLinkValidation(envelope);
        if (validation != PartyLinkValidationStatus.Valid)
        {
            return DomainResult.Rejection([new AgentPartyIdentityLinkRejected(agentId, validation)]);
        }

        // Re-asserting the same id is a deterministic no-op (AD-13); a different id while one is already linked is
        // rejected — changing identity requires the explicit ReplaceAgentPartyIdentity (AC3).
        if (string.Equals(state.PartyId, command.PartyId, StringComparison.Ordinal))
        {
            return DomainResult.NoOp();
        }

        if (state.PartyId is not null)
        {
            return DomainResult.Rejection([new AgentPartyIdentityAlreadyLinkedRejection(agentId, command.PartyId)]);
        }

        // Linking is a configuration change → bump ConfigurationVersion (AD-4 snapshot). Lifecycle is unchanged:
        // a linked Party clears MissingPartyIdentity but does not auto-activate (Story 1.3 lifecycle invariant).
        return DomainResult.Success([
            new AgentPartyIdentityLinked(agentId, command.PartyId, state.ConfigurationVersion + 1),
        ]);
    }

    /// <summary>Handles explicitly replacing the Agent's linked Party identity with a different one (AC2, AC3; FR-2).</summary>
    /// <param name="command">The replace command (carries only the stable <c>PartyId</c>).</param>
    /// <param name="state">The current Agent state.</param>
    /// <param name="envelope">The command envelope (carries the trusted authorization and Parties-validation extensions).</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(ReplaceAgentPartyIdentity command, AgentState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string agentId = envelope.AggregateId;

        if (!IsAgentAdmin(envelope))
        {
            return Denied(agentId, envelope, nameof(ReplaceAgentPartyIdentity));
        }

        if (state is null || !state.IsCreated)
        {
            return DomainResult.Rejection([new AgentNotFoundRejection(agentId)]);
        }

        // AC2 fail-closed: the same trusted-verdict gate applies to a replacement.
        PartyLinkValidationStatus validation = ReadPartyLinkValidation(envelope);
        if (validation != PartyLinkValidationStatus.Valid)
        {
            return DomainResult.Rejection([new AgentPartyIdentityLinkRejected(agentId, validation)]);
        }

        // Re-asserting the already-linked id is a deterministic no-op (AD-13).
        if (string.Equals(state.PartyId, command.PartyId, StringComparison.Ordinal))
        {
            return DomainResult.NoOp();
        }

        // Replace deterministically sets the single active identity, so there is always at most one PartyId (AC3).
        return DomainResult.Success([
            new AgentPartyIdentityReplaced(agentId, state.PartyId, command.PartyId, state.ConfigurationVersion + 1),
        ]);
    }

    private static bool IsAgentAdmin(CommandEnvelope envelope)
        => envelope.Extensions?.TryGetValue(AgentAdminExtensionKey, out string? value) == true
            && string.Equals(value, "true", StringComparison.Ordinal);

    // Reads the trusted, server-populated Parties-validation verdict from the envelope extension. Fails closed to
    // Unknown when the key is absent, or when its value is not an exact, case-sensitive PartyLinkValidationStatus
    // *name* (AC2). Numeric/aliased forms (e.g. "1", which Enum.TryParse would otherwise resolve to Valid) are
    // rejected on purpose: the verdict is contractually serialized by name ([JsonStringEnumConverter]), and this
    // gate is kept as strict as the "true"-only Agents-admin check so a surprising value can never become Valid.
    private static PartyLinkValidationStatus ReadPartyLinkValidation(CommandEnvelope envelope)
        => envelope.Extensions?.TryGetValue(PartyLinkValidationExtensionKey, out string? value) == true
            && Enum.TryParse(value, ignoreCase: false, out PartyLinkValidationStatus status)
            && string.Equals(Enum.GetName(status), value, StringComparison.Ordinal)
                ? status
                : PartyLinkValidationStatus.Unknown;

    private static DomainResult Denied(string agentId, CommandEnvelope envelope, string commandName)
        => DomainResult.Rejection([new AgentAdministrationDeniedRejection(agentId, envelope.UserId, commandName)]);

    private static DomainResult Invalid(string agentId, string reason)
        => DomainResult.Rejection([new InvalidAgentConfigurationRejection(agentId, reason)]);

    private static bool CreateMatchesExisting(
        AgentState existing,
        string tenantId,
        string displayName,
        string? description,
        string instructions)
        => string.Equals(existing.TenantId, tenantId, StringComparison.Ordinal)
            && string.Equals(existing.DisplayName, displayName, StringComparison.Ordinal)
            && string.Equals(existing.Description, description, StringComparison.Ordinal)
            && string.Equals(existing.Instructions, instructions, StringComparison.Ordinal);

    private static string NormalizeRequiredText(string? value) => value ?? string.Empty;

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
