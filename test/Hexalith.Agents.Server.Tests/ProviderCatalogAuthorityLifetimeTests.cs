using System.Text.Json;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Queries;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.TenantProviderEnablement;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Local current-authority lifetime evidence for provider reads, queries and preflight reads.</summary>
public sealed class ProviderCatalogAuthorityLifetimeTests
{
    private const string TenantId = "tenant-a";
    private const string ProviderId = "protected-provider";
    private const string ModelId = "model-primary";
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private readonly Story54TimeProvider _clock = new();
    private readonly IReadModelStore _store = Substitute.For<IReadModelStore>();
    private readonly IEventStoreGatewayClient _gateway = Substitute.For<IEventStoreGatewayClient>();
    private readonly IAgentAdministrationContextProvider _context = Substitute.For<IAgentAdministrationContextProvider>();
    private readonly ITenantAccessReader _access = Substitute.For<ITenantAccessReader>();
    private readonly IAgentCommandStatusReader _status = Substitute.For<IAgentCommandStatusReader>();
    private readonly List<string> _reads = [];

    /// <summary>Gets each protected read boundary with expiry/rollback failures and an advancing-clock control.</summary>
    public static TheoryData<string, string, string> ReadCases
    {
        get
        {
            var rows = new TheoryData<string, string, string>();
            foreach (string path in new[]
            {
                "platform-list-api", "platform-detail-api", "tenant-list-api", "tenant-detail-api",
                "platform-list-query", "platform-detail-query", "tenant-list-query", "tenant-detail-query",
                "entry-reader", "enablement-api", "status-platform-api", "status-tenant-api",
                "enablement-write", "decision-write",
            })
            {
                foreach (string boundary in Steps(path))
                {
                    rows.Add(path, boundary, "expiry");
                    rows.Add(path, boundary, "rollback");
                }
                rows.Add(path, "none", "current");
            }
            return rows;
        }
    }

