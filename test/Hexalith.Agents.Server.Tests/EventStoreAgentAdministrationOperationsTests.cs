namespace Hexalith.Agents.Server.Tests;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// End-to-end tests for the live public administration surface (Story 5.2 AC1–AC4) across the command path and the
/// projected setup read: an authorized write is accepted with a structured identity, the projection is what turns
/// that acceptance into confirmed truth, and an unauthorized caller learns nothing at all.
/// </summary>
public sealed class EventStoreAgentAdministrationOperationsTests
{
    private const string TenantId = "acme";
    private const string AgentId = "hexa";
    private const string StoreName = "statestore";

    private readonly FakeReadModelStore _store = new();
    private readonly IEventStoreGatewayClient _gateway = Substitute.For<IEventStoreGatewayClient>();
    private readonly IAgentAdministrationContextProvider _contextProvider = Substitute.For<IAgentAdministrationContextProvider>();
    private readonly IProviderCatalogReader _catalogReader = Substitute.For<IProviderCatalogReader>();
    private readonly IApproverPolicyResolver _approverPolicyResolver = Substitute.For<IApproverPolicyResolver>();

    public EventStoreAgentAdministrationOperationsTests()
    {
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "admin-user", IsAgentsAdmin: true));
        _ = _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse("corr-1", null, "msg-1"));
    }

    [Fact]
    public async Task An_accepted_command_answers_with_a_structured_identity_and_the_submitted_stage()
    {
        AgentOperationResult<AgentCommandAcceptance> result = await Operations().ConfigureResponseModeAsync(
            AgentId,
            new ConfigureAgentResponseMode(AgentResponseMode.Confirmation));

        result.IsSuccess.ShouldBeTrue();
        AgentCommandAcceptance acceptance = result.Value.ShouldNotBeNull();
        acceptance.AgentId.ShouldBe(AgentId);
        acceptance.MessageId.ShouldNotBeNullOrWhiteSpace();
        acceptance.CorrelationId.ShouldNotBeNullOrWhiteSpace();

        // Acceptance is not success: the command was taken, nothing yet claims it is durable.
        acceptance.TruthState.ShouldBe(AgentSetupTruthState.Submitted);
    }

    [Fact]
    public async Task An_acceptance_carries_no_eventstore_internals()
    {
        AgentOperationResult<AgentCommandAcceptance> result = await Operations().DisableAsync(AgentId, new DisableAgent());

        string serialized = JsonSerializer.Serialize(result.Value);
        serialized.ShouldNotContain("revision", Case.Insensitive);
        serialized.ShouldNotContain("stream", Case.Insensitive);
        serialized.ShouldNotContain("aggregate", Case.Insensitive);
        serialized.ShouldNotContain("sequence", Case.Insensitive);
    }

    [Fact]
    public async Task A_caller_supplied_idempotency_key_becomes_the_command_message_id()
    {
        CaptureSubmit();

        AgentOperationResult<AgentCommandAcceptance> first = await Operations().DisableAsync(
            AgentId,
            new DisableAgent(),
            new AgentOperationOptions(IdempotencyKey: "same-key"));
        SubmitCommandRequest firstSubmit = _lastSubmit.ShouldNotBeNull();

        AgentOperationResult<AgentCommandAcceptance> second = await Operations().DisableAsync(
            AgentId,
            new DisableAgent(),
            new AgentOperationOptions(IdempotencyKey: "same-key"));

        // Two identical submissions carry one identity, so the gateway can collapse them instead of appending twice.
        first.Value.ShouldNotBeNull().MessageId.ShouldBe("same-key");
        second.Value.ShouldNotBeNull().MessageId.ShouldBe("same-key");
        firstSubmit.IdempotencyKey.ShouldBe("same-key");
        _lastSubmit.ShouldNotBeNull().IdempotencyKey.ShouldBe("same-key");
    }

    [Fact]
    public async Task A_write_then_projection_then_read_reaches_projection_confirmed()
    {
        _ = await Operations().UpdateConfigurationAsync(
            AgentId,
            new UpdateAgentConfiguration("hexa renamed", null, "instructions long enough to be valid"));

        // Nothing is durable until the projection says so: the read before projection finds no Agent at all.
        AgentSetupResult beforeProjection = (await Operations().GetConfigurationAsync(AgentId)).Value.ShouldNotBeNull();
        beforeProjection.Status.ShouldBe(AgentInspectionStatus.AgentNotFound);

        SeedProjectedSetup(configurationVersion: 2);

        AgentSetupResult afterProjection = (await Operations().GetConfigurationAsync(AgentId, expectedConfigurationVersion: 2)).Value.ShouldNotBeNull();
        AgentSetupView setup = afterProjection.Setup.ShouldNotBeNull();
        setup.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        setup.ConfigurationVersion.ShouldBe(2);
    }

    [Fact]
    public async Task A_read_that_outruns_the_projection_is_authoritative_pending()
    {
        SeedProjectedSetup(configurationVersion: 2);

        AgentSetupResult result = (await Operations().GetStatusAsync(AgentId, expectedConfigurationVersion: 3)).Value.ShouldNotBeNull();

        result.Setup.ShouldNotBeNull().TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Setup.Freshness.ShouldBe(AgentSetupFreshness.Stale);
    }

    [Fact]
    public async Task The_status_and_configuration_reads_serve_identical_truth()
    {
        SeedProjectedSetup(configurationVersion: 2);

        AgentSetupResult status = (await Operations().GetStatusAsync(AgentId)).Value.ShouldNotBeNull();
        AgentSetupResult configuration = (await Operations().GetConfigurationAsync(AgentId)).Value.ShouldNotBeNull();

        status.ShouldBeEquivalentTo(configuration);
    }

    [Fact]
    public async Task Activation_dispatches_with_the_provider_and_mode_the_projection_recorded()
    {
        CaptureSubmit();
        SeedProjectedSetup(configurationVersion: 2, providerId: "openai", modelId: "gpt-4o", responseMode: AgentResponseMode.Automatic);
        _catalogReader
            .GetEntryAsync(TenantId, "openai", "gpt-4o", Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, SelectableEntry()));

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().ActivateAsync(AgentId, new ActivateAgent());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().TruthState.ShouldBe(AgentSetupTruthState.Submitted);

        // The recorded selection is what gets re-validated — the caller never names the provider it wants checked.
        await _catalogReader.Received(1).GetEntryAsync(TenantId, "openai", "gpt-4o", Arg.Any<CancellationToken>());

        SubmitCommandRequest submit = _lastSubmit.ShouldNotBeNull();
        submit.CommandType.ShouldBe(nameof(ActivateAgent));
        submit.AggregateId.ShouldBe(AgentId);
        submit.Tenant.ShouldBe(TenantId);

        Dictionary<string, string> extensions = submit.Extensions.ShouldNotBeNull();
        extensions[AgentProviderSelectionOrchestrator.AgentAdminExtensionKey].ShouldBe("true");
        extensions[AgentProviderSelectionOrchestrator.ProviderSelectionValidationExtensionKey]
            .ShouldBe(nameof(ProviderSelectionValidationStatus.Valid));

        // Automatic mode needs no approver policy, so no approver verdict is asserted from thin air.
        extensions[AgentActivationProviderRevalidation.ApproverPolicyValidationExtensionKey]
            .ShouldBe(nameof(ApproverPolicyValidationStatus.Unknown));
    }

    [Fact]
    public async Task Activation_without_a_projected_selection_still_dispatches_and_leaves_the_verdict_unknown()
    {
        CaptureSubmit();

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().ActivateAsync(AgentId, new ActivateAgent());

        // The surface never invents a provider verdict for an Agent whose selection it cannot see; the aggregate's
        // own gates are what refuse the activation.
        result.IsSuccess.ShouldBeTrue();
        await _catalogReader.DidNotReceiveWithAnyArgs().GetEntryAsync(default!, default!, default!, default);
        _lastSubmit.ShouldNotBeNull().Extensions
            .ShouldNotBeNull()[AgentProviderSelectionOrchestrator.ProviderSelectionValidationExtensionKey]
            .ShouldBe(nameof(ProviderSelectionValidationStatus.Unknown));
    }

    [Fact]
    public async Task An_unauthorized_activation_is_denied_before_any_dependency_is_read_or_dispatched()
    {
        CaptureSubmit();
        SeedProjectedSetup(configurationVersion: 2, providerId: "openai", modelId: "gpt-4o", responseMode: AgentResponseMode.Automatic);
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "intruder", IsAgentsAdmin: false));

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().ActivateAsync(AgentId, new ActivateAgent());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.NotAuthorized);
        _lastSubmit.ShouldBeNull();
        await _catalogReader.DidNotReceiveWithAnyArgs().GetEntryAsync(default!, default!, default!, default);
        await _gateway.DidNotReceiveWithAnyArgs().SubmitCommandAsync(default!, default);
    }

    [Fact]
    public async Task An_unauthorized_caller_is_denied_before_the_command_is_dispatched()
    {
        CaptureSubmit();
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "intruder", IsAgentsAdmin: false));

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().DisableAsync(AgentId, new DisableAgent());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.NotAuthorized);
        _lastSubmit.ShouldBeNull();
        await _gateway.DidNotReceiveWithAnyArgs().SubmitCommandAsync(default!, default);
    }

    [Fact]
    public async Task An_unauthorized_read_reveals_nothing_about_the_agent()
    {
        SeedProjectedSetup(configurationVersion: 2);
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "intruder", IsAgentsAdmin: false));

        AgentSetupResult result = (await Operations().GetStatusAsync(AgentId)).Value.ShouldNotBeNull();

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        result.Setup.ShouldBeNull();
    }

    [Fact]
    public async Task A_caller_from_another_tenant_cannot_read_the_agent()
    {
        SeedProjectedSetup(configurationVersion: 2);
        _contextProvider.GetContext().Returns(new AgentAdministrationContext("other-tenant", "their-admin", IsAgentsAdmin: true));

        AgentSetupResult result = (await Operations().GetStatusAsync(AgentId)).Value.ShouldNotBeNull();

        // Shaped exactly like an Agent that was never created — existence itself is not disclosed.
        result.Status.ShouldBe(AgentInspectionStatus.AgentNotFound);
        result.Setup.ShouldBeNull();
    }

    [Fact]
    public async Task A_gateway_conflict_is_reported_as_a_typed_failure_and_leaves_the_read_model_untouched()
    {
        SeedProjectedSetup(configurationVersion: 2);
        _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<SubmitCommandResponse>>(_ => throw new EventStoreGatewayException(409, "conflict"));

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().DisableAsync(AgentId, new DisableAgent());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Conflict);
        result.Value.ShouldBeNull();
        _store.Snapshot<AgentSetupReadModel>(StoreName, AgentSetupReadModelAddresses.Detail(TenantId, AgentId))
            .ShouldNotBeNull().ConfigurationVersion.ShouldBe(2);
    }

    [Fact]
    public async Task Out_of_scope_administration_commands_stay_fail_closed()
    {
        AgentOperationResult result = await Operations().SelectProviderModelAsync(
            new SelectAgentProviderModel("openai", "gpt-x", 1));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Unavailable);
        await _gateway.DidNotReceiveWithAnyArgs().SubmitCommandAsync(default!, default);
    }

    private SubmitCommandRequest? _lastSubmit;

    private void CaptureSubmit()
        => _gateway
            .SubmitCommandAsync(Arg.Do<SubmitCommandRequest>(request => _lastSubmit = request), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse("corr-1", null, "msg-1"));

    private IAgentAdministrationOperations Operations()
    {
        var dispatcher = new EventStoreAgentCommandDispatcher(_gateway);
        return new EventStoreAgentAdministrationOperations(
            _contextProvider,
            new AgentAdministrationOrchestrator(dispatcher),
            new AgentResponseModeOrchestrator(dispatcher),
            new AgentActivationProviderRevalidation(
                _catalogReader,
                _approverPolicyResolver,
                dispatcher),
            _store,
            Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }),
            new AgentCommandIdentityFactory());
    }

    private void SeedProjectedSetup(
        int configurationVersion,
        string? providerId = null,
        string? modelId = null,
        AgentResponseMode responseMode = AgentResponseMode.Automatic)
        => _store.Seed(
            StoreName,
            AgentSetupReadModelAddresses.Detail(TenantId, AgentId),
            new AgentSetupReadModel
            {
                IsCreated = true,
                AgentId = AgentId,
                TenantId = TenantId,
                DisplayName = "hexa",
                HasInstructions = true,
                InstructionsValid = true,
                InstructionsVersion = 1,
                Lifecycle = AgentLifecycleStatus.Draft,
                ConfigurationVersion = configurationVersion,
                ProviderId = providerId,
                ModelId = modelId,
                ResponseMode = responseMode,
                LastSequenceNumber = configurationVersion,
                ProjectedAt = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero),
                ProjectionVersion = configurationVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });

    private static ProviderCatalogEntryView SelectableEntry()
        => new(
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o",
            ProviderModelStatus.Enabled,
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 128_000,
            MaxOutputTokenLimit: 16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o",
            IsSelectableForNewActiveUse: true,
            CapabilityVersion: 1,
            new ProviderModelPricing("USD", 0.002m, 0.008m, 1));
}
