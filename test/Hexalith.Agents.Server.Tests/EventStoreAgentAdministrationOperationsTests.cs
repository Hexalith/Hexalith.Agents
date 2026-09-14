namespace Hexalith.Agents.Server.Tests;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

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
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    private readonly FakeReadModelStore _store = new();
    private readonly IEventStoreGatewayClient _gateway = Substitute.For<IEventStoreGatewayClient>();

    private readonly IAgentCommandStatusReader _statusReader = Substitute.For<IAgentCommandStatusReader>();
    private readonly IAgentAdministrationContextProvider _contextProvider = Substitute.For<IAgentAdministrationContextProvider>();
    private readonly IProviderCatalogReader _catalogReader = Substitute.For<IProviderCatalogReader>();
    private readonly IApproverPolicyResolver _approverPolicyResolver = Substitute.For<IApproverPolicyResolver>();

    public EventStoreAgentAdministrationOperationsTests()
    {
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "admin-user", IsAgentsAdmin: true));
        _ = _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => AppliedReceipt(call.ArgAt<SubmitCommandRequest>(0), configurationVersion: 4));
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
        NUlid.Ulid.TryParse(acceptance.MessageId, out _).ShouldBeTrue();
        NUlid.Ulid.TryParse(acceptance.CorrelationId, out _).ShouldBeTrue();

        acceptance.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        acceptance.Effect.ShouldBe(AgentSetupWriteEffect.Applied);
        acceptance.TargetConfigurationVersion.ShouldBe(4);
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
            new AgentOperationOptions(IdempotencyKey: MessageId));
        SubmitCommandRequest firstSubmit = _lastSubmit.ShouldNotBeNull();

        AgentOperationResult<AgentCommandAcceptance> second = await Operations().DisableAsync(
            AgentId,
            new DisableAgent(),
            new AgentOperationOptions(IdempotencyKey: MessageId));

        // Two identical submissions carry one identity, so the gateway can collapse them instead of appending twice.
        first.Value.ShouldNotBeNull().MessageId.ShouldBe(MessageId);
        second.Value.ShouldNotBeNull().MessageId.ShouldBe(MessageId);
        first.Value.Effect.ShouldBe(AgentSetupWriteEffect.Applied);
        second.Value.Effect.ShouldBe(first.Value.Effect);
        second.Value.TargetConfigurationVersion.ShouldBe(first.Value.TargetConfigurationVersion);
        firstSubmit.IdempotencyKey.ShouldBe(MessageId);
        _lastSubmit.ShouldNotBeNull().IdempotencyKey.ShouldBe(MessageId);
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
        result.Value.ShouldNotBeNull().TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);

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
    public async Task Authorization_precedes_even_invalid_caller_metadata()
    {
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "intruder", IsAgentsAdmin: false));

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().DisableAsync(
            AgentId,
            new DisableAgent(),
            new AgentOperationOptions(CorrelationId: "not-a-ulid", IdempotencyKey: "not-a-ulid"));

        result.Status.ShouldBe(AgentOperationStatus.NotAuthorized);
        result.CorrelationId.ShouldBeNull();
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TheRealDomainResultPayloadIsUnderstoodAtTheOperationsBoundary(bool alreadyApplied)
    {
        // The payload is written by the aggregate in Hexalith.Agents and read back in Hexalith.Agents.Server, so
        // fixtures hand-built on either side would keep agreeing with themselves after a one-sided rename. This
        // drives the genuine DomainResult.ResultPayload string through the genuine receipt path instead.
        CreateAgent create = new(TenantId, "Hexa Assistant", "Tenant governed assistant", RealInstructions);
        DomainResult domainResult = AgentAggregate.Handle(
            create,
            alreadyApplied ? CreatedState(create) : null,
            AdminEnvelope(create));

        domainResult.ResultPayload.ShouldNotBeNull();
        using JsonDocument payload = JsonDocument.Parse(domainResult.ResultPayload);
        JsonElement element = payload.RootElement.Clone();
        _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new SubmitCommandResponse(
                call.ArgAt<SubmitCommandRequest>(0).CorrelationId!,
                element,
                call.ArgAt<SubmitCommandRequest>(0).MessageId));

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().DisableAsync(AgentId, new DisableAgent());

        result.IsSuccess.ShouldBeTrue();
        AgentCommandAcceptance acceptance = result.Value.ShouldNotBeNull();
        acceptance.Effect.ShouldBe(alreadyApplied
            ? AgentSetupWriteEffect.AlreadyApplied
            : AgentSetupWriteEffect.Applied);
        acceptance.TargetConfigurationVersion.ShouldBe(1);
        acceptance.TruthState.ShouldBe(alreadyApplied
            ? AgentSetupTruthState.ProjectionConfirmed
            : AgentSetupTruthState.AuthoritativePending);
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
    public async Task A_noop_receipt_returns_the_unchanged_authoritative_version_without_projection_guessing()
    {
        _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => AlreadyAppliedReceipt(call.ArgAt<SubmitCommandRequest>(0), configurationVersion: 7));

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().UpdateConfigurationAsync(
            AgentId,
            new UpdateAgentConfiguration("hexa", null, "instructions long enough to be valid"));

        AgentCommandAcceptance acceptance = result.Value.ShouldNotBeNull();
        acceptance.Effect.ShouldBe(AgentSetupWriteEffect.AlreadyApplied);
        acceptance.TargetConfigurationVersion.ShouldBe(7);
    }

    [Fact]
    public async Task A_noop_acceptance_is_projection_confirmed_because_it_appended_nothing_to_reach()
    {
        _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => AlreadyAppliedReceipt(call.ArgAt<SubmitCommandRequest>(0), configurationVersion: 7));

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().UpdateConfigurationAsync(
            AgentId,
            new UpdateAgentConfiguration("hexa", null, "instructions long enough to be valid"));

        result.Value.ShouldNotBeNull().TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
    }

    [Fact]
    public async Task An_applied_acceptance_is_authoritative_but_not_yet_projected()
    {
        _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => AppliedReceipt(call.ArgAt<SubmitCommandRequest>(0), configurationVersion: 4));

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().DisableAsync(AgentId, new DisableAgent());

        result.Value.ShouldNotBeNull().TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
    }

    [Fact]
    public async Task A_domain_rejection_is_reported_as_rejected_rather_than_retryable_unverifiable()
    {
        SeedProjectedSetup(configurationVersion: 2);
        _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                SubmitCommandRequest request = call.ArgAt<SubmitCommandRequest>(0);
                return new SubmitCommandResponse(request.CorrelationId!, null, request.MessageId);
            });
        _statusReader
            .WasRejectedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(true));

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().ActivateAsync(AgentId, new ActivateAgent());

        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Rejected);
        result.Value.ShouldBeNull();

        // Prior setup is preserved.
        _store.Snapshot<AgentSetupReadModel>(StoreName, AgentSetupReadModelAddresses.Detail(TenantId, AgentId))
            .ShouldNotBeNull().ConfigurationVersion.ShouldBe(2);
    }

    [Theory]
    [InlineData("not-rejected")]
    [InlineData("no-status-recorded")]
    public async Task A_payload_less_receipt_that_is_not_a_rejection_stays_unverifiable(string scenario)
    {
        _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                SubmitCommandRequest request = call.ArgAt<SubmitCommandRequest>(0);
                return new SubmitCommandResponse(request.CorrelationId!, null, request.MessageId);
            });
        _statusReader
            .WasRejectedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(scenario == "not-rejected" ? false : (bool?)null));

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().DisableAsync(AgentId, new DisableAgent());

        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.UnableToVerify);
    }

    [Theory]
    [InlineData("missing-payload")]
    [InlineData("malformed-payload")]
    [InlineData("mismatched-message")]
    [InlineData("mismatched-correlation")]
    [InlineData("unknown-effect")]
    [InlineData("undefined-effect")]
    [InlineData("wrong-case-effect")]
    [InlineData("string-version")]
    [InlineData("null-version")]
    [InlineData("bool-version")]
    [InlineData("object-version")]
    public async Task An_unverifiable_gateway_receipt_fails_closed(string scenario)
    {
        _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                SubmitCommandRequest request = call.ArgAt<SubmitCommandRequest>(0);
                return scenario switch
                {
                    "missing-payload" => new SubmitCommandResponse(request.CorrelationId!, null, request.MessageId),
                    "malformed-payload" => new SubmitCommandResponse(
                        request.CorrelationId!,
                        JsonSerializer.SerializeToElement(new { effect = "Applied", configurationVersion = 0 }),
                        request.MessageId),
                    "mismatched-message" => new SubmitCommandResponse(
                        request.CorrelationId!,
                        SetupPayload("Applied", 4),
                        MessageId),
                    "mismatched-correlation" => new SubmitCommandResponse(
                        CorrelationId,
                        SetupPayload("Applied", 4),
                        request.MessageId),
                    "unknown-effect" => new SubmitCommandResponse(
                        request.CorrelationId!,
                        SetupPayload("Unknown", 4),
                        request.MessageId),
                    "undefined-effect" => new SubmitCommandResponse(
                        request.CorrelationId!,
                        SetupPayload("Undefined", 4),
                        request.MessageId),

                    // JsonElement.TryGetInt32 throws on a non-number element, so a version that is not a JSON
                    // number must be rejected by a ValueKind check rather than surfacing as an exception.
                    "string-version" => new SubmitCommandResponse(
                        request.CorrelationId!,
                        NonNumericVersionPayload("\"4\""),
                        request.MessageId),
                    "null-version" => new SubmitCommandResponse(
                        request.CorrelationId!,
                        NonNumericVersionPayload("null"),
                        request.MessageId),
                    "bool-version" => new SubmitCommandResponse(
                        request.CorrelationId!,
                        NonNumericVersionPayload("true"),
                        request.MessageId),
                    "object-version" => new SubmitCommandResponse(
                        request.CorrelationId!,
                        NonNumericVersionPayload("{\"value\":4}"),
                        request.MessageId),
                    _ => new SubmitCommandResponse(
                        request.CorrelationId!,
                        SetupPayload("applied", 4),
                        request.MessageId),
                };
            });

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().DisableAsync(
            AgentId,
            new DisableAgent());

        result.Status.ShouldBe(AgentOperationStatus.UnableToVerify);
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.UnableToVerify);
        result.Value.ShouldBeNull();
    }

    [Fact]
    public async Task NonUlidCallerCommandMetadataIsRejectedBeforeDispatch()
    {
        CaptureSubmit();

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().DisableAsync(
            AgentId,
            new DisableAgent(),
            new AgentOperationOptions(CorrelationId: "not-a-ulid", IdempotencyKey: "not-a-ulid"));

        result.Status.ShouldBe(AgentOperationStatus.ValidationFailed);
        _lastSubmit.ShouldBeNull();
    }

    [Fact]
    public async Task NonUlidIdempotencyKeyIsRejectedAfterAValidCorrelationWithoutDispatch()
    {
        CaptureSubmit();

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().DisableAsync(
            AgentId,
            new DisableAgent(),
            new AgentOperationOptions(CorrelationId: CorrelationId, IdempotencyKey: "not-a-ulid"));

        result.Status.ShouldBe(AgentOperationStatus.ValidationFailed);
        result.CorrelationId.ShouldBe(CorrelationId);
        _lastSubmit.ShouldBeNull();
        await _gateway.DidNotReceiveWithAnyArgs().SubmitCommandAsync(default!, default);
    }

    [Theory]
    [InlineData("correlation")]
    [InlineData("idempotency")]
    public async Task ParseableButNonCanonicalCallerUlidsAreRejectedBeforeDispatch(string field)
    {
        CaptureSubmit();
        var options = new AgentOperationOptions(
            CorrelationId: field == "correlation" ? CorrelationId.ToLowerInvariant() : CorrelationId,
            IdempotencyKey: field == "idempotency" ? MessageId.ToLowerInvariant() : MessageId);

        AgentOperationResult<AgentCommandAcceptance> result = await Operations().DisableAsync(
            AgentId,
            new DisableAgent(),
            options);

        result.Status.ShouldBe(AgentOperationStatus.ValidationFailed);
        _lastSubmit.ShouldBeNull();
        await _gateway.DidNotReceiveWithAnyArgs().SubmitCommandAsync(default!, default);
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
            .Returns(call => AppliedReceipt(call.ArgAt<SubmitCommandRequest>(0), configurationVersion: 4));

    private static SubmitCommandResponse AppliedReceipt(SubmitCommandRequest request, int configurationVersion)
        => new(
            request.CorrelationId!,
            SetupPayload("Applied", configurationVersion),
            request.MessageId);

    private static SubmitCommandResponse AlreadyAppliedReceipt(SubmitCommandRequest request, int configurationVersion)
        => new(
            request.CorrelationId!,
            SetupPayload("AlreadyApplied", configurationVersion),
            request.MessageId);

    private const string RealInstructions = "You are hexa, a helpful and concise enterprise assistant.";

    private static AgentState CreatedState(CreateAgent create)
    {
        AgentState state = new();
        state.Apply(new AgentCreated(
            AgentId,
            create.TenantId,
            create.DisplayName ?? string.Empty,
            create.Description,
            create.Instructions ?? string.Empty,
            ConfigurationVersion: 1,
            InstructionsVersion: 1));
        return state;
    }

    private static CommandEnvelope AdminEnvelope<T>(T command)
        where T : notnull
        => new(
            MessageId,
            TenantId,
            "agent",
            AgentId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId,
            null,
            "admin-user",
            new Dictionary<string, string>
            {
                [AgentProviderSelectionOrchestrator.AgentAdminExtensionKey] = "true",
            });

    private static JsonElement NonNumericVersionPayload(string versionJson)
        => JsonDocument.Parse(
                $$"""{"{{AgentSetupResultPayload.EffectProperty}}":"Applied","{{AgentSetupResultPayload.ConfigurationVersionProperty}}":{{versionJson}} }""")
            .RootElement
            .Clone();

    // Keyed by the shared constants, so a renamed property fails here instead of degrading every write to
    // UnableToVerify at run time.
    private static JsonElement SetupPayload(string effect, int configurationVersion)
        => JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            [AgentSetupResultPayload.EffectProperty] = effect,
            [AgentSetupResultPayload.ConfigurationVersionProperty] = configurationVersion,
        });

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
            new AgentCommandIdentityFactory(),
            _statusReader);
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
