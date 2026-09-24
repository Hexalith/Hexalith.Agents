namespace Hexalith.Agents.Server.Tests;

using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Server.Api;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using Shouldly;

/// <summary>
/// Public operations API/BFF contract tests for Story 4.1.
/// </summary>
public sealed class AgentsOperationEndpointsTests
{
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

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
    public async Task Every_setup_write_binds_correlation_and_idempotency_headers_to_command_options()
    {
        IAgentAdministrationOperations administration = Substitute.For<IAgentAdministrationOperations>();
        var acceptance = new AgentCommandAcceptance(
            "agent-1",
            MessageId,
            CorrelationId,
            AgentSetupTruthState.AuthoritativePending,
            AgentSetupWriteEffect.Applied,
            4);
        AgentOperationResult<AgentCommandAcceptance> accepted =
            AgentOperationResult<AgentCommandAcceptance>.Succeeded(acceptance);
        administration
            .CreateAsync(
                "agent-1",
                Arg.Any<CreateAgent>(),
                Arg.Any<AgentOperationOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentCommandAcceptance>>(accepted));
        administration
            .UpdateConfigurationAsync(
                "agent-1",
                Arg.Any<UpdateAgentConfiguration>(),
                Arg.Any<AgentOperationOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentCommandAcceptance>>(accepted));
        administration
            .ConfigureResponseModeAsync(
                "agent-1",
                Arg.Any<ConfigureAgentResponseMode>(),
                Arg.Any<AgentOperationOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentCommandAcceptance>>(accepted));
        administration
            .ActivateAsync(
                "agent-1",
                Arg.Any<ActivateAgent>(),
                Arg.Any<AgentOperationOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentCommandAcceptance>>(accepted));
        administration
            .DisableAsync(
                "agent-1",
                Arg.Any<DisableAgent>(),
                Arg.Any<AgentOperationOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<AgentCommandAcceptance>>(accepted));

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(AgentsClientWith(administration));
        await using WebApplication app = builder.Build();
        app.MapAgentsOperationEndpoints();
        await app.StartAsync().ConfigureAwait(true);
        using HttpClient client = app.GetTestClient();
        await SendWriteAsync(
            client,
            HttpMethod.Post,
            "/api/agents/operations/agents/agent-1",
            new CreateAgent("tenant-from-body", "hexa", null, "instructions long enough to be valid"));
        await SendWriteAsync(
            client,
            HttpMethod.Put,
            "/api/agents/operations/agents/agent-1",
            new UpdateAgentConfiguration("hexa", null, "instructions long enough to be valid"));
        await SendWriteAsync(
            client,
            HttpMethod.Post,
            "/api/agents/operations/agents/agent-1/response-mode",
            new ConfigureAgentResponseMode(AgentResponseMode.Confirmation));
        await SendWriteAsync(
            client,
            HttpMethod.Post,
            "/api/agents/operations/agents/agent-1/activate",
            new ActivateAgent());
        await SendWriteAsync(
            client,
            HttpMethod.Post,
            "/api/agents/operations/agents/agent-1/disable",
            new DisableAgent());

        await administration.Received(1).CreateAsync(
            "agent-1",
            Arg.Any<CreateAgent>(),
            Arg.Is<AgentOperationOptions?>(options => HasExpectedCommandOptions(options)),
            Arg.Any<CancellationToken>());
        await administration.Received(1).UpdateConfigurationAsync(
            "agent-1",
            Arg.Any<UpdateAgentConfiguration>(),
            Arg.Is<AgentOperationOptions?>(options => HasExpectedCommandOptions(options)),
            Arg.Any<CancellationToken>());
        await administration.Received(1).ConfigureResponseModeAsync(
            "agent-1",
            Arg.Any<ConfigureAgentResponseMode>(),
            Arg.Is<AgentOperationOptions?>(options => HasExpectedCommandOptions(options)),
            Arg.Any<CancellationToken>());
        await administration.Received(1).ActivateAsync(
            "agent-1",
            Arg.Any<ActivateAgent>(),
            Arg.Is<AgentOperationOptions?>(options => HasExpectedCommandOptions(options, 3)),
            Arg.Any<CancellationToken>());
        await administration.Received(1).DisableAsync(
            "agent-1",
            Arg.Any<DisableAgent>(),
            Arg.Is<AgentOperationOptions?>(options => HasExpectedCommandOptions(options)),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Idempotency-Key", " ")]
    [InlineData("Idempotency-Key", "77ca7f293a3347b287f963befc1bf125")]
    [InlineData("X-Correlation-ID", "01arz3ndektsv4rrffq69g5fav")]
    public async Task Explicit_noncanonical_setup_identity_headers_return_stable_problem_details(
        string headerName,
        string headerValue)
    {
        IAgentAdministrationOperations administration = Substitute.For<IAgentAdministrationOperations>();
        await using WebApplication app = BuildHttpApp(AgentsClientWith(administration));
        await app.StartAsync().ConfigureAwait(true);
        using HttpClient client = app.GetTestClient();

        foreach ((HttpMethod method, string path, object command) in SetupWriteRequests())
        {
            using var request = new HttpRequestMessage(method, path)
            {
                Content = JsonContent.Create(command),
            };
            request.Headers.TryAddWithoutValidation(headerName, headerValue).ShouldBeTrue();
            if (path.EndsWith("/activate", StringComparison.Ordinal))
            {
                request.Headers.Add("X-Expected-Configuration-Version", "3");
            }

            using HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(true);

            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest, path);
            response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json", path);
            string problem = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            problem.ShouldContain(AgentSetupCommandHeadersFilter.InvalidIdentityProblemType);
            problem.ShouldContain("canonical_value_required");
            problem.ShouldContain(headerName);
        }

        await AssertNoSetupWriteWasInvokedAsync(administration);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("+3")]
    [InlineData("03")]
    public async Task Activation_requires_one_positive_canonical_configuration_version_header(string? version)
    {
        IAgentAdministrationOperations administration = Substitute.For<IAgentAdministrationOperations>();
        await using WebApplication app = BuildHttpApp(AgentsClientWith(administration));
        await app.StartAsync().ConfigureAwait(true);
        using HttpClient client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/agents/operations/agents/agent-1/activate")
        {
            Content = JsonContent.Create(new ActivateAgent()),
        };
        request.Headers.Add("X-Correlation-ID", CorrelationId);
        request.Headers.Add("Idempotency-Key", MessageId);
        if (version is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Expected-Configuration-Version", version).ShouldBeTrue();
        }

        using HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(true);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        string problem = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        problem.ShouldContain(AgentSetupCommandHeadersFilter.InvalidActivationVersionProblemType);
        problem.ShouldContain("canonical_value_required");
        await administration.DidNotReceiveWithAnyArgs().ActivateAsync(
            default!,
            default!,
            default,
            default);
    }

    [Theory]
    [InlineData("X-Correlation-ID", CorrelationId)]
    [InlineData("Idempotency-Key", MessageId)]
    public async Task Duplicate_canonical_setup_identity_headers_return_stable_problem_details_on_every_write_route(
        string duplicateHeader,
        string canonicalValue)
    {
        IAgentAdministrationOperations administration = Substitute.For<IAgentAdministrationOperations>();
        await using WebApplication app = BuildHttpApp(AgentsClientWith(administration));
        await app.StartAsync().ConfigureAwait(true);
        using HttpClient client = app.GetTestClient();

        foreach ((HttpMethod method, string path, object command) in SetupWriteRequests())
        {
            using var request = new HttpRequestMessage(method, path)
            {
                Content = JsonContent.Create(command),
            };
            request.Headers.TryAddWithoutValidation(
                "X-Correlation-ID",
                duplicateHeader == "X-Correlation-ID" ? [canonicalValue, canonicalValue] : [CorrelationId]).ShouldBeTrue();
            request.Headers.TryAddWithoutValidation(
                "Idempotency-Key",
                duplicateHeader == "Idempotency-Key" ? [canonicalValue, canonicalValue] : [MessageId]).ShouldBeTrue();
            if (path.EndsWith("/activate", StringComparison.Ordinal))
            {
                request.Headers.Add("X-Expected-Configuration-Version", "3");
            }

            using HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(true);

            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest, path);
            response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json", path);
            string problem = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            problem.ShouldContain(AgentSetupCommandHeadersFilter.InvalidIdentityProblemType);
            problem.ShouldContain("canonical_value_required");
            problem.ShouldContain(duplicateHeader);
        }

        await AssertNoSetupWriteWasInvokedAsync(administration);
    }

