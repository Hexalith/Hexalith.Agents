namespace Hexalith.Agents.Server.Tests;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
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
        // Story 4.4 — launch-readiness record/enable commands + the launch-readiness status read.
        patterns.ShouldContain("/api/agents/operations/agents/launch-readiness");
        patterns.ShouldContain("/api/agents/operations/agents/enable-production-like-generation");
        patterns.ShouldContain("/api/agents/operations/status/agents/{agentId}/launch-readiness");
    }

    [Fact]
    public void Story_5_2_administration_routes_name_their_target_agent_in_the_path()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(AgentsClient.Unavailable());

        WebApplication app = builder.Build();
        app.MapAgentsOperationEndpoints();

        (string Pattern, string Method)[] routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => (
                Pattern: endpoint.RoutePattern.RawText ?? string.Empty,
                Method: endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods[0] ?? string.Empty))
            .ToArray();

        // The aggregate identity is routing information, never something the command body may assert (AC4).
        routes.ShouldContain(("/api/agents/operations/agents/{agentId}", HttpMethods.Post));
        routes.ShouldContain(("/api/agents/operations/agents/{agentId}", HttpMethods.Put));
        routes.ShouldContain(("/api/agents/operations/agents/{agentId}/response-mode", HttpMethods.Post));
        routes.ShouldContain(("/api/agents/operations/agents/{agentId}/activate", HttpMethods.Post));
        routes.ShouldContain(("/api/agents/operations/agents/{agentId}/disable", HttpMethods.Post));
        routes.ShouldContain(("/api/agents/operations/agents/{agentId}/status", HttpMethods.Get));
        routes.ShouldContain(("/api/agents/operations/agents/{agentId}/configuration", HttpMethods.Get));
    }

    [Fact]
    public async Task The_status_read_forwards_the_expected_configuration_version_to_the_client()
    {
        IAgentAdministrationOperations administration = StubbedSetupReads();

        await using WebApplication app = BuildApp(AgentsClientWith(administration));
        _ = await InvokeEndpointAsync(
            app,
            "/api/agents/operations/agents/{agentId}/status",
            queryString: "?expectedConfigurationVersion=7",
            ("agentId", "agent-1")).ConfigureAwait(true);

        // Without the version reaching the client the caller can never tell submitted from projection-confirmed.
        await administration.Received(1).GetStatusAsync("agent-1", 7, Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_configuration_read_forwards_the_expected_configuration_version_to_the_client()
    {
        IAgentAdministrationOperations administration = StubbedSetupReads();

        await using WebApplication app = BuildApp(AgentsClientWith(administration));
        _ = await InvokeEndpointAsync(
            app,
            "/api/agents/operations/agents/{agentId}/configuration",
            queryString: "?expectedConfigurationVersion=7",
            ("agentId", "agent-1")).ConfigureAwait(true);

        await administration.Received(1).GetConfigurationAsync("agent-1", 7, Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_catalog_list_forwards_the_expected_projection_version_to_the_client()
    {
        IProviderCatalogOperations catalog = StubbedCatalogReads();

        await using WebApplication app = BuildApp(AgentsClientWith(catalog));
        _ = await InvokeEndpointAsync(
            app,
            "/api/agents/operations/providers/",
            queryString: "?includeDisabled=true&expectedProjectionVersion=9").ConfigureAwait(true);

        await catalog.Received(1).ListEntriesAsync(true, "9", Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_catalog_get_forwards_the_expected_capability_version_to_the_client()
    {
        IProviderCatalogOperations catalog = StubbedCatalogReads();

        await using WebApplication app = BuildApp(AgentsClientWith(catalog));
        _ = await InvokeEndpointAsync(
            app,
            "/api/agents/operations/providers/{providerId}/{modelId}",
            queryString: "?expectedCapabilityVersion=2",
            ("providerId", "openai"),
            ("modelId", "gpt-4o")).ConfigureAwait(true);

        await catalog.Received(1).GetEntryAsync("openai", "gpt-4o", 2, Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_setup_read_without_a_version_asks_for_the_currently_projected_truth()
    {
        IAgentAdministrationOperations administration = StubbedSetupReads();

        await using WebApplication app = BuildApp(AgentsClientWith(administration));
        string json = await InvokeEndpointAsync(
            app,
            "/api/agents/operations/agents/{agentId}/configuration",
            queryString: null,
            ("agentId", "agent-1")).ConfigureAwait(true);

        await administration.Received(1).GetConfigurationAsync("agent-1", null, Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>());

        // A read the client could not satisfy still carries no setup payload over the wire (AC4).
        json.ShouldContain("\"setup\":null");
    }

    private static IAgentAdministrationOperations StubbedSetupReads()
    {
        IAgentAdministrationOperations administration = Substitute.For<IAgentAdministrationOperations>();
        administration
            .GetStatusAsync("agent-1", Arg.Any<int?>(), Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentSetupResult>>(
                AgentOperationResult<AgentSetupResult>.Succeeded(AgentSetupResult.NotFound())));
        administration
            .GetConfigurationAsync("agent-1", Arg.Any<int?>(), Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentSetupResult>>(
                AgentOperationResult<AgentSetupResult>.Succeeded(AgentSetupResult.NotFound())));
        return administration;
    }

    private static IAgentsClient AgentsClientWith(IAgentAdministrationOperations administration)
    {
        IAgentsClient client = Substitute.For<IAgentsClient>();
        client.AgentAdministration.Returns(administration);
        return client;
    }

    private static IAgentsClient AgentsClientWith(IProviderCatalogOperations catalog)
    {
        IAgentsClient client = Substitute.For<IAgentsClient>();
        client.ProviderCatalog.Returns(catalog);
        return client;
    }

    private static IProviderCatalogOperations StubbedCatalogReads()
    {
        IProviderCatalogOperations catalog = Substitute.For<IProviderCatalogOperations>();
        catalog
            .ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>>(
                AgentOperationResult<ProviderCatalogInspectionResult>.Succeeded(ProviderCatalogInspectionResult.NotFound())));
        catalog
            .GetEntryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>>(
                AgentOperationResult<ProviderCatalogInspectionResult>.Succeeded(ProviderCatalogInspectionResult.NotFound())));
        return catalog;
    }

    [Fact]
    public async Task Launch_readiness_status_endpoint_returns_client_result_json()
    {
        var view = new AgentLaunchReadinessView(
            [],
            [],
            CostControlPosture.Budgets,
            LaunchReadinessVersion: 1,
            HasContentSafetyPolicy: true,
            HasContextPolicy: true,
            ProductionLikeGenerationEnabled: false,
            [AgentLaunchReadinessBlocker.MissingLaunchMetrics]);
        IAgentsClient client = Substitute.For<IAgentsClient>();
        IAgentStatusOperations status = Substitute.For<IAgentStatusOperations>();
        status.GetAgentLaunchReadinessAsync("agent-1", Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentLaunchReadinessView>>(
                AgentOperationResult<AgentLaunchReadinessView>.Succeeded(view)));
        client.Status.Returns(status);

        await using WebApplication app = BuildApp(client);
        string json = await InvokeEndpointAsync(
            app,
            "/api/agents/operations/status/agents/{agentId}/launch-readiness",
            ("agentId", "agent-1")).ConfigureAwait(true);

        json.ShouldContain("\"status\":\"Succeeded\"");
        json.ShouldContain("MissingLaunchMetrics");
    }

    [Fact]
    public async Task Launch_readiness_status_endpoint_fails_closed_without_leaking_internal_text()
    {
        await using WebApplication app = BuildApp(AgentsClient.Unavailable());
        string json = await InvokeEndpointAsync(
            app,
            "/api/agents/operations/status/agents/{agentId}/launch-readiness",
            ("agentId", "agent-1")).ConfigureAwait(true);

        json.ShouldContain("\"status\":\"Unavailable\"");
        json.ShouldContain("\"code\":\"Unavailable\"");
        foreach (string poison in _poisonValues)
        {
            json.ShouldNotContain(poison, Case.Insensitive);
        }
    }

    [Fact]
    public async Task Default_client_maps_launch_readiness_commands_to_structured_unavailable_never_success()
    {
        IAgentsClient client = AgentsClient.Unavailable();

        AgentOperationResult record = await client.AgentAdministration.RecordLaunchReadinessAsync(
            new RecordAgentLaunchReadiness(new AgentLaunchReadiness([], [], CostControlPosture.Budgets, null, "full-conversation-v1")));
        AgentOperationResult enable = await client.AgentAdministration.EnableProductionLikeGenerationAsync(new EnableProductionLikeGeneration());

        // Fail-closed: a deferred binding is Unavailable, never Succeeded — pending/blocked is never success (AD-12).
        record.Status.ShouldBe(AgentOperationStatus.Unavailable);
        enable.Status.ShouldBe(AgentOperationStatus.Unavailable);
        record.Error.ShouldNotBeNull();
        record.Error.Message.ShouldNotContain("DeferredAgentCommandDispatcher", Case.Sensitive);
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

    private static Task<string> InvokeEndpointAsync(
        WebApplication app,
        string routePattern,
        params (string Key, string Value)[] routeValues)
        => InvokeEndpointAsync(app, routePattern, queryString: null, routeValues);

    private static async Task<string> InvokeEndpointAsync(
        WebApplication app,
        string routePattern,
        string? queryString,
        params (string Key, string Value)[] routeValues)
    {
        RouteEndpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern
                && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(HttpMethods.Get) == true);

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
        if (queryString is { Length: > 0 })
        {
            context.Request.QueryString = new QueryString(queryString);
        }

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
