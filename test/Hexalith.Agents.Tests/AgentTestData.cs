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
    internal const string ApproverPolicyValidationExtensionKey = "approver:policyValidation";
    internal const string AuditGovernanceResolvedExtensionKey = "audit:governanceResolved";
    internal const string LinkedPartyId = "party-001";
    internal const string SelectedProviderId = "openai";
    internal const string SelectedModelId = "gpt-4o";
    internal const int SelectedCapabilityVersion = 1;
    internal const string ApproverPartyId = "party-approver";
    internal const string ApproverTenantRole = "tenant-approver";
    internal const string ValidInstructions = "You are hexa, a helpful and concise enterprise assistant.";

    /// <summary>A valid sample Approver Policy (caller + a predefined Party + a tenant role) with operator-only disclosure.</summary>
    internal static AgentApproverPolicy SampleApproverPolicy { get; } = new(
        [
            new ApproverPolicySource(ApproverPolicySourceKind.Caller, null, null),
            new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, ApproverPartyId, null),
            new ApproverPolicySource(ApproverPolicySourceKind.TenantRole, null, ApproverTenantRole),
        ],
        ApproverPolicyBasisDisclosure.OperatorOnly);

    /// <summary>A valid sample Content Safety Policy (one prompt constraint + one blocked category; block-and-audit, metadata-only).</summary>
    internal static AgentContentSafetyPolicy SampleContentSafetyPolicy { get; } = new(
        ["No system-prompt disclosure"],
        ["self-harm"],
        [],
        ContentSafetyFailureHandling.BlockAndAudit,
        ContentSafetyAuditTreatment.MetadataOnly);

    /// <summary>A valid sample Content Safety configuration (active policy only, no stricter mode-specific overrides).</summary>
    internal static AgentContentSafetyConfiguration SampleContentSafetyConfiguration { get; } = new(
        SampleContentSafetyPolicy,
        null,
        null);

    /// <summary>A valid sample launch-readiness decision: a primary + counter metric, both per-mode latency targets, a budgets cost posture, and the V1 context-policy reference (Story 4.4).</summary>
    internal static AgentLaunchReadiness SampleLaunchReadiness { get; } = new(
        [
            new LaunchMetricDefinition("SM-2", LaunchMetricClassification.Primary, "Launch Conversations using >=1 Agent Call", "Eligible launch Conversations", 0.30m, "First 14 days post-enablement", "Launch cohort A"),
            new LaunchMetricDefinition("SM-C1", LaunchMetricClassification.Counter, "Safety-blocked Agent Calls", "Total Agent Calls", 0.05m, "Rolling 7 days", "All launch tenants"),
        ],
        [
            new ResponseModeLatencyTarget(AgentResponseMode.Automatic, 4000),
            new ResponseModeLatencyTarget(AgentResponseMode.Confirmation, 8000),
        ],
        CostControlPosture.Budgets,
        null,
        "full-conversation-v1");

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

    /// <summary>
    /// Builds an <see cref="ActivateAgent"/> command envelope carrying the trusted Agents-admin extension plus the
    /// server-populated <c>provider:selectionValidation</c> and <c>approver:policyValidation</c> verdicts — the trust
    /// model the activation gate reads (Task 3/4). Use this to activate a Confirmation-mode Agent whose recorded
    /// approver policy must re-resolve. Set <paramref name="includeApproverValidation"/>/<paramref name="includeProviderValidation"/>
    /// to <see langword="false"/> to simulate a direct-gateway activation that never went through the orchestration
    /// (absent verdict → fails closed to <c>Unknown</c>).
    /// </summary>
    /// <param name="providerValidation">The trusted provider-readiness verdict to populate.</param>
    /// <param name="approverValidation">The trusted approver-policy verdict to populate.</param>
    /// <param name="isAgentsAdmin">Whether the trusted Agents-admin extension is present.</param>
    /// <param name="includeProviderValidation">Whether the <c>provider:selectionValidation</c> extension is present at all.</param>
    /// <param name="includeApproverValidation">Whether the <c>approver:policyValidation</c> extension is present at all.</param>
    /// <param name="agentId">The Agent aggregate id.</param>
    /// <param name="tenantId">The tenant scope.</param>
    /// <param name="actorUserId">The actor.</param>
    /// <returns>The command envelope.</returns>
    internal static CommandEnvelope ActivateEnvelope(
        ProviderSelectionValidationStatus providerValidation = ProviderSelectionValidationStatus.Valid,
        ApproverPolicyValidationStatus approverValidation = ApproverPolicyValidationStatus.Valid,
        bool isAgentsAdmin = true,
        bool includeProviderValidation = true,
        bool includeApproverValidation = true,
        string agentId = AgentId,
        string tenantId = TenantId,
        string actorUserId = "admin-user")
    {
        var command = new ActivateAgent();
        var extensions = new Dictionary<string, string>();
        if (isAgentsAdmin)
        {
            extensions[AgentAdminExtensionKey] = "true";
        }

        if (includeProviderValidation)
        {
            extensions[ProviderSelectionValidationExtensionKey] = providerValidation.ToString();
        }

        if (includeApproverValidation)
        {
            extensions[ApproverPolicyValidationExtensionKey] = approverValidation.ToString();
        }

        return new(
            "msg-ActivateAgent",
            tenantId,
            "agent",
            agentId,
            nameof(ActivateAgent),
            JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-1",
            null,
            actorUserId,
            extensions.Count > 0 ? extensions : null);
    }

    /// <summary>
    /// Builds an <see cref="EnableProductionLikeGeneration"/> command envelope carrying the trusted Agents-admin
    /// extension plus the server-populated <c>audit:governanceResolved</c> flag — the trust model the launch-readiness
    /// gate reads (Story 4.4). Set <paramref name="includeAuditGovernance"/> to <see langword="false"/> to simulate a
    /// direct-gateway enablement that never resolved audit governance (absent flag → fails closed to false).
    /// </summary>
    /// <param name="auditGovernanceResolved">The trusted audit-governance-resolved flag to populate.</param>
    /// <param name="isAgentsAdmin">Whether the trusted Agents-admin extension is present.</param>
    /// <param name="includeAuditGovernance">Whether the <c>audit:governanceResolved</c> extension is present at all.</param>
    /// <param name="agentId">The Agent aggregate id.</param>
    /// <param name="tenantId">The tenant scope.</param>
    /// <param name="actorUserId">The actor.</param>
    /// <returns>The command envelope.</returns>
    internal static CommandEnvelope EnableEnvelope(
        bool auditGovernanceResolved = true,
        bool isAgentsAdmin = true,
        bool includeAuditGovernance = true,
        string agentId = AgentId,
        string tenantId = TenantId,
        string actorUserId = "admin-user")
    {
        var command = new EnableProductionLikeGeneration();
        var extensions = new Dictionary<string, string>();
        if (isAgentsAdmin)
        {
            extensions[AgentAdminExtensionKey] = "true";
        }

        if (includeAuditGovernance)
        {
            extensions[AuditGovernanceResolvedExtensionKey] = auditGovernanceResolved ? "true" : "false";
        }

        return new(
            "msg-EnableProductionLikeGeneration",
            tenantId,
            "agent",
            agentId,
            nameof(EnableProductionLikeGeneration),
            JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-1",
            null,
            actorUserId,
            extensions.Count > 0 ? extensions : null);
    }

    /// <summary>Builds a created state with a recorded launch-readiness decision applied (lifecycle still Draft).</summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <param name="readiness">The launch-readiness decision to record.</param>
    /// <returns>The Agent state with the recorded launch readiness (launch-readiness version = 1).</returns>
    internal static AgentState StateWithLaunchReadiness(CreateAgent create, AgentLaunchReadiness? readiness = null)
    {
        AgentState state = StateWith(create);
        state.Apply(new AgentLaunchReadinessRecorded(AgentId, readiness ?? SampleLaunchReadiness, 1, state.ConfigurationVersion + 1));
        return state;
    }

    /// <summary>
    /// Builds a fully launch-ready Agent state (lifecycle still Draft): a recorded Content Safety Policy AND a recorded
    /// launch-readiness decision (which carries the in-force context-policy reference, both per-mode latency targets, the
    /// launch metrics, and the cost posture). The only remaining launch-readiness gate is the trusted audit-governance
    /// flag supplied on the enablement envelope (Story 4.4).
    /// </summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <param name="readiness">The launch-readiness decision to record.</param>
    /// <returns>The launch-ready Agent state.</returns>
    internal static AgentState StateLaunchReady(CreateAgent create, AgentLaunchReadiness? readiness = null)
    {
        AgentState state = StateWithContentSafety(create);
        state.Apply(new AgentLaunchReadinessRecorded(AgentId, readiness ?? SampleLaunchReadiness, 1, state.ConfigurationVersion + 1));
        return state;
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
    /// Builds a created state with a linked Party identity, a recorded Provider/model selection, an Automatic Response
    /// Mode, AND a recorded Content Safety Policy (lifecycle still Draft) — a fully readiness-cleared Agent (Story 1.6:
    /// Automatic mode needs no approver policy; Story 1.7: content safety is the final required gate) whose only
    /// remaining gate is the live provider verdict at activation.
    /// </summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <param name="providerId">The provider id to select.</param>
    /// <param name="modelId">The model id to select.</param>
    /// <param name="capabilityVersion">The captured provider capability version.</param>
    /// <returns>The Agent state with a linked Party, a provider selection, Automatic mode, and a content-safety policy.</returns>
    internal static AgentState StateWithSelectedProvider(
        CreateAgent create,
        string providerId = SelectedProviderId,
        string modelId = SelectedModelId,
        int capabilityVersion = SelectedCapabilityVersion)
    {
        AgentState state = StateWithLinkedParty(create);
        state.Apply(new AgentProviderModelSelected(AgentId, providerId, modelId, capabilityVersion, state.ConfigurationVersion + 1));
        state.Apply(new AgentResponseModeConfigured(AgentId, AgentResponseMode.Automatic, state.ConfigurationVersion + 1));
        state.Apply(new AgentContentSafetyPolicyConfigured(AgentId, SampleContentSafetyConfiguration, 1, state.ConfigurationVersion + 1));
        return state;
    }

    /// <summary>Builds a created state with a recorded Content Safety Policy applied (lifecycle still Draft).</summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <param name="configuration">The Content Safety configuration to record.</param>
    /// <returns>The Agent state with the recorded Content Safety configuration (content-safety policy version = 1).</returns>
    internal static AgentState StateWithContentSafety(CreateAgent create, AgentContentSafetyConfiguration? configuration = null)
    {
        AgentState state = StateWith(create);
        state.Apply(new AgentContentSafetyPolicyConfigured(AgentId, configuration ?? SampleContentSafetyConfiguration, 1, state.ConfigurationVersion + 1));
        return state;
    }

    /// <summary>Builds a created state with only a chosen Response Mode applied (lifecycle still Draft).</summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <param name="mode">The Response Mode to record.</param>
    /// <returns>The Agent state with the recorded Response Mode.</returns>
    internal static AgentState StateWithResponseMode(CreateAgent create, AgentResponseMode mode = AgentResponseMode.Automatic)
    {
        AgentState state = StateWith(create);
        state.Apply(new AgentResponseModeConfigured(AgentId, mode, state.ConfigurationVersion + 1));
        return state;
    }

    /// <summary>Builds a created state with a recorded Approver Policy applied (no Response Mode; lifecycle Draft).</summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <param name="policy">The Approver Policy to record.</param>
    /// <returns>The Agent state with the recorded Approver Policy (approver-policy version = 1).</returns>
    internal static AgentState StateWithApproverPolicy(CreateAgent create, AgentApproverPolicy? policy = null)
    {
        AgentState state = StateWith(create);
        state.Apply(new AgentApproverPolicyConfigured(AgentId, policy ?? SampleApproverPolicy, 1, state.ConfigurationVersion + 1));
        return state;
    }

    /// <summary>
    /// Builds a fully Confirmation-ready Agent state (lifecycle still Draft): linked Party, recorded Provider/model,
    /// Confirmation Response Mode, a configured Approver Policy, AND a recorded Content Safety Policy — so the only
    /// remaining activation gate is the live approver-policy verdict (Story 1.6 AC3; Story 1.7 content-safety gate
    /// cleared).
    /// </summary>
    /// <param name="create">The create command whose creation event seeds the state.</param>
    /// <param name="policy">The Approver Policy to record.</param>
    /// <returns>The Confirmation-ready Agent state.</returns>
    internal static AgentState StateConfirmationReady(CreateAgent create, AgentApproverPolicy? policy = null)
    {
        AgentState state = StateWithLinkedParty(create);
        state.Apply(new AgentProviderModelSelected(AgentId, SelectedProviderId, SelectedModelId, SelectedCapabilityVersion, state.ConfigurationVersion + 1));
        state.Apply(new AgentResponseModeConfigured(AgentId, AgentResponseMode.Confirmation, state.ConfigurationVersion + 1));
        state.Apply(new AgentApproverPolicyConfigured(AgentId, policy ?? SampleApproverPolicy, 1, state.ConfigurationVersion + 1));
        state.Apply(new AgentContentSafetyPolicyConfigured(AgentId, SampleContentSafetyConfiguration, 1, state.ConfigurationVersion + 1));
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
                case AgentResponseModeConfigured e: state.Apply(e); break;
                case AgentApproverPolicyConfigured e: state.Apply(e); break;
                case AgentContentSafetyPolicyConfigured e: state.Apply(e); break;
                case AgentLaunchReadinessRecorded e: state.Apply(e); break;
                case AgentProductionLikeGenerationEnabled e: state.Apply(e); break;
                case AgentLaunchReadinessRejection e: state.Apply(e); break;
                case AgentProductionLikeGenerationBlockedRejection e: state.Apply(e); break;
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