    [Fact]
    public async Task Duplicate_activation_version_header_returns_stable_problem_details_without_invoking_the_operation()
    {
        IAgentAdministrationOperations administration = Substitute.For<IAgentAdministrationOperations>();
        await using WebApplication app = BuildHttpApp(AgentsClientWith(administration));
        await app.StartAsync().ConfigureAwait(true);
        using HttpClient client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/agents/operations/agents/agent-1/activate")
        {
            Content = JsonContent.Create(new ActivateAgent()),
        };
        request.Headers.Add("X-Correlation-ID", CorrelationId);
        request.Headers.Add("Idempotency-Key", MessageId);
        request.Headers.TryAddWithoutValidation("X-Expected-Configuration-Version", ["3", "3"]).ShouldBeTrue();

        using HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(true);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        string problem = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        problem.ShouldContain(AgentSetupCommandHeadersFilter.InvalidActivationVersionProblemType);
        problem.ShouldContain("canonical_value_required");
        problem.ShouldContain("X-Expected-Configuration-Version");
        await administration.DidNotReceiveWithAnyArgs().ActivateAsync(
            default!,
            default!,
            default,
            default);
    }

    [Fact]
    public void Setup_write_handlers_declare_both_optional_retry_headers_for_endpoint_metadata()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(AgentsClient.Unavailable());
        WebApplication app = builder.Build();
        app.MapAgentsOperationEndpoints();

