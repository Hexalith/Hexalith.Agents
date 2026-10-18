using System.Security.Claims;
using System.Text.Json;
using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.EventStore;
using Hexalith.Agents.Server.Api;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Local transport refusal evidence; no live owner seam, credential or readiness qualification is exercised.</summary>
public sealed class Story54BoundaryTests
{
    /// <summary>Verifies cancellation precedes admission effects and target construction even if admission ignores its token.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationStopsBeforeAdmissionOrTargetConstruction(bool cancelDuringAdmission)
    {
        using var cancellation = new CancellationTokenSource();
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        var intent = new AgentCommandIntent("logical", "claimed:tenant-a", "agent", "claimed:hexa", "DisableAgent", [], "correlation", null, "actor", null);
        admission.AdmitAsync(intent, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cancellation.Cancel();
            return intent;
        });
        if (!cancelDuringAdmission)
        {
            cancellation.Cancel();
        }

        OperationCanceledException error = await Should.ThrowAsync<OperationCanceledException>(
            () => new EventStoreAgentCommandDispatcher(gateway, admission).DispatchAsync(intent, cancellation.Token));

        error.CancellationToken.ShouldBe(cancellation.Token);
        gateway.ReceivedCalls().ShouldBeEmpty();
        if (!cancelDuringAdmission)
        {
            admission.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>Verifies that unavailable replay admission constructs no gateway target request.</summary>
    [Fact]
    public async Task UnavailableReplayAdmissionConstructsNoGatewayTargetRequest()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var dispatcher = new EventStoreAgentCommandDispatcher(gateway);
        // This identity would throw during CommandEnvelope construction. Dependency admission must happen first.
        var envelope = new AgentCommandIntent("logical", "claimed:tenant-a", "agent", "claimed:hexa", "DisableAgent", [], "correlation", null, "actor", null);
        var error = await Should.ThrowAsync<AgentDependencyNotAvailableException>(() => dispatcher.DispatchAsync(envelope, CancellationToken.None));
        error.RecordId.ShouldBe("EXT-SECRETS-1");
        gateway.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Verifies that a classified admission rejection precedes eager target identity validation.</summary>
    [Fact]
    public async Task RejectedAdmissionPrecedesTargetCommandConstruction()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        var denied = new InvalidOperationException("classified-denial-fixture");
        admission.AdmitAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AgentCommandIntent>(denied));
        var intent = new AgentCommandIntent("logical", "claimed:tenant-a", "agent", "claimed:hexa", "DisableAgent", [], "correlation", null, "actor", null);

        var error = await Should.ThrowAsync<InvalidOperationException>(
            () => new EventStoreAgentCommandDispatcher(gateway, admission).DispatchAsync(intent, CancellationToken.None));

        error.ShouldBeSameAs(denied);
        await admission.Received(1).AdmitAsync(intent, Arg.Any<CancellationToken>());
        gateway.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Verifies that even target identity validation waits for successful host admission.</summary>
    [Fact]
    public async Task TargetIdentityValidationRunsOnlyAfterAdmissionCompletes()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        var pending = new TaskCompletionSource<AgentCommandIntent>(TaskCreationOptions.RunContinuationsAsynchronously);
        admission.AdmitAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var intent = new AgentCommandIntent("logical", "claimed:tenant-a", "agent", "claimed:hexa", "DisableAgent", [], "correlation", null, "actor", null);

        Task<SubmitCommandResponse> dispatch = new EventStoreAgentCommandDispatcher(gateway, admission).DispatchAsync(intent, CancellationToken.None);
        dispatch.IsCompleted.ShouldBeFalse();
        gateway.ReceivedCalls().ShouldBeEmpty();
        pending.SetResult(intent);

