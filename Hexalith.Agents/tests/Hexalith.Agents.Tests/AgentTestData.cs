using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Shared fixtures for the Agent aggregate, state-replay, and inspection tests: a valid command envelope builder
/// (with the trusted Agents-admin extension), a valid create command, and helpers to pre-build and advance Agent
/// state through the production <c>Apply</c> handlers.
/// </summary>
internal static class AgentTestData
{
    internal const string AgentId = "hexa";
    internal const string TenantId = "acme";
    internal const string AgentAdminExtensionKey = "actor:agentsAdmin";
    internal const string PartyLinkValidationExtensionKey = "party:linkValidation";
    internal const string ProviderSelectionValidationExtensionKey = "provider:selectionValidation";
    internal const string LinkedPartyId = "party-001";
    internal const string SelectedProviderId = "openai";
    internal const string SelectedModelId = "gpt-4o";
    internal const int SelectedCapabilityVersion = 1;
    internal const string ValidInstructions = "You are hexa, a helpful and concise enterprise assistant.";

    internal static CommandEnvelope Envelope<T>(
        T command,
        bool isAgentsAdmin = true,
        string agentId = AgentId,
        string tenantId = TenantId,
        string actorUserId = "admin-user")
        where T : notnull
        => new(
            "msg-" + typeof(T).Name,
            tenantId,
            "agent",
            agentId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-1",
            null,
            actorUserId,
            isAgentsAdmin
                ? new Dictionary<string, string> { [AgentAdminExtensionKey] = "true" }
                : null);

    /// <summary>
    /// Builds a command envelope carrying the trusted Agents-admin extension and (optionally) the server-populated
    /// <c>party:linkValidation</c> verdict — the trust model the link/replace handlers read (Task 3/4). Set
    /// <paramref name="includeValidation"/> to <see langword="false"/> to simulate a direct-gateway call that never
    /// went through the orchestration (absent verdict → fails closed to <c>Unknown</c>).
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <param name="command">The command to serialize.</param>
    /// <param name="validation">The trusted Parties-validation verdict to populate.</param>
    /// <param name="isAgentsAdmin">Whether the trusted Agents-admin extension is present.</param>
    /// <param name="includeValidation">Whether the <c>party:linkValidation</c> extension is present at all.</param>
    /// <param name="agentId">The Agent aggregate id.</param>
    /// <param name="tenantId">The tenant scope.</param>
    /// <param name="actorUserId">The actor.</param>
    /// <returns>The command envelope.</returns>
    internal static CommandEnvelope LinkEnvelope<T>(
        T command,
        PartyLinkValidationStatus validation = PartyLinkValidationStatus.Valid,
        bool isAgentsAdmin = true,
        bool includeValidation = true,
        string agentId = AgentId,
        string tenantId = TenantId,
        string actorUserId = "admin-user")
        where T : notnull
    {
        var extensions = new Dictionary<string, string>();
        if (isAgentsAdmin)
        {
            extensions[AgentAdminExtensionKey] = "true";
        }

        if (includeValidation)
        {
            extensions[PartyLinkValidationExtensionKey] = validation.ToString();
        }

        return new(
            "msg-" + typeof(T).Name,
            tenantId,
            "agent",
            agentId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-1",
            null,
            actorUserId,
            extensions.Count > 0 ? extensions : null);
    }