        string[] writePatterns =
        [
            "/api/agents/operations/agents/{agentId}",
            "/api/agents/operations/agents/{agentId}/response-mode",
            "/api/agents/operations/agents/{agentId}/activate",
            "/api/agents/operations/agents/{agentId}/disable",
        ];
        RouteEndpoint[] endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => writePatterns.Contains(endpoint.RoutePattern.RawText))
            .ToArray();

        endpoints.Length.ShouldBe(5);
        foreach (RouteEndpoint endpoint in endpoints)
        {
            MethodInfo handler = endpoint.Metadata.OfType<MethodInfo>().Single();
            string[] headerNames = handler
                .GetParameters()
                .Select(parameter => parameter.GetCustomAttribute<FromHeaderAttribute>())
                .Where(attribute => attribute is not null)
                .Select(attribute => attribute!.Name!)
                .ToArray();

            headerNames.ShouldContain("X-Correlation-ID", endpoint.RoutePattern.RawText);
            headerNames.ShouldContain("Idempotency-Key", endpoint.RoutePattern.RawText);
            if (endpoint.RoutePattern.RawText?.EndsWith("/activate", StringComparison.Ordinal) == true)
            {
                headerNames.ShouldContain("X-Expected-Configuration-Version", endpoint.RoutePattern.RawText);
            }
            else
            {
                headerNames.ShouldNotContain("X-Expected-Configuration-Version", endpoint.RoutePattern.RawText);
            }
        }
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
    public async Task Tenant_decision_http_route_forwards_retry_and_correlation_headers()
    {
        IProviderCatalogOperations catalog = Substitute.For<IProviderCatalogOperations>();
        catalog.DecideDataHandlingAsync(Arg.Any<DecideProviderDataHandling>(),
                Arg.Any<AgentOperationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>>(
                AgentOperationResult<ProviderCatalogCommandAcceptance>.Succeeded(
                    new("openai", "gpt-4o", MessageId, CorrelationId, AgentSetupTruthState.Submitted))));
        await using WebApplication app = BuildHttpApp(AgentsClientWith(catalog));
        await app.StartAsync();
        var terms = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1);
        var decision = new DecideProviderDataHandling("openai", "gpt-4o", 1, true, "approved", 1, terms, default);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            "/api/agents/operations/providers/tenant/data-handling")
        {
            Content = JsonContent.Create(decision),
        };
        request.Headers.Add("X-Correlation-ID", CorrelationId);
        request.Headers.Add("Idempotency-Key", MessageId);

        using HttpResponseMessage response = await app.GetTestClient().SendAsync(request);

        response.EnsureSuccessStatusCode();
        await catalog.Received(1).DecideDataHandlingAsync(Arg.Any<DecideProviderDataHandling>(),
            Arg.Is<AgentOperationOptions?>(options => options != null
                && options.CorrelationId == CorrelationId && options.IdempotencyKey == MessageId),
            Arg.Any<CancellationToken>());
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

    private static bool HasExpectedCommandOptions(
        AgentOperationOptions? options,
        int? expectedConfigurationVersion = null)
        => options is
        {
            IdempotencyKey: MessageId,
            CorrelationId: CorrelationId,
        }
        && options.ExpectedConfigurationVersion == expectedConfigurationVersion;

    private static async Task SendWriteAsync(HttpClient client, HttpMethod method, string path, object command)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(command),
        };
        request.Headers.Add("Idempotency-Key", MessageId);
        request.Headers.Add("X-Correlation-ID", CorrelationId);
        if (path.EndsWith("/activate", StringComparison.Ordinal))
        {
            request.Headers.Add("X-Expected-Configuration-Version", "3");
        }

        using HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();
    }

    private static (HttpMethod Method, string Path, object Command)[] SetupWriteRequests()
        =>
        [
            (HttpMethod.Post, "/api/agents/operations/agents/agent-1", new CreateAgent("tenant-from-body", "hexa", null, "instructions long enough to be valid")),
            (HttpMethod.Put, "/api/agents/operations/agents/agent-1", new UpdateAgentConfiguration("hexa", null, "instructions long enough to be valid")),
            (HttpMethod.Post, "/api/agents/operations/agents/agent-1/response-mode", new ConfigureAgentResponseMode(AgentResponseMode.Confirmation)),
            (HttpMethod.Post, "/api/agents/operations/agents/agent-1/activate", new ActivateAgent()),
            (HttpMethod.Post, "/api/agents/operations/agents/agent-1/disable", new DisableAgent()),
        ];

    private static async Task AssertNoSetupWriteWasInvokedAsync(IAgentAdministrationOperations administration)
    {
        await administration.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default, default);
        await administration.DidNotReceiveWithAnyArgs().UpdateConfigurationAsync(default!, default!, default, default);
        await administration.DidNotReceiveWithAnyArgs().ConfigureResponseModeAsync(default!, default!, default, default);
        await administration.DidNotReceiveWithAnyArgs().ActivateAsync(default!, default!, default, default);
        await administration.DidNotReceiveWithAnyArgs().DisableAsync(default!, default!, default, default);
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

    private static WebApplication BuildHttpApp(IAgentsClient client)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
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