        await Should.ThrowAsync<ArgumentException>(() => dispatch);
        gateway.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Verifies that payload and metadata cannot change during admission and nonce does not replace logical identity.</summary>
    [Fact]
    public async Task IntentBytesStayImmutableWhileAdmissionAwaitsAndDeliveryNonceRemainsSeparate()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var admission = Substitute.For<IAgentTrustedCommandAdmission>();
        var pending = new TaskCompletionSource<AgentCommandIntent>(TaskCreationOptions.RunContinuationsAsynchronously);
        admission.AdmitAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        byte[] bytes = "{\"value\":1}"u8.ToArray();
        var extensions = new Dictionary<string, string> { ["caller-metadata"] = "original" };
        var intent = new AgentCommandIntent("logical", "tenant-a", "agent", "hexa", "DisableAgent", bytes, "correlation", null, "actor", extensions);
        SubmitCommandRequest? submitted = null;
        gateway.SubmitCommandAsync(Arg.Do<SubmitCommandRequest>(value => submitted = value), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse("correlation", null, "logical"));

        Task<SubmitCommandResponse> dispatch = new EventStoreAgentCommandDispatcher(gateway, admission).DispatchAsync(intent, CancellationToken.None);
        bytes[9] = (byte)'2';
        extensions["caller-metadata"] = "changed";
        intent.Payload[9] = (byte)'3';
        intent.Extensions.ShouldNotBeNull()["caller-metadata"] = "changed-again";
        var admittedExtensions = intent.Extensions.ShouldNotBeNull();
        admittedExtensions["trust:deliveryNonce"] = "delivery-1";
        pending.SetResult(new AgentCommandIntent(intent.MessageId, intent.TenantId, intent.Domain, intent.AggregateId,
            intent.CommandType, intent.Payload, intent.CorrelationId, intent.CausationId, intent.UserId, admittedExtensions));

        await dispatch;
        submitted.ShouldNotBeNull().Payload.GetProperty("value").GetInt32().ShouldBe(1);
        submitted.Extensions.ShouldNotBeNull()["caller-metadata"].ShouldBe("original");
        submitted.Extensions["trust:deliveryNonce"].ShouldBe("delivery-1");
        submitted.MessageId.ShouldBe("logical");
        submitted.IdempotencyKey.ShouldBe("logical");
        intent.ToString().ShouldBe("AgentCommandIntent");
    }

    /// <summary>Verifies that authenticated dapr caller and unsigned flag grant no trusted extension.</summary>
    [Fact]
    public void AuthenticatedDaprCallerAndUnsignedFlagGrantNoTrustedExtension()
    {
        var services = new ServiceCollection();
        services.AddAgentsEventStore("agents");
        using var provider = services.BuildServiceProvider();
        var policy = provider.GetServices<ITrustedCommandExtensionPolicy>().Single();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("dapr_caller_app_id", "agents")], DaprInternalAuthenticationOptions.SchemeName));
        var request = new SubmitCommandRequest("message", "tenant-a", "agent", "hexa", "DisableAgent", JsonSerializer.SerializeToElement(new { }), "correlation");
        policy.Accepts(principal, request, AgentSetupTrustedExtensions.AgentAdministrator, "true").ShouldBeFalse();
    }

    /// <summary>Verifies that committed public api returns named dependency without calling the client or echoing claimed target.</summary>
    [Fact]
    public async Task CommittedPublicApiReturnsNamedDependencyWithoutCallingTheClientOrEchoingClaimedTarget()
    {
        var client = Substitute.For<IAgentsClient>();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(client);
        await using var app = builder.Build();
        app.MapAgentsOperationEndpoints();
        await app.StartAsync();
        using var http = app.GetTestClient();
        using var response = await http.GetAsync("/api/agents/operations/agents/foreign-agent/status");
        ((int)response.StatusCode).ShouldBe(503);
        string json = await response.Content.ReadAsStringAsync();
        json.ShouldContain("DependencyNotAvailable");
        json.ShouldContain("EXT-HOST-1");
        json.ShouldNotContain("foreign-agent");
        client.ReceivedCalls().ShouldBeEmpty();
    }
}