    /// <summary>
    /// Builds a command envelope carrying the trusted Agents-admin extension and (optionally) the server-populated
    /// <c>provider:selectionValidation</c> verdict — the trust model the select handler and the activation provider
    /// gate read (Task 3/4). Use this to drive <see cref="SelectAgentProviderModel"/> and to activate an Agent whose
    /// recorded selection must be re-validated. Set <paramref name="includeValidation"/> to <see langword="false"/> to
    /// simulate a direct-gateway call that never went through the orchestration (absent verdict → fails closed to
    /// <c>Unknown</c>).
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <param name="command">The command to serialize.</param>
    /// <param name="validation">The trusted provider-readiness verdict to populate.</param>
    /// <param name="isAgentsAdmin">Whether the trusted Agents-admin extension is present.</param>
    /// <param name="includeValidation">Whether the <c>provider:selectionValidation</c> extension is present at all.</param>
    /// <param name="agentId">The Agent aggregate id.</param>
    /// <param name="tenantId">The tenant scope.</param>
    /// <param name="actorUserId">The actor.</param>
    /// <returns>The command envelope.</returns>
    internal static CommandEnvelope SelectEnvelope<T>(
        T command,
        ProviderSelectionValidationStatus validation = ProviderSelectionValidationStatus.Valid,
        bool isAgentsAdmin = true,
        bool includeValidation = true,
        string agentId = AgentId,
        string tenantId = TenantId,
        string actorUserId = "admin-user")
        where T : notnull
    {
        var extensions = new Dictionary<string, string>();
        if (isAgentsAdmin)
        {
            extensions[AgentAdminExtensionKey] = "true";
        }

        if (includeValidation)
        {
            extensions[ProviderSelectionValidationExtensionKey] = validation.ToString();
        }

        return new(
            "msg-" + typeof(T).Name,
            tenantId,
            "agent",
            agentId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-1",
            null,
            actorUserId,
            extensions.Count > 0 ? extensions : null);
    }

    internal static CreateAgent ValidCreate(
        string tenantId = TenantId,
        string displayName = "Hexa Assistant",
        string? description = "Tenant governed assistant",
        string instructions = ValidInstructions)
        => new(tenantId, displayName, description, instructions);

    internal static AgentCreated CreatedEvent(CreateAgent create, string agentId = AgentId)
        => new(
            agentId,
            create.TenantId,
            create.DisplayName ?? string.Empty,
            string.IsNullOrWhiteSpace(create.Description) ? null : create.Description,
            create.Instructions ?? string.Empty,
            ConfigurationVersion: 1,
            string.IsNullOrWhiteSpace(create.Instructions) ? 0 : 1);

    /// <summary>Builds Agent state by applying the creation event for the given create command.</summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <returns>The rehydrated Agent state.</returns>
    internal static AgentState StateWith(CreateAgent create)
    {
        var state = new AgentState();
        state.Apply(CreatedEvent(create));
        return state;
    }

    /// <summary>Builds a created-then-activated Agent state (lifecycle = Active).</summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <returns>The active Agent state.</returns>
    internal static AgentState ActiveStateWith(CreateAgent create)
    {
        AgentState state = StateWith(create);
        state.Apply(new AgentActivated(AgentId));
        return state;
    }

    /// <summary>Builds a created-then-disabled Agent state (lifecycle = Disabled).</summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <returns>The disabled Agent state.</returns>
    internal static AgentState DisabledStateWith(CreateAgent create)
    {
        AgentState state = StateWith(create);
        state.Apply(new AgentDisabled(AgentId));
        return state;
    }

    /// <summary>Builds a created state with a single linked Party identity (lifecycle still Draft).</summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <param name="partyId">The Party id to link.</param>
    /// <returns>The Agent state with a linked Party identity.</returns>
    internal static AgentState StateWithLinkedParty(CreateAgent create, string partyId = LinkedPartyId)
    {
        AgentState state = StateWith(create);
        state.Apply(new AgentPartyIdentityLinked(AgentId, partyId, state.ConfigurationVersion + 1));
        return state;
    }

    /// <summary>
    /// Builds a created state with a linked Party identity AND a recorded Provider/model selection (lifecycle still
    /// Draft) — a fully readiness-cleared Agent whose only remaining gate is the live provider verdict at activation.
    /// </summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <param name="providerId">The provider id to select.</param>
    /// <param name="modelId">The model id to select.</param>
    /// <param name="capabilityVersion">The captured provider capability version.</param>
    /// <returns>The Agent state with a linked Party identity and a recorded provider selection.</returns>
    internal static AgentState StateWithSelectedProvider(
        CreateAgent create,
        string providerId = SelectedProviderId,
        string modelId = SelectedModelId,
        int capabilityVersion = SelectedCapabilityVersion)
    {
        AgentState state = StateWithLinkedParty(create);
        state.Apply(new AgentProviderModelSelected(AgentId, providerId, modelId, capabilityVersion, state.ConfigurationVersion + 1));
        return state;
    }

