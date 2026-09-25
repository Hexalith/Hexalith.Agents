namespace Hexalith.Agents.Server.Tests;

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// Live public catalog surface (Story 5.3): accepted writes return a structured identity, reads serve the
/// projected catalog, and unauthorized callers learn nothing.
/// </summary>
public sealed class EventStoreProviderCatalogOperationsTests
{
    private const string TenantId = "acme";
    private const string StoreName = "statestore";

    private readonly FakeReadModelStore _store = new();
    private readonly IEventStoreGatewayClient _gateway = Substitute.For<IEventStoreGatewayClient>();
    private readonly IAgentAdministrationContextProvider _contextProvider = Substitute.For<IAgentAdministrationContextProvider>();
    private readonly List<SubmitCommandRequest> _submitted = [];

    public EventStoreProviderCatalogOperationsTests()
    {
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "admin-user", IsAgentsAdmin: true, IsPlatformOperator: true));
        _ = _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                SubmitCommandRequest request = call.Arg<SubmitCommandRequest>();
                _submitted.Add(request);
                return new SubmitCommandResponse(request.CorrelationId!, null, request.MessageId);
            });
        _gateway.GetCommandStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                SubmitCommandRequest? request = _submitted.FirstOrDefault(item => item.MessageId == call.Arg<string>());
                return request is null ? null : new CommandStatusQueryResponse(request.CorrelationId!,
                    nameof(CommandStatus.Received), (int)CommandStatus.Received, MessageId: request.MessageId)
                {
                    TenantId = request.Tenant,
                };
            });
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                long latest = request.Domain == TenantProviderEnablementAggregate.Domain
                    ? _store.Snapshot<TenantProviderEnablementReadModel>(StoreName,
                        TenantProviderEnablementReadModelAddresses.Detail(request.Tenant))?.LastSequenceNumber ?? 0
                    : _store.Snapshot<ProviderCatalogReadModel>(StoreName,
                        ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId))?
                        .StreamSequences.GetValueOrDefault(request.AggregateId ?? string.Empty) ?? 0;
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, latest, 0, false, null));
            });
    }

    [Fact]
    public async Task Missing_command_status_without_a_receipt_is_unverifiable()
    {
        AgentOperationResult<AgentSetupWriteStatus> result = await Operations()
            .GetCommandOutcomeAsync(ProviderCatalogIdentity.PlatformTenantId, "unknown-message");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(AgentSetupWriteStatus.UnableToVerify);
    }

    [Fact]
    public async Task A_valid_submission_receipt_can_report_submitted_before_its_status_record_exists()
    {
        _gateway.GetCommandStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CommandStatusQueryResponse?>(null));

        AgentOperationResult<ProviderCatalogCommandAcceptance> write = await Operations().CreateEntryAsync(CreateCommand());

        write.IsSuccess.ShouldBeTrue();
        write.Value.ShouldNotBeNull().TruthState.ShouldBe(AgentSetupTruthState.Submitted);
        (await Operations().GetCommandOutcomeAsync(ProviderCatalogIdentity.PlatformTenantId,
            write.Value.MessageId)).Value.ShouldBe(AgentSetupWriteStatus.UnableToVerify);
    }

    [Theory]
    [InlineData("platform")]
    [InlineData("tenant")]
    public async Task Public_tenant_reads_remain_pending_when_a_committed_stream_is_ahead(string ahead)
    {
        SeedTenantDecisionView();
        TenantProviderCatalogInspectionResult current = (await Operations().ListTenantEntriesAsync(
            includeDisabled: true)).Value.ShouldNotBeNull();
        current.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        current.Entries.ShouldHaveSingleItem();

        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                long head = request.Domain == TenantProviderEnablementAggregate.Domain
                    ? ahead == "tenant" ? 2 : 1
                    : ahead == "platform" ? 2 : 1;
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, head, 0, false, null));
            });

        TenantProviderCatalogInspectionResult list = (await Operations().ListTenantEntriesAsync(
            includeDisabled: true)).Value.ShouldNotBeNull();
        TenantProviderCatalogInspectionResult detail = (await Operations().GetTenantEntryAsync(
            "openai", "gpt-4o")).Value.ShouldNotBeNull();

        list.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        detail.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        list.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        detail.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        list.Entries.ShouldBeEmpty();
        detail.Entries.ShouldBeEmpty();
        detail.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        list.ProjectedCommandMessageIds.ShouldBeNull();
        detail.ProjectedCommandMessageIds.ShouldBeNull();
    }

    [Fact]
    public async Task Rejected_create_with_projected_stream_but_no_row_is_authoritatively_absent()
    {
        string entryId = ProviderCatalogIdentity.EntryId("openai", "gpt-4o");
        _store.Seed(StoreName, ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId),
            new ProviderCatalogReadModel
            {
                CatalogId = ProviderCatalogIdentity.PlatformTenantId,
                TenantId = ProviderCatalogIdentity.PlatformTenantId,
                StreamSequences = new Dictionary<string, long>(StringComparer.Ordinal) { [entryId] = 1 },
            });

        ProviderCatalogInspectionResult result = (await Operations().GetEntryAsync("openai", "gpt-4o"))
            .Value.ShouldNotBeNull();

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);
        result.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        result.Freshness.ShouldBe(AgentSetupFreshness.Current);
        result.ProjectedCommandMessageIds.ShouldBeNull();

        ProviderCatalogInspectionResult waiting = (await Operations().GetEntryAsync("openai", "gpt-4o",
            expectedCapabilityVersion: 1)).Value.ShouldNotBeNull();
        waiting.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        waiting.Freshness.ShouldBe(AgentSetupFreshness.Stale);
    }

    [Theory]
    [InlineData("missing-stream", AgentSetupTruthState.ProjectionConfirmed)]
    [InlineData("other", AgentSetupTruthState.AuthoritativePending)]
    public async Task Named_missing_stream_is_current_only_for_exact_gateway_absence(
        string reason, AgentSetupTruthState expected)
    {
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<StreamReadPage>>(_ => throw new EventStoreGatewayException(404, "Not Found", reasonCode: reason));

        ProviderCatalogInspectionResult result = (await Operations().GetEntryAsync("openai", "gpt-4o")).Value.ShouldNotBeNull();

        result.Status.ShouldBe(reason == "missing-stream"
            ? ProviderCatalogInspectionStatus.EntryNotFound : ProviderCatalogInspectionStatus.Success);
        result.TruthState.ShouldBe(expected);
    }

    [Fact]
    public async Task Completed_platform_no_op_confirms_the_submitted_message_without_a_new_projection()
    {
        _gateway.GetCommandStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => new CommandStatusQueryResponse("corr", nameof(CommandStatus.Completed),
                (int)CommandStatus.Completed, MessageId: call.Arg<string>())
            {
                TenantId = ProviderCatalogIdentity.PlatformTenantId,
                EventCount = 0,
            });

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        result.Value.MessageId.ShouldBe(_submitted.ShouldHaveSingleItem().MessageId);
    }

    [Theory]
    [InlineData("Applied", AgentSetupTruthState.AuthoritativePending)]
    [InlineData("AlreadyApplied", AgentSetupTruthState.ProjectionConfirmed)]
    public async Task Governance_receipt_effect_controls_the_exact_submitted_write_truth(
        string effect, AgentSetupTruthState expectedTruth)
    {
        _gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                SubmitCommandRequest request = call.Arg<SubmitCommandRequest>();
                _submitted.Add(request);
                using JsonDocument payload = JsonDocument.Parse($$"""{"effect":"{{effect}}"}""");
                return new SubmitCommandResponse(request.CorrelationId!, payload.RootElement.Clone(), request.MessageId);
            });

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().TruthState.ShouldBe(expectedTruth);
        result.Value.MessageId.ShouldBe(_submitted.ShouldHaveSingleItem().MessageId);
        await _gateway.DidNotReceiveWithAnyArgs().GetCommandStatusAsync(default!, default);
    }

    [Fact]
    public async Task Non_string_governance_receipt_effect_is_unverifiable_without_throwing()
    {
        _gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                SubmitCommandRequest request = call.Arg<SubmitCommandRequest>();
                using JsonDocument payload = JsonDocument.Parse("""{"effect":42}""");
                return new SubmitCommandResponse(request.CorrelationId!, payload.RootElement.Clone(), request.MessageId);
            });

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.UnableToVerify);
        await _gateway.DidNotReceiveWithAnyArgs().GetCommandStatusAsync(default!, default);
    }

    [Fact]
    public async Task Operator_list_without_expected_version_remains_pending_when_known_heads_match()
    {
        SeedProjectedEntry(1);

        ProviderCatalogInspectionResult result = (await Operations().ListEntriesAsync(includeDisabled: true))
            .Value.ShouldNotBeNull();

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
    }

    [Fact]
    public async Task Rejected_platform_command_is_terminal_even_if_another_writer_advances_the_projection()
    {
        SeedProjectedEntry(2);
        _gateway.GetCommandStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => new CommandStatusQueryResponse("corr", nameof(CommandStatus.Rejected),
                (int)CommandStatus.Rejected, "InvalidProviderModelMetadataRejection", call.Arg<string>())
            {
                TenantId = ProviderCatalogIdentity.PlatformTenantId,
                EventCount = null,
            });

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Rejected);
        _submitted.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("infrastructure", AgentSetupWriteStatus.Unavailable)]
    [InlineData("unknown-rejection", AgentSetupWriteStatus.UnableToVerify)]
    [InlineData("retryable", AgentSetupWriteStatus.Submitted)]
    [InlineData("mismatched-status", AgentSetupWriteStatus.UnableToVerify)]
    public async Task Only_canonical_domain_rejections_are_reported_as_rejected(
        string scenario, AgentSetupWriteStatus expected)
    {
        _gateway.GetCommandStatusAsync("msg-outcome", Arg.Any<CancellationToken>())
            .Returns(new CommandStatusQueryResponse("corr",
                scenario == "mismatched-status" ? nameof(CommandStatus.Completed) : nameof(CommandStatus.Rejected),
                (int)CommandStatus.Rejected, MessageId: "msg-outcome")
            {
                TenantId = ProviderCatalogIdentity.PlatformTenantId,
                FailureReason = scenario == "infrastructure" ? "ConcurrencyConflict" : null,
                Retryable = scenario == "retryable",
            });

        AgentOperationResult<AgentSetupWriteStatus> result = await Operations()
            .GetCommandOutcomeAsync(ProviderCatalogIdentity.PlatformTenantId, "msg-outcome");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
        result.Value.ShouldNotBe(AgentSetupWriteStatus.Rejected);
    }

    [Fact]
    public async Task Completed_tenant_no_op_and_cross_tenant_status_are_scoped_to_the_command_target()
    {
        SeedProjectedEntry(1);
        _gateway.GetCommandStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => new CommandStatusQueryResponse("corr", nameof(CommandStatus.Completed),
                (int)CommandStatus.Completed, MessageId: call.Arg<string>())
            {
                TenantId = TenantId,
                EventCount = 0,
            });

        var command = new SetTenantProviderModelEnablement(TenantId, "openai", "gpt-4o", true, 0,
            SelectableEntry(1).DataHandling);
        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().SetTenantEnablementAsync(command);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        AgentOperationResult<AgentSetupWriteStatus> otherTenant = await Operations()
            .GetCommandOutcomeAsync("another-tenant", result.Value.MessageId);
        otherTenant.IsSuccess.ShouldBeFalse();
        otherTenant.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.NotAuthorized);
    }

    [Fact]
    public async Task Platform_enablement_read_reports_disabled_state_and_denies_tenant_admin_before_lookup()
    {
        SeedProjectedEntry(1);
        var tenant = new TenantProviderEnablementReadModel { ProjectionVersion = "2", LastSequenceNumber = 2 };
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-4o", false, 2,
            "operator", null));
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);

        TenantProviderEnablementInspectionResult visible = (await Operations()
            .GetTenantEnablementAsync(TenantId, "openai", "gpt-4o")).Value.ShouldNotBeNull();
        visible.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        visible.Enabled.ShouldBe(false);
        visible.Revision.ShouldBe(2);

        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "tenant-admin", IsAgentsAdmin: true));
        int priorGets = _store.GetCount;
        TenantProviderEnablementInspectionResult denied = (await Operations()
            .GetTenantEnablementAsync(TenantId, "openai", "gpt-4o")).Value.ShouldNotBeNull();
        denied.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        denied.Enabled.ShouldBeNull();
        _store.GetCount.ShouldBe(priorGets);
    }

    [Fact]
    public async Task Tenant_disable_dispatches_even_when_platform_terms_are_absent()
    {
        SeedProjectedEntry(1);
        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId)).ShouldNotBeNull();
        platform.Entries[0] = platform.Entries[0] with { DataHandling = null };
        _store.Seed(StoreName, ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), platform);
        var command = new SetTenantProviderModelEnablement(TenantId, "openai", "gpt-4o", false, 0, null);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().SetTenantEnablementAsync(command);

        result.IsSuccess.ShouldBeTrue();
        SubmitCommandRequest sent = _submitted.ShouldHaveSingleItem();
        sent.Tenant.ShouldBe(TenantId);
        sent.CommandType.ShouldBe(nameof(SetTenantProviderModelEnablement));
        JsonSerializer.Deserialize<SetTenantProviderModelEnablement>(sent.Payload.GetRawText()).ShouldNotBeNull()
            .CurrentTerms.ShouldBeNull();
    }

    [Fact]
    public async Task Tenant_disable_dispatches_during_platform_store_failure()
    {
        IReadModelStore failedStore = Substitute.For<IReadModelStore>();
        failedStore.GetAsync<ProviderCatalogReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReadModelEntry<ProviderCatalogReadModel>>(new InvalidOperationException("offline")));
        var command = new SetTenantProviderModelEnablement(TenantId, "openai", "gpt-4o", false, 2, null);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations(failedStore)
            .SetTenantEnablementAsync(command);

        result.IsSuccess.ShouldBeTrue();
        _submitted.ShouldHaveSingleItem().CommandType.ShouldBe(nameof(SetTenantProviderModelEnablement));
        await failedStore.DidNotReceive().GetAsync<ProviderCatalogReadModel>(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "gpt-4o")]
    [InlineData("openai", " ")]
    public async Task Blank_enablement_identifier_returns_validation_failure_before_lookup(
        string providerId, string modelId)
    {
        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations()
            .SetTenantEnablementAsync(new SetTenantProviderModelEnablement(TenantId,
                providerId, modelId, true, 0, null));

        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.ValidationFailed);
        _store.GetCount.ShouldBe(0);
        _submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task Tenant_enablement_distinguishes_a_missing_model_from_an_unavailable_store()
    {
        var command = new SetTenantProviderModelEnablement(TenantId, "openai", "gpt-4o", true, 0, null);
        AgentOperationResult<ProviderCatalogCommandAcceptance> missing = await Operations().SetTenantEnablementAsync(command);
        missing.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.NotFound);

        IReadModelStore failedStore = Substitute.For<IReadModelStore>();
        failedStore.GetAsync<ProviderCatalogReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReadModelEntry<ProviderCatalogReadModel>>(new InvalidOperationException("store offline")));

        AgentOperationResult<ProviderCatalogCommandAcceptance> unavailable = await Operations(failedStore)
            .SetTenantEnablementAsync(command);
        unavailable.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Unavailable);
        _submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task Tenant_enablement_waits_for_a_committed_platform_create_awaiting_projection()
    {
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, 1, 0, false, null));
            });

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().SetTenantEnablementAsync(
            new SetTenantProviderModelEnablement(TenantId, "openai", "gpt-4o", true, 0, null));

        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Unavailable);
        _submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task Operator_reads_and_enablement_inspection_wait_for_authoritative_stream_heads()
    {
        SeedProjectedEntry(1);
        var tenant = new TenantProviderEnablementReadModel { LastSequenceNumber = 1 };
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-4o", true, 1,
            "operator", null));
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, 2, 0, false, null));
            });

        ProviderCatalogInspectionResult list = (await Operations().ListEntriesAsync(true)).Value.ShouldNotBeNull();
        ProviderCatalogInspectionResult detail = (await Operations().GetEntryAsync("openai", "gpt-4o")).Value.ShouldNotBeNull();
        foreach (ProviderCatalogInspectionResult result in new[] { list, detail })
        {
            result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
            result.Entries.ShouldBeEmpty();
            result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        }

        TenantProviderEnablementInspectionResult enablement = (await Operations()
            .GetTenantEnablementAsync(TenantId, "openai", "gpt-4o")).Value.ShouldNotBeNull();
        enablement.Enabled.ShouldBeNull();
        enablement.Revision.ShouldBeNull();
        enablement.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
    }

    [Fact]
    public async Task Operator_enablement_inspection_waits_for_lagging_platform_terms_even_with_current_tenant_state()
    {
        SeedProjectedEntry(1);
        var tenant = new TenantProviderEnablementReadModel { LastSequenceNumber = 1 };
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-4o", true, 1,
            "operator", null));
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                long latest = request.Domain == ProviderCatalogAggregate.Domain ? 2 : 1;
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, latest, 0, false, null));
            });

        TenantProviderEnablementInspectionResult result = (await Operations()
            .GetTenantEnablementAsync(TenantId, "openai", "gpt-4o")).Value.ShouldNotBeNull();

        result.Enabled.ShouldBeNull();
        result.Revision.ShouldBeNull();
        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
    }

    [Fact]
    public async Task Public_tenant_enablement_rejects_migration_provenance_before_lookup_or_dispatch()
    {
        var command = new SetTenantProviderModelEnablement(TenantId, "openai", "gpt-4o", true, 0, null,
            MigratedFrom: "legacy:forged");

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().SetTenantEnablementAsync(command);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Blocked);
        _store.GetCount.ShouldBe(0);
        _submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task Authorized_decision_dispatches_exact_current_terms_with_tenant_actor_and_trusted_decision_time()
    {
        SeedTenantDecisionView();
        DateTimeOffset before = DateTimeOffset.UtcNow;
        ProviderDataHandlingRecord terms = SelectableEntry(1).DataHandling.ShouldNotBeNull();
        var decision = new DecideProviderDataHandling("openai", "gpt-4o", 1, true, "approved", 1, terms, default);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().DecideDataHandlingAsync(decision);

        DateTimeOffset after = DateTimeOffset.UtcNow;
        result.IsSuccess.ShouldBeTrue();
        SubmitCommandRequest sent = _submitted.ShouldHaveSingleItem();
        sent.Tenant.ShouldBe(TenantId);
        sent.AggregateId.ShouldBe(TenantId);
        sent.CommandType.ShouldBe(nameof(DecideProviderDataHandling));
        sent.Extensions.ShouldNotBeNull()["actor:tenantAgentAdministrator"].ShouldBe("true");
        DecideProviderDataHandling payload = JsonSerializer.Deserialize<DecideProviderDataHandling>(sent.Payload.GetRawText()).ShouldNotBeNull();
        ProviderDataHandlingPolicy.SameSnapshot(payload.ConfirmedTerms, terms).ShouldBeTrue();
        payload.DecidedAt.ShouldBeGreaterThanOrEqualTo(before);
        payload.DecidedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Theory]
    [InlineData("effective-at")]
    [InlineData("declaration")]
    [InlineData("null-regions")]
    public async Task Decision_rejects_an_altered_or_malformed_submitted_terms_snapshot(string change)
    {
        SeedTenantDecisionView();
        ProviderDataHandlingRecord terms = SelectableEntry(1).DataHandling.ShouldNotBeNull();
        ProviderDataHandlingRecord submitted = change switch
        {
            "effective-at" => terms with { EffectiveAt = DateTimeOffset.UtcNow },
            "declaration" => terms with { TighteningDeclaration = new ProviderDataHandlingTighteningDeclaration(
                0, 1, new ProviderDataHandlingFieldDiff(40, 30, false, false, [], [], "old", "terms-v1"),
                "operator", "PlatformOperator", DateTimeOffset.UtcNow) },
            _ => terms with { ProcessingRegions = null! },
        };
        var decision = new DecideProviderDataHandling("openai", "gpt-4o", 1, true, "approved", 1, submitted, default);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().DecideDataHandlingAsync(decision);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Stale);
        await _gateway.DidNotReceiveWithAnyArgs().SubmitCommandAsync(default!, default);
    }

    [Fact]
    public async Task Retained_key_sends_original_terms_for_authoritative_reconciliation_after_platform_terms_advance()
    {
        SeedTenantDecisionView();
        ProviderDataHandlingRecord original = SelectableEntry(1).DataHandling.ShouldNotBeNull();
        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId)).ShouldNotBeNull();
        platform.Entries[0] = platform.Entries[0] with
        {
            DataHandling = original with { DataHandlingVersion = 2, TermsReferenceId = "terms-v2" },
        };
        _store.Seed(StoreName, ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), platform);
        var decision = new DecideProviderDataHandling("openai", "gpt-4o", 1, true, "approved", 1, original, default);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().DecideDataHandlingAsync(
            decision, new AgentOperationOptions("corr-retry", "msg-original"));

        result.IsSuccess.ShouldBeTrue();
        SubmitCommandRequest sent = _submitted.ShouldHaveSingleItem();
        sent.MessageId.ShouldBe("msg-original");
        sent.IdempotencyKey.ShouldBe("msg-original");
        JsonSerializer.Deserialize<DecideProviderDataHandling>(sent.Payload.GetRawText()).ShouldNotBeNull()
            .ConfirmedTerms.DataHandlingVersion.ShouldBe(1);
    }

    [Fact]
    public async Task An_accepted_create_answers_with_a_structured_identity_and_the_submitted_stage()
    {
        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        result.IsSuccess.ShouldBeTrue();
        ProviderCatalogCommandAcceptance acceptance = result.Value.ShouldNotBeNull();
        acceptance.ProviderId.ShouldBe("openai");
        acceptance.ModelId.ShouldBe("gpt-4o");
        acceptance.MessageId.ShouldNotBeNullOrWhiteSpace();
        acceptance.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        acceptance.TruthState.ShouldBe(AgentSetupTruthState.Submitted);
    }

    [Theory]
    [InlineData("provenance")]
    [InlineData("historical-version")]
    public async Task Public_create_rejects_migration_only_fields(string kind)
    {
        CreateProviderModelEntry command = kind == "provenance"
            ? CreateCommand() with { MigratedFrom = string.Empty }
            : CreateCommand() with { InitialCapabilityVersion = 2 };

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(command);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Blocked);
        _submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unauthorized_create_is_denied_before_migration_field_validation()
    {
        _contextProvider.GetContext().Returns(AgentAdministrationContext.Anonymous);
        CreateProviderModelEntry command = CreateCommand() with { MigratedFrom = "legacy:forged" };

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(command);

        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.NotAuthorized);
        _submitted.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_acceptance_carries_no_eventstore_or_secret_internals()
    {
        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        string serialized = JsonSerializer.Serialize(result.Value);
        serialized.ShouldNotContain("revision", Case.Insensitive);
        serialized.ShouldNotContain("stream", Case.Insensitive);
        serialized.ShouldNotContain("aggregate", Case.Insensitive);
        serialized.ShouldNotContain("cfg-openai-gpt4o");
        serialized.ShouldNotContain("sk-");
    }

    [Fact]
    public async Task A_write_then_projection_then_read_reaches_projection_confirmed()
    {
        _ = await Operations().CreateEntryAsync(CreateCommand());

        ProviderCatalogInspectionResult before = (await Operations().GetEntryAsync("openai", "gpt-4o", expectedCapabilityVersion: 1))
            .Value
            .ShouldNotBeNull();
        before.Status.ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);
        before.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        before.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        before.Entries.ShouldBeEmpty();

        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult after = (await Operations().GetEntryAsync("openai", "gpt-4o", expectedCapabilityVersion: 1))
            .Value
            .ShouldNotBeNull();
        after.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        after.Entries.ShouldHaveSingleItem().Pricing.ShouldNotBeNull().Currency.ShouldBe("USD");
        after.Entries[0].IsSelectableForNewActiveUse.ShouldBeTrue();
    }

    [Fact]
    public async Task A_read_that_outruns_the_projection_is_authoritative_pending()
    {
        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult result = (await Operations().GetEntryAsync("openai", "gpt-4o", expectedCapabilityVersion: 2))
            .Value
            .ShouldNotBeNull();

        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        result.Entries.ShouldHaveSingleItem().CapabilityVersion.ShouldBe(1);
    }

    [Fact]
    public async Task A_list_waiting_on_a_newer_projection_version_is_authoritative_pending()
    {
        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult result = (await Operations().ListEntriesAsync(includeDisabled: true, expectedProjectionVersion: "9"))
            .Value
            .ShouldNotBeNull();

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        result.Entries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task An_unparseable_expected_projection_version_is_treated_as_behind()
    {
        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult result = (await Operations().ListEntriesAsync(includeDisabled: true, expectedProjectionVersion: "not-a-number"))
            .Value
            .ShouldNotBeNull();

        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
    }

    [Fact]
    public async Task An_unauthorized_write_does_not_dispatch()
    {
        _contextProvider.GetContext().Returns(AgentAdministrationContext.Anonymous);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.NotAuthorized);
        await _gateway.DidNotReceiveWithAnyArgs().SubmitCommandAsync(default!, default);
    }

    [Fact]
    public async Task Select_provider_model_stays_out_of_scope_on_the_administration_surface()
    {
        IAgentAdministrationOperations administration = new EventStoreAgentAdministrationOperations(
            _contextProvider,
            new AgentAdministrationOrchestrator(new EventStoreAgentCommandDispatcher(_gateway)),
            new AgentResponseModeOrchestrator(new EventStoreAgentCommandDispatcher(_gateway)),
            new AgentActivationProviderRevalidation(
                Substitute.For<IProviderCatalogReader>(),
                Substitute.For<IApproverPolicyResolver>(),
                new EventStoreAgentCommandDispatcher(_gateway)),
            _store,
            Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }),
            new AgentCommandIdentityFactory(),
            new DeferredAgentCommandStatusReader());

        AgentOperationResult result = await administration.SelectProviderModelAsync(
            new Hexalith.Agents.Contracts.Agent.Commands.SelectAgentProviderModel("openai", "gpt-4o", 1));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Unavailable);
    }

    private IProviderCatalogOperations Operations(IReadModelStore? store = null)
    {
        var dispatcher = new EventStoreAgentCommandDispatcher(_gateway);
        return new EventStoreProviderCatalogOperations(
            _contextProvider,
            new ProviderCatalogAdministrationOrchestrator(dispatcher),
            store ?? _store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }),
            new AgentCommandIdentityFactory(), gateway: _gateway);
    }

    private void SeedProjectedEntry(int capabilityVersion)
        => _store.Seed(
            StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId),
            new ProviderCatalogReadModel
            {
                CatalogId = ProviderCatalogIdentity.PlatformTenantId,
                TenantId = ProviderCatalogIdentity.PlatformTenantId,
                Entries = [SelectableEntry(capabilityVersion)],
                LastSequenceNumber = capabilityVersion,
                StreamSequences = new Dictionary<string, long>(StringComparer.Ordinal)
                {
                    [ProviderCatalogIdentity.EntryId("openai", "gpt-4o")] = 1,
                },
                ProjectedAt = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero),
                ProjectionVersion = capabilityVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });

    private void SeedTenantDecisionView()
    {
        SeedProjectedEntry(1);
        var tenant = new TenantProviderEnablementReadModel();
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-4o", true, 1,
            "operator", null));
        tenant.LastSequenceNumber = 1;
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);
    }

    private static CreateProviderModelEntry CreateCommand()
        => new(
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o",
            Enabled: true,
            SupportsTextGeneration: true,
            128_000,
            16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            "cfg-openai-gpt4o",
            new ProviderModelPricing("USD", 0.002m, 0.008m, 0),
            new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1));

    private static ProviderCatalogEntryView SelectableEntry(int capabilityVersion)
        => new(
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o",
            ProviderModelStatus.Enabled,
            SupportsTextGeneration: true,
            128_000,
            16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o",
            IsSelectableForNewActiveUse: true,
            capabilityVersion,
            new ProviderModelPricing("USD", 0.002m, 0.008m, 1),
            new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1));
}
