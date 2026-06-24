using System;
using System.Collections.Generic;
using System.Linq;

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

    // SECURITY: server-populated only, identical trust model to the two keys above (Story 1.5). The ProviderCatalog
    // read + verdict computation runs in the Server orchestration (AD-3) and its verdict is fed back here through
    // this trusted extension. The command entry point strips any client-supplied value and repopulates it from the
    // orchestration's catalog read. A direct client command (e.g. a spoofed capability version) carries no trusted
    // verdict, so it parses to Unknown and is rejected — the aggregate makes no provider/catalog call at all, so no
    // bad selection is recorded and no provider SDK/credential path is reachable from the aggregate (AC2; AD-9).
    private const string ProviderSelectionValidationExtensionKey = "provider:selectionValidation";

    // SECURITY: server-populated only, identical trust model to the keys above (Story 1.6). The approver-policy
    // resolution (Parties / Tenants projection / Conversations facilitator) runs in the Server orchestration (AD-3)
    // and its aggregated verdict is fed back here through this trusted extension. The command entry point strips any
    // client-supplied value and repopulates it from the resolver's result. A direct ActivateAgent that did not
    // re-resolve carries no trusted verdict, so it parses to Unknown and a Confirmation-mode policy fails closed —
    // the aggregate makes no Parties/Tenants/Conversations call at all (AD-3; AC3).
    private const string ApproverPolicyValidationExtensionKey = "approver:policyValidation";

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
        // Provider/model readiness is re-validated here (1.5 AC2 "or activate"): the activation orchestration re-reads
        // the catalog for the recorded (ProviderId, ModelId) and populates the trusted verdict; a direct activation
        // that did not re-validate carries no trusted verdict, so a present selection fails closed with
        // ProviderUnavailable until a genuinely-ready verdict clears it (AD-9, AD-12).
        // Response mode + approver policy are re-validated the same way (1.6 AC1, AC3): a Confirmation-mode agent with
        // a configured policy fails closed with ApproverPolicyUnresolvable unless the activation orchestration re-resolved
        // the sources and populated a trusted Valid verdict. Automatic mode needs no approver policy and is unaffected.
        IReadOnlyList<AgentActivationBlocker> blockers =
            AgentConfigurationPolicy.ComputeActivationBlockers(
                state.DisplayName,
                state.Instructions,
                state.PartyId is not null,
                hasProviderSelection: state.ProviderId is not null,
                selectedProviderReady: ReadProviderSelectionValidation(envelope) == ProviderSelectionValidationStatus.Valid,
                responseMode: state.ResponseMode,
                hasApproverPolicy: state.ApproverPolicySources is { Count: > 0 },
                approverPolicyResolved: ReadApproverPolicyValidation(envelope) == ApproverPolicyValidationStatus.Valid);
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

    /// <summary>Handles selecting an enabled Provider/model from the governed catalog for the Agent (AC1, AC2, AC3; FR-5).</summary>
    /// <param name="command">The selection command (carries only the safe ids + captured capability version).</param>
    /// <param name="state">The current Agent state.</param>
    /// <param name="envelope">The command envelope (carries the trusted authorization and provider-validation extensions).</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(SelectAgentProviderModel command, AgentState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string agentId = envelope.AggregateId;

        if (!IsAgentAdmin(envelope))
        {
            return Denied(agentId, envelope, nameof(SelectAgentProviderModel));
        }

        if (state is null || !state.IsCreated)
        {
            return DomainResult.Rejection([new AgentNotFoundRejection(agentId)]);
        }

        // AC2 fail-closed: anything other than a trusted Valid verdict (disabled/missing/not-configured/not-text-gen/
        // missing-metadata/unauthorized/unavailable — or an absent/unparseable verdict from a direct-gateway call)
        // rejects the selection and changes no state. The catalog/provider is never reached from here (AD-3, AD-9);
        // the verdict is plain trusted data — so no provider SDK call or credential access occurs on this path.
        ProviderSelectionValidationStatus validation = ReadProviderSelectionValidation(envelope);
        if (validation != ProviderSelectionValidationStatus.Valid)
        {
            return DomainResult.Rejection([new AgentProviderModelSelectionRejected(agentId, validation)]);
        }

        // Valid verdict, idempotent re-select: re-asserting the same provider/model/version is a deterministic no-op
        // (AD-13) — no duplicate event, no version bump.
        if (string.Equals(state.ProviderId, command.ProviderId, StringComparison.Ordinal)
            && string.Equals(state.ModelId, command.ModelId, StringComparison.Ordinal)
            && state.ProviderCapabilityVersion == command.ProviderCapabilityVersion)
        {
            return DomainResult.NoOp();
        }

        // Valid verdict, new/changed selection: selecting/changing the provider is a configuration change → bump
        // ConfigurationVersion (needed for the AD-4 interaction snapshot in Epic 2). A changed selection
        // deterministically overwrites the single recorded selection; prior events are append-only and never
        // rewritten (AC3). Lifecycle is unchanged (Story 1.3 invariant) — readiness is surfaced through the blocker.
        return DomainResult.Success([
            new AgentProviderModelSelected(
                agentId,
                command.ProviderId,
                command.ModelId,
                command.ProviderCapabilityVersion,
                state.ConfigurationVersion + 1),
        ]);
    }

    /// <summary>Handles choosing the Agent's Response Mode (AC1; FR-6).</summary>
    /// <param name="command">The response-mode command (carries only the safe mode choice).</param>
    /// <param name="state">The current Agent state.</param>
    /// <param name="envelope">The command envelope (carries the trusted authorization extension).</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(ConfigureAgentResponseMode command, AgentState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string agentId = envelope.AggregateId;

        if (!IsAgentAdmin(envelope))
        {
            return Denied(agentId, envelope, nameof(ConfigureAgentResponseMode));
        }

        if (state is null || !state.IsCreated)
        {
            return DomainResult.Rejection([new AgentNotFoundRejection(agentId)]);
        }

        // The Unknown sentinel is the "not-yet-configured" marker — it can never be chosen as a mode (AC1).
        if (command.Mode == AgentResponseMode.Unknown)
        {
            return Invalid(agentId, "Response mode must be Automatic or Confirmation.");
        }

        // Re-asserting the recorded mode is a deterministic no-op (AD-13) — no duplicate event, no version bump.
        if (state.ResponseMode == command.Mode)
        {
            return DomainResult.NoOp();
        }

        // Choosing/changing the mode is a configuration change → bump ConfigurationVersion (AD-4 snapshot). Lifecycle
        // is unchanged (Story 1.3 invariant) and the change applies only to future Agent Calls (AC1).
        return DomainResult.Success([
            new AgentResponseModeConfigured(agentId, command.Mode, state.ConfigurationVersion + 1),
        ]);
    }

    /// <summary>Handles configuring the Agent's Approver Policy for Confirmation mode (AC2, AC4; FR-7).</summary>
    /// <param name="command">The approver-policy command (carries only the safe policy value).</param>
    /// <param name="state">The current Agent state.</param>
    /// <param name="envelope">The command envelope (carries the trusted authorization extension).</param>
    /// <returns>The domain result.</returns>
    public static DomainResult Handle(ConfigureAgentApproverPolicy command, AgentState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string agentId = envelope.AggregateId;

        if (!IsAgentAdmin(envelope))
        {
            return Denied(agentId, envelope, nameof(ConfigureAgentApproverPolicy));
        }

        if (state is null || !state.IsCreated)
        {
            return DomainResult.Rejection([new AgentNotFoundRejection(agentId)]);
        }

        // Structural validation only — storing the policy NEVER reads Parties/Tenants/Conversations (AD-3); resolving
        // the sources against dependencies is the activation-readiness concern (the trusted verdict at activation).
        // The reason is a safe classification and never echoes a configured value (AD-14).
        string? reason = ValidateAndNormalizeApproverPolicy(command.Policy, out AgentApproverPolicy normalized);
        if (reason is not null)
        {
            return Invalid(agentId, reason);
        }

        // Re-asserting an equal policy (same ordered sources + disclosure) is a deterministic no-op (AD-13).
        if (state.ApproverPolicySources is not null
            && state.ApproverPolicyDisclosure == normalized.DisclosureCategory
            && state.ApproverPolicySources.SequenceEqual(normalized.Sources))
        {
            return DomainResult.NoOp();
        }

        // A genuine policy change bumps both the policy version (AC4) and the configuration version (AD-4). Lifecycle
        // is unchanged; prior events are append-only and never rewritten, so the change is future-only (AC4).
        return DomainResult.Success([
            new AgentApproverPolicyConfigured(
                agentId,
                normalized,
                state.ApproverPolicyVersion + 1,
                state.ConfigurationVersion + 1),
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

    // Reads the trusted, server-populated provider-readiness verdict from the envelope extension. Fails closed to
    // Unknown when the key is absent, or when its value is not an exact, case-sensitive ProviderSelectionValidationStatus
    // *name* (AC2) — identical hardening to ReadPartyLinkValidation, so numeric/aliased/cased input (e.g. "1", "valid",
    // " Valid") can never become Valid and bypass catalog validation.
    private static ProviderSelectionValidationStatus ReadProviderSelectionValidation(CommandEnvelope envelope)
        => envelope.Extensions?.TryGetValue(ProviderSelectionValidationExtensionKey, out string? value) == true
            && Enum.TryParse(value, ignoreCase: false, out ProviderSelectionValidationStatus status)
            && string.Equals(Enum.GetName(status), value, StringComparison.Ordinal)
                ? status
                : ProviderSelectionValidationStatus.Unknown;

    // Reads the trusted, server-populated approver-policy verdict from the envelope extension. Fails closed to
    // Unknown when the key is absent, or when its value is not an exact, case-sensitive ApproverPolicyValidationStatus
    // *name* (AC3) — identical hardening to the party/provider verdict reads, so numeric/aliased/cased input
    // (e.g. "1", "valid", " Valid") can never become Valid and bypass approver-policy resolution.
    private static ApproverPolicyValidationStatus ReadApproverPolicyValidation(CommandEnvelope envelope)
        => envelope.Extensions?.TryGetValue(ApproverPolicyValidationExtensionKey, out string? value) == true
            && Enum.TryParse(value, ignoreCase: false, out ApproverPolicyValidationStatus status)
            && string.Equals(Enum.GetName(status), value, StringComparison.Ordinal)
                ? status
                : ApproverPolicyValidationStatus.Unknown;

    // Structurally validates and normalizes a configured Approver Policy (Story 1.6 AC2). Each source's shape must
    // match its kind; the disclosure category must be specified; duplicate sources are rejected. An EMPTY source
    // list is structurally storable (it just won't satisfy Confirmation-mode readiness). Normalization nulls blank
    // PartyId/TenantRole fields and strips the fields that do not belong to a kind, so the recorded policy and the
    // idempotency comparison are stable. Returns a safe rejection reason (never echoing a configured value), or
    // <see langword="null"/> when storable (with the normalized policy in <paramref name="normalized"/>).
    private static string? ValidateAndNormalizeApproverPolicy(AgentApproverPolicy? policy, out AgentApproverPolicy normalized)
    {
        normalized = policy ?? new AgentApproverPolicy([], ApproverPolicyBasisDisclosure.Unknown);
        if (policy is null)
        {
            return "Approver policy is required.";
        }

        if (policy.DisclosureCategory == ApproverPolicyBasisDisclosure.Unknown)
        {
            return "Approver policy disclosure category must be specified.";
        }

        IReadOnlyList<ApproverPolicySource> sources = policy.Sources ?? [];
        var normalizedSources = new List<ApproverPolicySource>(sources.Count);
        var seen = new HashSet<ApproverPolicySource>();
        foreach (ApproverPolicySource source in sources)
        {
            string? partyId = NormalizeOptionalText(source.PartyId);
            string? tenantRole = NormalizeOptionalText(source.TenantRole);
            switch (source.Kind)
            {
                case ApproverPolicySourceKind.Caller:
                case ApproverPolicySourceKind.ConversationOwner:
                    if (partyId is not null || tenantRole is not null)
                    {
                        return "Caller and ConversationOwner approver sources must not carry a PartyId or TenantRole.";
                    }

                    break;
                case ApproverPolicySourceKind.PredefinedParty:
                    if (partyId is null || tenantRole is not null)
                    {
                        return "A PredefinedParty approver source requires a PartyId and no TenantRole.";
                    }

                    break;
                case ApproverPolicySourceKind.TenantRole:
                    if (tenantRole is null || partyId is not null)
                    {
                        return "A TenantRole approver source requires a TenantRole and no PartyId.";
                    }

                    break;
                default:
                    return "Approver source kind must be specified.";
            }

            var normalizedSource = new ApproverPolicySource(source.Kind, partyId, tenantRole);
            if (!seen.Add(normalizedSource))
            {
                return "Approver policy sources must not contain duplicates.";
            }

            normalizedSources.Add(normalizedSource);
        }

        normalized = new AgentApproverPolicy(normalizedSources, policy.DisclosureCategory);
        return null;
    }

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
