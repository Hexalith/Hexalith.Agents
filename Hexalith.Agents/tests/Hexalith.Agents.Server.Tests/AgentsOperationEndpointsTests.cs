namespace Hexalith.Agents.Server.Tests;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Server.Api;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using Shouldly;

/// <summary>
/// Public operations API/BFF contract tests for Story 4.1.
/// </summary>
public sealed class AgentsOperationEndpointsTests
{
    private static readonly string[] _poisonValues =
    [
        "DeferredAgentCommandDispatcher",
        "System.InvalidOperationException",
        "stack trace",
        "provider secret",
        "raw provider payload",
        "other-tenant-id",
    ];

    [Fact]
    public void Operation_endpoints_register_stable_route_area()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(AgentsClient.Unavailable());

        WebApplication app = builder.Build();
        app.MapAgentsOperationEndpoints();

        string[] patterns = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .ToArray();

        patterns.ShouldContain("/api/agents/operations/providers/");
        patterns.ShouldContain("/api/agents/operations/agents/{agentId}/status");
        patterns.ShouldContain("/api/agents/operations/interactions/{agentInteractionId}/status");
        patterns.ShouldContain("/api/agents/operations/interactions/{agentInteractionId}/generation-evidence");
        patterns.ShouldContain("/api/agents/operations/proposals/{agentInteractionId}");
        patterns.ShouldContain("/api/agents/operations/status/interactions/{agentInteractionId}/audit");
        patterns.ShouldContain("/api/agents/operations/audit/interactions/{agentInteractionId}/posting");
        patterns.ShouldContain("/api/agents/operations/audit/interactions/{agentInteractionId}/proposal-approval");
    }

    [Fact]
    public async Task Operation_status_endpoint_returns_client_result_json()
    {
        IAgentsClient client = Substitute.For<IAgentsClient>();
        IAgentStatusOperations status = Substitute.For<IAgentStatusOperations>();
        status.GetAgentReadinessAsync("agent-1", Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentReadinessStatus>>(
                AgentOperationResult<AgentReadinessStatus>.Succeeded(AgentReadinessStatus.Callable)));
        client.Status.Returns(status);

        await using WebApplication app = BuildApp(client);
        string json = await InvokeEndpointAsync(
            app,
            "/api/agents/operations/status/agents/{agentId}/readiness",
            ("agentId", "agent-1")).ConfigureAwait(true);

        json.ShouldContain("\"status\":\"Succeeded\"");
        json.ShouldContain("\"value\":\"Callable\"");
        json.ShouldNotContain("\"value\":\"Checking\"");
    }

    [Fact]
    public async Task Operation_status_endpoint_preserves_validation_failure_shape()
    {
        IAgentsClient client = Substitute.For<IAgentsClient>();
        IAgentStatusOperations status = Substitute.For<IAgentStatusOperations>();
        status.GetCallStatusAsync("interaction-1", Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentCallOperationStatus>>(
                AgentOperationResult<AgentCallOperationStatus>.Failed(AgentOperationErrorCode.ValidationFailed)));
        client.Status.Returns(status);

        await using WebApplication app = BuildApp(client);
        string json = await InvokeEndpointAsync(
            app,
            "/api/agents/operations/status/interactions/{agentInteractionId}/call",
            ("agentInteractionId", "interaction-1")).ConfigureAwait(true);

        json.ShouldContain("\"status\":\"ValidationFailed\"");
        json.ShouldContain("\"code\":\"ValidationFailed\"");
        json.ShouldContain("\"message\":\"The operation request is invalid.\"");
    }

    [Fact]
    public async Task Operation_status_endpoint_does_not_leak_internal_failure_text()
    {
        await using WebApplication app = BuildApp(AgentsClient.Unavailable());
        string json = await InvokeEndpointAsync(
            app,
            "/api/agents/operations/status/interactions/{agentInteractionId}/audit",
            ("agentInteractionId", "interaction-1")).ConfigureAwait(true);

        json.ShouldContain("\"status\":\"Unavailable\"");
        json.ShouldContain("\"code\":\"Unavailable\"");
        foreach (string poison in _poisonValues)
        {
            json.ShouldNotContain(poison, Case.Insensitive);
        }
    }

    [Fact]
    public async Task Default_operation_client_maps_deferred_paths_to_structured_unavailable()
    {
        IAgentsClient client = AgentsClient.Unavailable();

        AgentOperationResult<AuditAvailabilityStatus> result =
            await client.Status.GetAuditAvailabilityAsync("interaction-1");

        result.Status.ShouldBe(AgentOperationStatus.Unavailable);
        result.Error.ShouldNotBeNull();
        result.Error.Message.ShouldNotContain("DeferredAgentCommandDispatcher", Case.Sensitive);
        result.Error.Message.ShouldNotContain("StackTrace", Case.Sensitive);
    }

    private static WebApplication BuildApp(IAgentsClient client)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(client);

        WebApplication app = builder.Build();
        app.MapAgentsOperationEndpoints();

        return app;
    }

    private static async Task<string> InvokeEndpointAsync(
        WebApplication app,
        string routePattern,
        params (string Key, string Value)[] routeValues)
    {
        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern);

        await using MemoryStream body = new();
        DefaultHttpContext context = new()
        {
            RequestServices = app.Services,
            Response =
            {
                Body = body,
            },
        };

        context.Request.Method = HttpMethods.Get;
        foreach ((string key, string value) in routeValues)
        {
            context.Request.RouteValues[key] = value;
        }

        await endpoint.RequestDelegate!(context).ConfigureAwait(true);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);

        body.Position = 0;
        using StreamReader reader = new(body);
        return await reader.ReadToEndAsync().ConfigureAwait(true);
    }
}