    /// <summary>No later protected read or output follows expiry/rollback at any catalog boundary.</summary>
    [Theory]
    [MemberData(nameof(ReadCases))]
    public async Task ProtectedProviderPathsRetainAuthorityAcrossEveryRead(string path, string boundary, string mode)
    {
        ArgumentNullException.ThrowIfNull(path);
        Configure(boundary, mode);

        (ProviderCatalogInspectionStatus status, string output) = await ReadAsync(path);

        status.ShouldBe(mode == "current" ? ProviderCatalogInspectionStatus.Success : ProviderCatalogInspectionStatus.NotAuthorized);
        string[] expected = Steps(path);
        _reads.ShouldBe(mode == "current" ? expected : expected.Take(Array.IndexOf(expected, boundary) + 1));
        if (mode != "current")
        {
            output.ShouldNotContain(ProviderId);
            output.ShouldNotContain("configuration-reference");
            await _gateway.DidNotReceive().SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>());
        }
        else if (path.EndsWith("-write", StringComparison.Ordinal))
        {
            await _gateway.Received(1).SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>());
        }
    }

    /// <summary>A gateway error cannot conceal an authority failure and allow a later head read.</summary>
    [Theory]
    [InlineData("platform-list-api", "expiry")]
    [InlineData("platform-list-query", "rollback")]
    [InlineData("tenant-list-api", "expiry")]
    [InlineData("tenant-list-query", "rollback")]
    [InlineData("entry-reader", "expiry")]
    public async Task FailedFreshnessReadStillHonorsAuthorityLifetime(string path, string mode)
    {
        ArgumentNullException.ThrowIfNull(path);
        Configure("platform-head-primary", mode, failHead: true);

        (ProviderCatalogInspectionStatus status, string output) = await ReadAsync(path);

        status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        _reads.ShouldBe(Steps(path).Take(Array.IndexOf(Steps(path), "platform-head-primary") + 1));
        output.ShouldNotContain(ProviderId);
    }

    /// <summary>A delayed acknowledgement preserves receipt truth, without a post-expiry status disclosure.</summary>
    [Theory]
    [InlineData("Applied", AgentSetupTruthState.AuthoritativePending)]
    [InlineData("AlreadyApplied", AgentSetupTruthState.ProjectionConfirmed)]
    [InlineData(null, AgentSetupTruthState.Submitted)]
    public async Task AcceptedProviderAcknowledgementSurvivesAuthorityExpiry(string? effect, AgentSetupTruthState expected)
    {
        DateTimeOffset deadline = _clock.Now.AddSeconds(30);
        Configure("none", "current");
        _context.GetContextAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(call => new AgentAdministrationContext(
            call.Arg<bool>() ? "system" : TenantId, "stable-actor", !call.Arg<bool>(), call.Arg<bool>())
            { Authority = new AgentAuthorityValidity(_clock, _clock.Now, _clock.Now.AddSeconds(30)) });
        _gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            SubmitCommandRequest request = call.Arg<SubmitCommandRequest>();
            _clock.Now = deadline;
            return new SubmitCommandResponse(request.CorrelationId!, effect is null ? null : JsonSerializer.SerializeToElement(new { effect }), request.MessageId);
        });

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().DisableEntryAsync(
            new DisableProviderModelEntry(ProviderId, ModelId), new AgentOperationOptions(CorrelationId, MessageId));

        result.Value.ShouldNotBeNull().TruthState.ShouldBe(expected);
        result.Value.MessageId.ShouldBe(MessageId);
        _status.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Refreshing a nested context cannot extend the operation's originating authority deadline.</summary>
    [Theory]
    [InlineData("enable", false)]
    [InlineData("disable", false)]
    [InlineData("decision", false)]
    [InlineData("decision-retry", false)]
    [InlineData("enable", true)]
    [InlineData("disable", true)]
    [InlineData("decision", true)]
    [InlineData("decision-retry", true)]
    public async Task NestedWriteCannotRenewTheOriginalAuthority(string operation, bool current)
    {
        DateTimeOffset started = _clock.Now;
        Configure("none", "current");
        int contexts = 0;
        _context.GetContextAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(call => new AgentAdministrationContext(
            call.Arg<bool>() ? "system" : TenantId, "stable-actor", !call.Arg<bool>(), call.Arg<bool>())
            { Authority = new AgentAuthorityValidity(_clock, _clock.Now, started.AddSeconds(30 + 30 * contexts++)) });
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        admission.AdmitAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _clock.Now = started.AddSeconds(current ? 29 : 30);
            return call.Arg<AgentCommandIntent>();
        });
        var operations = new EventStoreProviderCatalogOperations(_context,
            new ProviderCatalogAdministrationOrchestrator(new EventStoreAgentCommandDispatcher(_gateway, admission)),
            _store, Options.Create(new ProviderCatalogReadModelOptions()), new AgentCommandIdentityFactory(), _clock, _gateway, _status);
        var options = new AgentOperationOptions(CorrelationId, operation == "decision" ? null : MessageId);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = operation is "enable" or "disable"
            ? await operations.SetTenantEnablementAsync(new SetTenantProviderModelEnablement(TenantId, ProviderId, ModelId,
                operation == "enable", 2, null), options)
            : await operations.DecideDataHandlingAsync(new DecideProviderDataHandling(ProviderId, ModelId, 1, true,
                "approved", 2, Terms(), default), options);

        result.IsSuccess.ShouldBe(current);
        if (!current)
        {
            result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.NotAuthorized);
            await _gateway.DidNotReceive().SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>());
        }
    }

    /// <summary>The eligibility timestamp itself becomes a checked observation before final disclosure.</summary>
    [Theory]
    [InlineData("tenant-detail-api", false)]
    [InlineData("tenant-detail-api", true)]
    [InlineData("tenant-detail-query", false)]
    [InlineData("tenant-detail-query", true)]
    [InlineData("entry-reader", false)]
    [InlineData("entry-reader", true)]
    public async Task EligibilityObservationCannotHideClockRollbackBeforeDisclosure(string path, bool rollback)
    {
        ArgumentNullException.ThrowIfNull(path);
        Configure("none", "current");
        var viewClock = Substitute.For<TimeProvider>();
        viewClock.GetUtcNow().Returns(_ =>
        {
            DateTimeOffset observed = _clock.Now.AddSeconds(2);
            _clock.Now = rollback ? observed.AddTicks(-1) : observed;
            return observed;
        });

        (ProviderCatalogInspectionStatus status, string output) = await ReadAsync(path, viewClock);

        status.ShouldBe(rollback ? ProviderCatalogInspectionStatus.NotAuthorized : ProviderCatalogInspectionStatus.Success);
        _reads.ShouldBe(Steps(path));
        if (rollback)
        {
            output.ShouldNotContain(ProviderId);
        }
    }

    /// <summary>Missing-stream responses cannot confirm absence after the original authorization interval fails.</summary>
    [Theory]
    [InlineData("expiry", false)]
    [InlineData("rollback", false)]
    [InlineData("current", true)]
    public async Task MissingStreamResponseRetainsAuthorityBeforeConfirmingAbsence(string mode, bool accepted)
    {
        DateTimeOffset started = _clock.Now;
        var bound = new AgentAuthorityValidity(_clock, started, started.AddSeconds(30));
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>()).Returns<Task<StreamReadPage>>(_ =>
        {
            _clock.Now = mode == "expiry" ? started.AddSeconds(30) : mode == "rollback" ? started.AddTicks(-1) : started.AddSeconds(1);
            return Task.FromException<StreamReadPage>(new EventStoreGatewayException(404, "missing",
                reasonCode: StreamReplayReasonCodes.MissingStream));
        });

        if (accepted)
        {
            (await ProviderCatalogReadFreshness.HasHeadAsync(_gateway, TenantId,
                TenantProviderEnablementAggregate.Domain, TenantId, 0, CancellationToken.None, bound)).ShouldBeTrue();
        }
        else
        {
            await Should.ThrowAsync<AgentCommandAuthorityExpiredException>(() => ProviderCatalogReadFreshness.HasHeadAsync(
                _gateway, TenantId, TenantProviderEnablementAggregate.Domain, TenantId, 0, CancellationToken.None, bound));
        }
        await _gateway.Received(1).ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>());
    }

    private void Configure(string boundary, string mode, bool failHead = false)
    {
        DateTimeOffset deadline = _clock.Now.AddSeconds(30);
        var bound = new AgentAuthorityValidity(_clock, _clock.Now, deadline);
        _context.GetContext().Returns(new AgentAdministrationContext(TenantId, "subject", false));
        _context.GetContextAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(call => new AgentAdministrationContext(
            call.Arg<bool>() ? "system" : TenantId, "stable-actor", !call.Arg<bool>(), call.Arg<bool>()) { Authority = bound });
        _access.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, true) { Authority = bound });
        var platform = new ProviderCatalogReadModel { TenantId = "system", CatalogId = "system", Entries = [Entry(ModelId), Entry("model-second")] };
        foreach (ProviderCatalogEntryView entry in platform.Entries)
        {
            platform.StreamSequences.Add(ProviderCatalogIdentity.EntryId(entry.ProviderId, entry.ModelId), 1);
        }
        var tenant = new TenantProviderEnablementReadModel { LastSequenceNumber = 2 };
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, ProviderId, ModelId, true, 1, "operator", null));
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, ProviderId, "model-second", true, 2, "operator", null));
        _store.GetAsync<ProviderCatalogReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("platform-model");
            return new ReadModelEntry<ProviderCatalogReadModel>(platform, "platform-etag");
        });
        _store.GetAsync<TenantProviderEnablementReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("tenant-model");
            return new ReadModelEntry<TenantProviderEnablementReadModel>(tenant, "tenant-etag");
        });
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>()).Returns<Task<StreamReadPage>>(call =>
        {
            StreamReadRequest request = call.Arg<StreamReadRequest>();
            string step = request.Domain == TenantProviderEnablementAggregate.Domain ? "tenant-head"
                : request.AggregateId == ProviderCatalogIdentity.EntryId(ProviderId, ModelId) ? "platform-head-primary" : "platform-head-second";
            Complete(step);
            if (failHead && step == boundary)
            {
                return Task.FromException<StreamReadPage>(new EventStoreGatewayException(503, "private-head-error"));
            }
            return Task.FromResult(new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                new StreamReadMetadata(0, null, null, step == "tenant-head" ? 2 : 1, 0, false, null)));
        });
        _status.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("status");
            return (CommandStatusQueryResponse?)null;
        });
        _gateway.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            SubmitCommandRequest request = call.Arg<SubmitCommandRequest>();
            return new SubmitCommandResponse(request.CorrelationId!, JsonSerializer.SerializeToElement(new { effect = "Applied" }), request.MessageId);
        });
        void Complete(string step)
        {
            _reads.Add(step);
            _clock.Now = step == boundary ? mode == "expiry" ? deadline : _clock.Now.AddTicks(-1) : _clock.Now.AddSeconds(1);
        }
    }

    private async Task<(ProviderCatalogInspectionStatus Status, string Output)> ReadAsync(string path, TimeProvider? viewClock = null)
    {
        if (path.EndsWith("-query", StringComparison.Ordinal))
        {
            bool platform = path.StartsWith("platform-", StringComparison.Ordinal);
            bool list = path.Contains("-list-", StringComparison.Ordinal);
            ProviderCatalogQueryHandlerBase handler = list
                ? new ListProviderCatalogEntriesQueryHandler(_store, Options.Create(new ProviderCatalogReadModelOptions()), _access, _context, _gateway, viewClock ?? _clock)
                : new GetProviderCatalogEntryQueryHandler(_store, Options.Create(new ProviderCatalogReadModelOptions()), _access, _context, _gateway, viewClock ?? _clock);
            object payload = list ? new ListProviderCatalogEntriesQuery(true) : new GetProviderCatalogEntryQuery(ProviderId, ModelId);
            QueryResult wire = await handler.ExecuteAsync(new QueryEnvelope(platform ? "system" : TenantId, ProviderCatalogAggregate.Domain,
                ProviderCatalogIdentity.EntryId(ProviderId, ModelId), handler.QueryType, JsonSerializer.SerializeToUtf8Bytes(payload),
                CorrelationId, "subject", isGlobalAdmin: platform), CancellationToken.None);
            string output = System.Text.Encoding.UTF8.GetString(wire.PayloadBytes.ShouldNotBeNull());
            using JsonDocument json = JsonDocument.Parse(output);
            return (Enum.Parse<ProviderCatalogInspectionStatus>(json.RootElement.GetProperty("status").GetString().ShouldNotBeNull()), output);
        }
        if (path == "entry-reader")
        {
            var result = await new ProjectedProviderCatalogReader(_store, Options.Create(new ProviderCatalogReadModelOptions()),
                _context, _gateway, viewClock ?? _clock).GetEntryAsync(TenantId, ProviderId, ModelId, CancellationToken.None);
            return (result.Status, JsonSerializer.Serialize(result));
        }
        EventStoreProviderCatalogOperations operations = Operations(viewClock);
        if (path.StartsWith("status-", StringComparison.Ordinal))
        {
            var result = await operations.GetCommandWriteOutcomeAsync(path == "status-platform-api" ? "system" : TenantId, MessageId);
            return (result.Error?.Code == AgentOperationErrorCode.NotAuthorized ? ProviderCatalogInspectionStatus.NotAuthorized : ProviderCatalogInspectionStatus.Success,
                JsonSerializer.Serialize(result));
        }
        if (path == "enablement-api")
        {
            var result = await operations.GetTenantEnablementAsync(TenantId, ProviderId, ModelId);
            return (result.Error?.Code == AgentOperationErrorCode.NotAuthorized ? ProviderCatalogInspectionStatus.NotAuthorized : result.Value.ShouldNotBeNull().Status,
                JsonSerializer.Serialize(result));
        }
        if (path.EndsWith("-write", StringComparison.Ordinal))
        {
            var result = path == "enablement-write"
                ? await operations.SetTenantEnablementAsync(new SetTenantProviderModelEnablement(TenantId, ProviderId, ModelId, true, 2, null), new AgentOperationOptions(CorrelationId, MessageId))
                : await operations.DecideDataHandlingAsync(new DecideProviderDataHandling(ProviderId, ModelId, 1, true, "approved", 2, Terms(), default), new AgentOperationOptions(CorrelationId));
            return (result.Error?.Code == AgentOperationErrorCode.NotAuthorized ? ProviderCatalogInspectionStatus.NotAuthorized : result.IsSuccess ? ProviderCatalogInspectionStatus.Success : ProviderCatalogInspectionStatus.Unavailable,
                JsonSerializer.Serialize(result));
        }
        if (path.StartsWith("platform-", StringComparison.Ordinal))
        {
            var result = path == "platform-list-api" ? await operations.ListEntriesAsync(true) : await operations.GetEntryAsync(ProviderId, ModelId);
            return (result.Error?.Code == AgentOperationErrorCode.NotAuthorized ? ProviderCatalogInspectionStatus.NotAuthorized : result.Value.ShouldNotBeNull().Status,
                JsonSerializer.Serialize(result));
        }
        else
        {
            var result = path == "tenant-list-api" ? await operations.ListTenantEntriesAsync(true) : await operations.GetTenantEntryAsync(ProviderId, ModelId);
            return (result.Error?.Code == AgentOperationErrorCode.NotAuthorized ? ProviderCatalogInspectionStatus.NotAuthorized : result.Value.ShouldNotBeNull().Status,
                JsonSerializer.Serialize(result));
        }
    }

    private EventStoreProviderCatalogOperations Operations(TimeProvider? viewClock = null) => new(_context,
        new ProviderCatalogAdministrationOrchestrator(new EventStoreAgentCommandDispatcher(_gateway, new FixtureTrustedCommandAdmission())),
        _store, Options.Create(new ProviderCatalogReadModelOptions()), new AgentCommandIdentityFactory(), viewClock ?? _clock, _gateway, _status);

    private static ProviderDataHandlingRecord Terms() => new(30, false, ["EU"], "terms-v1", 1);

    private static ProviderCatalogEntryView Entry(string modelId) => new(ProviderId, modelId, "protected-label", ProviderModelStatus.Enabled,
        true, 128000, 16000, new ProviderModelTimeoutPolicy(30000, 3), ProviderModelCapabilityFlags.Streaming,
        ProviderConfigurationState.Configured, "configuration-reference", true, 1, new ProviderModelPricing("USD", 1, 2, 1), Terms());

    private static string[] Steps(string path) => path switch
    {
        "platform-list-api" or "platform-list-query" => ["platform-model", "platform-head-primary", "platform-head-second"],
        "platform-detail-api" or "platform-detail-query" or "enablement-write" => ["platform-model", "platform-head-primary"],
        "tenant-list-api" or "tenant-list-query" => ["platform-model", "tenant-model", "tenant-head", "platform-head-primary", "platform-head-second"],
        "tenant-detail-api" or "tenant-detail-query" or "entry-reader" or "decision-write" => ["platform-model", "tenant-model", "tenant-head", "platform-head-primary"],
        "enablement-api" => ["tenant-model", "platform-model", "tenant-head", "platform-head-primary"],
        "status-platform-api" or "status-tenant-api" => ["status"],
        _ => throw new ArgumentOutOfRangeException(nameof(path)),
    };
}
