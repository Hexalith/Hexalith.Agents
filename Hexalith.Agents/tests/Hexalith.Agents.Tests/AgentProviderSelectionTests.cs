using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Handle-method tests for the Provider/model selection behaviour added to <see cref="AgentAggregate"/>
/// (Story 1.5). Covers: the AD-3 trusted-verdict gate and fail-closed rejection on any non-<c>Valid</c>/absent
/// verdict with no provider SDK call or credential access (AC2), recording only the safe ids + capability version
/// and bumping the configuration version while lifecycle stays unchanged (AC1), idempotent re-select and changed
/// selection without rewriting prior events (AC3, AD-13), authorization / not-found fail-closed behaviour, and the
/// <c>MissingProviderSelection</c>/<c>ProviderUnavailable</c> readiness gates blocking then unblocking activation.
/// </summary>
public sealed class AgentProviderSelectionTests
{
    private const string OtherModelId = "gpt-4o-mini";

    private static SelectAgentProviderModel SelectCommand(
        string providerId = SelectedProviderId,
        string modelId = SelectedModelId,
        int capabilityVersion = SelectedCapabilityVersion)
        => new(providerId, modelId, capabilityVersion);

    // ===== AC1: select success records only safe ids + version, bumps version, lifecycle unchanged =====

    [Fact]
    public void Select_with_valid_verdict_records_selection_bumps_version_and_keeps_lifecycle()
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1, no selection

