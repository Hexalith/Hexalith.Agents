using System.Net;

using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Verifies exact authenticated status evidence through the public SDK HTTP transport.</summary>
public sealed class EventStoreAgentCommandStatusReaderTests
{
    private const string Tenant = "tenant-a";
    private const string Domain = "agent";
    private const string Message = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private readonly IEventStoreGatewayWorkloadAssertionSource _source = Substitute.For<IEventStoreGatewayWorkloadAssertionSource>();
    private readonly CommandStatusResponseHandler _handler = new();

    public EventStoreAgentCommandStatusReaderTests()
    {
        _source.IssueAsync(EventStoreGatewayWorkloadOperations.CommandStatus, Tenant, Domain, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("independent-host-assertion"));
        _handler.Status = Rejected();
    }

    [Fact]
    public async Task ExactDomainRejectionUsesWorkloadRouteAndExplicitDomainDelegation()
    {
        (await Reader().WasRejectedAsync(Tenant, Domain, Message, Correlation, TestContext.Current.CancellationToken)).ShouldBe(true);
        _handler.Path.ShouldBe($"/api/v1/commands/status/workload/{Tenant}/{Message}");
        _handler.Assertion.ShouldBe("independent-host-assertion");
        await _source.Received(1).IssueAsync(EventStoreGatewayWorkloadOperations.CommandStatus, Tenant, Domain,
            Arg.Any<CancellationToken>());
        await _source.DidNotReceive().IssueAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("domain")]
    [InlineData("message")]
    [InlineData("correlation")]
    [InlineData("legacy-domain")]
    [InlineData("status-name")]
    [InlineData("status-code")]
    [InlineData("unknown")]
    [InlineData("infrastructure")]
    [InlineData("retryable")]
    [InlineData("completed")]
    [InlineData("absent-rejection")]
    public async Task ForeignUnknownAndInfrastructureEvidenceCannotClaimRejection(string scenario)
    {
        CommandStatusQueryResponse status = Rejected();
        _handler.Status = scenario switch
        {
            "tenant" => status with { TenantId = "foreign" },
            "domain" => status with { Domain = "foreign" },
            "message" => status with { MessageId = "foreign" },
            "correlation" => status with { CorrelationId = "foreign" },
            "legacy-domain" => status with { Domain = null },
            "status-name" => status with { Status = "rejected" },
            "status-code" => status with { StatusCode = (int)CommandStatus.Completed },
            "unknown" => status with { Status = "Future", StatusCode = 12345 },
            "infrastructure" => status with { FailureReason = "infrastructure" },
            "retryable" => status with { Retryable = true },
            "completed" => status with { Status = nameof(CommandStatus.Completed), StatusCode = (int)CommandStatus.Completed },
            _ => status with { RejectionEventType = null },
        };
        (await Reader().WasRejectedAsync(Tenant, Domain, Message, Correlation, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Theory]
    [InlineData("missing-source")]
    [InlineData("missing-credential")]
    [InlineData("credential-failure")]
    [InlineData("transport")]
    [InlineData("timeout")]
    [InlineData("forbidden")]
    [InlineData("missing-status")]
    public async Task CredentialAndTransportFailuresRemainUnverifiable(string failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        if (failure == "missing-credential")
        {
            _source.IssueAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult<string?>(null));
        }
        if (failure == "credential-failure")
        {
            _source.IssueAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns<ValueTask<string?>>(_ => throw new InvalidOperationException("issuer unavailable"));
        }
        if (failure == "transport") { _handler.Failure = new HttpRequestException("unreachable"); }
        if (failure == "timeout") { _handler.Failure = new TaskCanceledException("timeout"); }
        if (failure == "forbidden") { _handler.ResponseCode = HttpStatusCode.Forbidden; }
        if (failure == "missing-status") { _handler.ResponseCode = HttpStatusCode.NotFound; }
        (await Reader(failure != "missing-source").WasRejectedAsync(Tenant, Domain, Message, Correlation,
            TestContext.Current.CancellationToken)).ShouldBeNull();
        if (failure.StartsWith("missing-", StringComparison.Ordinal) && failure != "missing-status" || failure == "credential-failure")
        {
            _handler.Requests.ShouldBe(0);
        }
    }


    [Fact]
    public async Task LegacyMessageOnlyResultsCannotBecomeScopedEvidence()
    {
        var legacy = new LegacyMessageOnlyAgentCommandStatusReader();
        IAgentCommandStatusReader reader = legacy;
        (await reader.WasRejectedAsync(Message, TestContext.Current.CancellationToken)).ShouldBe(true);
        (await reader.WasRejectedAsync(Tenant, Domain, Message, Correlation, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await reader.ReadAsync(Tenant, Domain, Message, Correlation, TestContext.Current.CancellationToken)).ShouldBeNull();
        legacy.MessageReads.ShouldBe(1);
    }

    [Fact]
    public async Task CancellationPropagatesBeforeDispatchAndAfterNonCooperativeStatusRead()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => Reader().WasRejectedAsync(Tenant, Domain, Message, Correlation, canceled.Token));
        _handler.Requests.ShouldBe(0);
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        using var duringRead = new CancellationTokenSource();
        gateway.GetWorkloadCommandStatusAsync(Tenant, Message, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            duringRead.Cancel();
            return Rejected();
        });
        await Should.ThrowAsync<OperationCanceledException>(() => new EventStoreAgentCommandStatusReader(_ => gateway)
            .WasRejectedAsync(Tenant, Domain, Message, Correlation, duringRead.Token));
    }

    [Fact]
    public async Task MessageOnlyReadsAndUndeclaredDelegateOperationsCannotObtainCredentials()
    {
        (await Reader().WasRejectedAsync(Message, TestContext.Current.CancellationToken)).ShouldBeNull();
        _handler.Requests.ShouldBe(0);
        var adapter = new CommandStatusWorkloadAssertionSource(_source, Domain);
        (await adapter.IssueAsync(EventStoreGatewayWorkloadOperations.CommandSubmit, Tenant, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await adapter.IssueAsync(EventStoreGatewayWorkloadOperations.CommandStatus, Tenant, "foreign", TestContext.Current.CancellationToken)).ShouldBeNull();
        _source.ReceivedCalls().ShouldBeEmpty();
    }

    private EventStoreAgentCommandStatusReader Reader(bool credentials = true)
        => new(domain => new EventStoreGatewayClient(new HttpClient(_handler, disposeHandler: false) { BaseAddress = new Uri("https://gateway.example/") },
            Options.Create(new EventStoreGatewayClientOptions()), new CommandStatusWorkloadAssertionSource(credentials ? _source : null, domain)));

    private static CommandStatusQueryResponse Rejected()
        => new(Correlation, nameof(CommandStatus.Rejected), (int)CommandStatus.Rejected, "InvalidAgentConfigurationRejection", Message)
        { TenantId = Tenant, Domain = Domain };
}