    /// <summary>
    /// Applies every event of a <see cref="DomainResult"/> to the supplied state through the aggregate's typed
    /// <c>Apply</c> methods — the same production replay handlers the EventStore state-store invokes. Success events
    /// advance state; rejection events are replay-safe no-ops (they must not mutate state).
    /// </summary>
    /// <param name="state">The Agent state to advance in place.</param>
    /// <param name="result">The domain result whose events are applied in order.</param>
    internal static void ApplyAll(AgentState state, DomainResult result)
    {
        foreach (IEventPayload payload in result.Events)
        {
            switch (payload)
            {
                case AgentCreated e: state.Apply(e); break;
                case AgentConfigurationUpdated e: state.Apply(e); break;
                case AgentActivated e: state.Apply(e); break;
                case AgentDisabled e: state.Apply(e); break;
                case AgentPartyIdentityLinked e: state.Apply(e); break;
                case AgentPartyIdentityReplaced e: state.Apply(e); break;
                case AgentProviderModelSelected e: state.Apply(e); break;
                case AgentProviderModelSelectionRejected e: state.Apply(e); break;
                case AgentAdministrationDeniedRejection e: state.Apply(e); break;
                case AgentNotFoundRejection e: state.Apply(e); break;
                case AgentAlreadyExistsRejection e: state.Apply(e); break;
                case AgentActivationBlockedRejection e: state.Apply(e); break;
                case AgentLifecycleStateAlreadySetRejection e: state.Apply(e); break;
                case InvalidAgentConfigurationRejection e: state.Apply(e); break;
                case AgentPartyIdentityLinkRejected e: state.Apply(e); break;
                case AgentPartyIdentityAlreadyLinkedRejection e: state.Apply(e); break;
                default: throw new InvalidOperationException($"Unhandled event type '{payload.GetType().Name}' in test apply dispatch.");
            }
        }
    }

    /// <summary>
    /// Drives one command end-to-end through the real aggregate pipeline — JSON-serialized command envelope →
    /// reflection dispatch in <see cref="AgentAggregate.ProcessAsync"/> → typed handler → events — then applies the
    /// resulting events to <paramref name="state"/> so the next command sees the evolved state.
    /// </summary>
    /// <typeparam name="TCommand">The command type (drives the dispatch lookup and payload round-trip).</typeparam>
    /// <param name="aggregate">The aggregate under test.</param>
    /// <param name="state">The threaded Agent state, advanced in place.</param>
    /// <param name="command">The command to process.</param>
    /// <param name="isAgentsAdmin">Whether the trusted Agents-admin extension is present (AC4).</param>
    /// <returns>The domain result of processing the command.</returns>
    internal static async Task<DomainResult> ProcessAndApplyAsync<TCommand>(
        AgentAggregate aggregate,
        AgentState state,
        TCommand command,
        bool isAgentsAdmin = true)
        where TCommand : notnull
    {
        DomainResult result = await aggregate.ProcessAsync(Envelope(command, isAgentsAdmin), state);
        ApplyAll(state, result);
        return result;
    }

    /// <summary>
    /// Drives one command through the real aggregate pipeline using an explicitly-built envelope — used for the
    /// party link/replace journey, whose handlers read the trusted <c>party:linkValidation</c> verdict from the
    /// envelope (so the default admin-only <see cref="Envelope{T}"/> would fail closed).
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="aggregate">The aggregate under test.</param>
    /// <param name="state">The threaded Agent state, advanced in place.</param>
    /// <param name="command">The command to process.</param>
    /// <param name="envelope">The explicit command envelope (e.g. from <see cref="LinkEnvelope{T}"/>).</param>
    /// <returns>The domain result of processing the command.</returns>
    internal static async Task<DomainResult> ProcessAndApplyAsync<TCommand>(
        AgentAggregate aggregate,
        AgentState state,
        TCommand command,
        CommandEnvelope envelope)
        where TCommand : notnull
    {
        DomainResult result = await aggregate.ProcessAsync(envelope, state);
        ApplyAll(state, result);
        return result;
    }
}