        SelectAgentProviderModel command = SelectCommand();
        DomainResult result = AgentAggregate.Handle(command, state, SelectEnvelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentProviderModelSelected selected = result.Events[0].ShouldBeOfType<AgentProviderModelSelected>();
        selected.AgentId.ShouldBe(AgentId);
        selected.ProviderId.ShouldBe(SelectedProviderId);
        selected.ModelId.ShouldBe(SelectedModelId);
        selected.ProviderCapabilityVersion.ShouldBe(SelectedCapabilityVersion);
        selected.ConfigurationVersion.ShouldBe(2);

        ApplyAll(state, result);
        state.ProviderId.ShouldBe(SelectedProviderId);
        state.ModelId.ShouldBe(SelectedModelId);
        state.ProviderCapabilityVersion.ShouldBe(SelectedCapabilityVersion);
        state.ConfigurationVersion.ShouldBe(2);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft); // selecting never changes lifecycle (Story 1.3 invariant)
    }

    // ===== AC2: any non-Valid verdict fails closed — nothing recorded, and the provider gate still blocks =====

    [Theory]
    [InlineData(ProviderSelectionValidationStatus.Disabled)]
    [InlineData(ProviderSelectionValidationStatus.Missing)]
    [InlineData(ProviderSelectionValidationStatus.NotConfigured)]
    [InlineData(ProviderSelectionValidationStatus.NotTextGenerationCapable)]
    [InlineData(ProviderSelectionValidationStatus.MissingCapabilityMetadata)]
    [InlineData(ProviderSelectionValidationStatus.Unauthorized)]
    [InlineData(ProviderSelectionValidationStatus.Unavailable)]
    [InlineData(ProviderSelectionValidationStatus.Unknown)]
    public void Select_with_non_valid_verdict_rejects_with_that_status_and_records_nothing(ProviderSelectionValidationStatus verdict)
    {
        AgentState state = StateWithLinkedParty(ValidCreate()); // party cleared, so only the provider gate is in play

        SelectAgentProviderModel command = SelectCommand();
        DomainResult result = AgentAggregate.Handle(command, state, SelectEnvelope(command, verdict));

        result.IsRejection.ShouldBeTrue();
        AgentProviderModelSelectionRejected rejected = result.Events[0].ShouldBeOfType<AgentProviderModelSelectionRejected>();
        rejected.AgentId.ShouldBe(AgentId);
        rejected.Status.ShouldBe(verdict);

        ApplyAll(state, result);
        state.ProviderId.ShouldBeNull(); // AC1/AC2: nothing recorded on a rejected selection
        state.ModelId.ShouldBeNull();
        state.ProviderCapabilityVersion.ShouldBeNull();

        // AC2: with no recorded selection, activation still fails closed with MissingProviderSelection.
        DomainResult activation = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));
        AgentActivationBlockedRejection blocked = activation.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        blocked.Blockers.ShouldContain(AgentActivationBlocker.MissingProviderSelection);
    }

    [Fact]
    public void Select_with_absent_verdict_fails_closed_to_unknown()
    {
        // A direct-gateway command (e.g. a spoofed capability version) that never went through the orchestration
        // carries no trusted verdict — the aggregate makes no catalog/provider call and rejects it.
        AgentState state = StateWith(ValidCreate());

        SelectAgentProviderModel command = SelectCommand();
        DomainResult result = AgentAggregate.Handle(command, state, SelectEnvelope(command, includeValidation: false));

        result.Events[0].ShouldBeOfType<AgentProviderModelSelectionRejected>().Status.ShouldBe(ProviderSelectionValidationStatus.Unknown);
        ApplyAll(state, result);
        state.ProviderId.ShouldBeNull();
    }

    [Theory]
    [InlineData("1")]      // numeric underlying value of Valid — Enum.TryParse would resolve it, so it must be rejected
    [InlineData("01")]     // numeric with leading zero
    [InlineData("valid")]  // wrong case — the verdict is matched case-sensitively by its canonical name
    [InlineData(" Valid")] // padded — not an exact name
    public void Select_with_numeric_or_aliased_verdict_fails_closed_to_unknown(string rawVerdict)
    {
        // Mirrors the Story 1.4 verdict-parse hardening: a numeric/aliased/cased form must never be trusted as Valid.
        AgentState state = StateWith(ValidCreate());
        SelectAgentProviderModel command = SelectCommand();

        CommandEnvelope envelope = EnvelopeWithRawProviderValidation(command, rawVerdict);
        DomainResult result = AgentAggregate.Handle(command, state, envelope);

        result.Events[0].ShouldBeOfType<AgentProviderModelSelectionRejected>().Status.ShouldBe(ProviderSelectionValidationStatus.Unknown);
        ApplyAll(state, result);
        state.ProviderId.ShouldBeNull(); // nothing recorded on a fail-closed rejection
    }

    // ===== AC3 / AD-13: idempotent re-select, changed selection, prior event preserved =====

    [Fact]
    public void Reselect_identical_provider_model_version_is_an_idempotent_noop()
    {
        AgentState state = StateWithSelectedProvider(ValidCreate());

        SelectAgentProviderModel command = SelectCommand();
        DomainResult result = AgentAggregate.Handle(command, state, SelectEnvelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Changing_the_selection_emits_a_new_event_bumps_version_and_does_not_rewrite_prior_events()
    {
        AgentState state = StateWith(ValidCreate());

        // First selection.
        SelectAgentProviderModel first = SelectCommand();
        DomainResult firstResult = AgentAggregate.Handle(first, state, SelectEnvelope(first));
        AgentProviderModelSelected firstEvent = firstResult.Events[0].ShouldBeOfType<AgentProviderModelSelected>();
        ApplyAll(state, firstResult);
        int versionAfterFirst = state.ConfigurationVersion;

        // Change the model selection.
        SelectAgentProviderModel changed = SelectCommand(modelId: OtherModelId, capabilityVersion: 2);
        DomainResult changedResult = AgentAggregate.Handle(changed, state, SelectEnvelope(changed));

        changedResult.IsSuccess.ShouldBeTrue();
        AgentProviderModelSelected changedEvent = changedResult.Events[0].ShouldBeOfType<AgentProviderModelSelected>();
        changedEvent.ModelId.ShouldBe(OtherModelId);
        changedEvent.ProviderCapabilityVersion.ShouldBe(2);
        changedEvent.ConfigurationVersion.ShouldBe(versionAfterFirst + 1);

        // AC3: the prior append-only event is unchanged — a changed selection never rewrites it.
        firstEvent.ModelId.ShouldBe(SelectedModelId);
        firstEvent.ProviderCapabilityVersion.ShouldBe(SelectedCapabilityVersion);

        ApplyAll(state, changedResult);
        state.ModelId.ShouldBe(OtherModelId); // exactly one recorded selection, now the change
        state.ProviderCapabilityVersion.ShouldBe(2);
    }

    // QA gap-fill (Story 1.5 AC3/AD-13): the idempotency key is the FULL (provider, model, capabilityVersion) triple.
    // The changed-selection test above mutates model AND version together; these prove the per-field boundary — a
    // change in EITHER the captured capability version alone OR the provider alone is a real change, not a NoOp.
    [Fact]
    public void Reselect_same_provider_and_model_with_only_a_changed_capability_version_emits_a_new_event()
    {
        AgentState state = StateWithSelectedProvider(ValidCreate()); // selected at SelectedCapabilityVersion
        int versionBefore = state.ConfigurationVersion;

        // Same provider + model, but the captured capability version changed (e.g. the catalog metadata was revised).
        SelectAgentProviderModel command = SelectCommand(capabilityVersion: SelectedCapabilityVersion + 1);
        DomainResult result = AgentAggregate.Handle(command, state, SelectEnvelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentProviderModelSelected selected = result.Events[0].ShouldBeOfType<AgentProviderModelSelected>();
        selected.ProviderId.ShouldBe(SelectedProviderId);
        selected.ModelId.ShouldBe(SelectedModelId);
        selected.ProviderCapabilityVersion.ShouldBe(SelectedCapabilityVersion + 1);
        selected.ConfigurationVersion.ShouldBe(versionBefore + 1);

        ApplyAll(state, result);
        state.ProviderCapabilityVersion.ShouldBe(SelectedCapabilityVersion + 1);
    }

    [Fact]
    public void Changing_only_the_provider_emits_a_new_event_and_bumps_version()
    {
        AgentState state = StateWithSelectedProvider(ValidCreate());
        int versionBefore = state.ConfigurationVersion;

        SelectAgentProviderModel command = SelectCommand(providerId: "anthropic");
        DomainResult result = AgentAggregate.Handle(command, state, SelectEnvelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentProviderModelSelected selected = result.Events[0].ShouldBeOfType<AgentProviderModelSelected>();
        selected.ProviderId.ShouldBe("anthropic");
        selected.ModelId.ShouldBe(SelectedModelId);
        selected.ConfigurationVersion.ShouldBe(versionBefore + 1);

        ApplyAll(state, result);
        state.ProviderId.ShouldBe("anthropic"); // exactly one recorded selection, now the changed provider
    }

    // ===== Not-found and authorization fail closed =====

    [Fact]
    public void Select_on_a_missing_agent_is_rejected_as_not_found()
    {
        SelectAgentProviderModel command = SelectCommand();

        DomainResult result = AgentAggregate.Handle(command, state: null, SelectEnvelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentNotFoundRejection>();
    }

    [Fact]
    public void Select_without_agents_admin_is_denied_before_reading_the_verdict()
    {
        AgentState state = StateWith(ValidCreate());
        SelectAgentProviderModel command = SelectCommand();

        // Even with a forged Valid verdict, the unauthorized caller is denied first.
        DomainResult result = AgentAggregate.Handle(
            command,
            state,
            SelectEnvelope(command, ProviderSelectionValidationStatus.Valid, isAgentsAdmin: false, actorUserId: "intruder"));

        AgentAdministrationDeniedRejection denied = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
        denied.CommandName.ShouldBe(nameof(SelectAgentProviderModel));
        result.Events.ShouldNotContain(e => e is AgentProviderModelSelected);
    }

    // ===== AC2: provider readiness is a distinct gate that blocks then unblocks activation =====

    [Fact]
    public void Activation_is_blocked_by_missing_provider_selection_when_no_provider_is_selected()
    {
        // Party cleared, but no provider selected yet — the only remaining gate is MissingProviderSelection.
        AgentState state = StateWithLinkedParty(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, SelectEnvelope(new ActivateAgent()));

        AgentActivationBlockedRejection blocked = result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        blocked.Blockers.ShouldBe([AgentActivationBlocker.MissingProviderSelection]);
    }

    [Fact]
    public void Activation_is_blocked_by_provider_unavailable_when_a_selection_is_present_but_verdict_is_absent()
    {
        // A selection is recorded, but a direct activation that did not re-validate carries no trusted verdict —
        // it fails closed with ProviderUnavailable (AC2), never a blind flip to active.
        AgentState state = StateWithSelectedProvider(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, SelectEnvelope(new ActivateAgent(), includeValidation: false));

        AgentActivationBlockedRejection blocked = result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        blocked.Blockers.ShouldBe([AgentActivationBlocker.ProviderUnavailable]);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);
    }

    [Theory]
    [InlineData(ProviderSelectionValidationStatus.Disabled)]
    [InlineData(ProviderSelectionValidationStatus.Unavailable)]
    [InlineData(ProviderSelectionValidationStatus.Unknown)]
    public void Activation_is_blocked_by_provider_unavailable_when_the_revalidation_verdict_is_non_valid(ProviderSelectionValidationStatus verdict)
    {
        AgentState state = StateWithSelectedProvider(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, SelectEnvelope(new ActivateAgent(), verdict));

        result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldBe([AgentActivationBlocker.ProviderUnavailable]);
    }

    [Fact]
    public void Activation_succeeds_when_a_selection_is_present_and_the_revalidation_verdict_is_valid()
    {
        AgentState state = StateWithSelectedProvider(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, SelectEnvelope(new ActivateAgent(), ProviderSelectionValidationStatus.Valid));

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentActivated>();
    }

    // ===== Replay / rehydration through Apply including the persisted rejection =====

    [Fact]
    public void Apply_select_then_change_tracks_a_single_selection_and_bumps_version()
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1

        state.Apply(new AgentProviderModelSelected(AgentId, SelectedProviderId, SelectedModelId, SelectedCapabilityVersion, ConfigurationVersion: 2));
        state.ProviderId.ShouldBe(SelectedProviderId);
        state.ModelId.ShouldBe(SelectedModelId);
        state.ProviderCapabilityVersion.ShouldBe(SelectedCapabilityVersion);
        state.ConfigurationVersion.ShouldBe(2);

        state.Apply(new AgentProviderModelSelected(AgentId, SelectedProviderId, OtherModelId, 3, ConfigurationVersion: 3));
        state.ModelId.ShouldBe(OtherModelId); // still exactly one selection, now the change
        state.ProviderCapabilityVersion.ShouldBe(3);
        state.ConfigurationVersion.ShouldBe(3);
    }

    [Fact]
    public void Apply_selection_rejection_is_a_replay_safe_noop()
    {
        AgentState state = StateWithSelectedProvider(ValidCreate());
        string? providerBefore = state.ProviderId;

        state.Apply(new AgentProviderModelSelectionRejected(AgentId, ProviderSelectionValidationStatus.Unavailable));

        state.ProviderId.ShouldBe(providerBefore); // persisted rejections never mutate the recorded selection
    }

    [Fact]
    public void Apply_selection_before_create_is_ignored()
    {
        var state = new AgentState();

        state.Apply(new AgentProviderModelSelected(AgentId, SelectedProviderId, SelectedModelId, SelectedCapabilityVersion, ConfigurationVersion: 2));

        state.ProviderId.ShouldBeNull();
        state.IsCreated.ShouldBeFalse();
    }

    // ===== Full reflection dispatch + JSON payload round-trip via ProcessAsync (E2E journey) =====

    [Fact]
    public async Task ProcessAndApply_select_then_reselect_then_change_threads_state_and_keeps_one_selection()
    {
        var aggregate = new AgentAggregate();
        var state = new AgentState();

        (await ProcessAndApplyAsync(aggregate, state, ValidCreate())).IsSuccess.ShouldBeTrue();

        // Select through the full pipeline (trusted verdict in the envelope).
        SelectAgentProviderModel select = SelectCommand();
        (await ProcessAndApplyAsync(aggregate, state, select, SelectEnvelope(select))).IsSuccess.ShouldBeTrue();
        state.ProviderId.ShouldBe(SelectedProviderId);
        state.ModelId.ShouldBe(SelectedModelId);

        // The selection surfaces through the read path (AC1) without exposing any secret.
        AgentStatusView view = AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull();
        view.HasProviderSelection.ShouldBeTrue();
        view.SelectedProviderId.ShouldBe(SelectedProviderId);
        view.SelectedModelId.ShouldBe(SelectedModelId);

        // Re-selecting the same triple is an idempotent no-op.
        (await ProcessAndApplyAsync(aggregate, state, select, SelectEnvelope(select))).IsNoOp.ShouldBeTrue();

        // Changing the selection keeps exactly one recorded selection.
        SelectAgentProviderModel changed = SelectCommand(modelId: OtherModelId, capabilityVersion: 2);
        (await ProcessAndApplyAsync(aggregate, state, changed, SelectEnvelope(changed))).IsSuccess.ShouldBeTrue();
        state.ModelId.ShouldBe(OtherModelId);
        state.ProviderCapabilityVersion.ShouldBe(2);
    }

    // Builds a select envelope whose provider:selectionValidation extension holds an arbitrary raw string (to test
    // fail-closed parsing of an unrecognized verdict value).
    private static CommandEnvelope EnvelopeWithRawProviderValidation(SelectAgentProviderModel command, string rawValidationValue)
        => new(
            "msg-raw",
            TenantId,
            "agent",
            AgentId,
            nameof(SelectAgentProviderModel),
            JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-raw",
            null,
            "admin-user",
            new Dictionary<string, string>
            {
                [AgentAdminExtensionKey] = "true",
                [ProviderSelectionValidationExtensionKey] = rawValidationValue,
            });
}
