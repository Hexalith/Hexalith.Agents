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
    /// <summary>Verifies that unavailable replay admission constructs no gateway target request.</summary>
    [Fact]
    public async Task UnavailableReplayAdmissionConstructsNoGatewayTargetRequest()
    {
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        var dispatcher = new EventStoreAgentCommandDispatcher(gateway);
        var envelope = new CommandEnvelope("logical", "tenant-a", "agent", "hexa", "DisableAgent", [], "correlation", null, "actor", null);
        var error = await Should.ThrowAsync<AgentDependencyNotAvailableException>(() => dispatcher.DispatchAsync(envelope, CancellationToken.None));
        error.RecordId.ShouldBe("EXT-SECRETS-1");
        gateway.ReceivedCalls().ShouldBeEmpty();
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
