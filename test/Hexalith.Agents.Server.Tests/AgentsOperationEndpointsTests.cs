namespace Hexalith.Agents.Server.Tests;

using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Server.Api;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
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
    public void ReturnedBuilderAppliesAuthorizationAndCustomConventionsToBothRouteFamilies()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>(Substitute.For<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>());
        builder.Services.AddSingleton(AgentsClient.Unavailable());
        WebApplication app = builder.Build();
        app.MapAgentsOperationEndpoints().RequireAuthorization("operators").WithMetadata("shared-operation-convention");
        RouteEndpoint[] routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().ToArray();
        routes.ShouldContain(endpoint => endpoint.RoutePattern.RawText!.StartsWith("/api/v1/agents/operations", StringComparison.Ordinal));
        routes.ShouldContain(endpoint => endpoint.RoutePattern.RawText!.StartsWith("/api/agents/operations", StringComparison.Ordinal));
        foreach (RouteEndpoint endpoint in routes)
        {
            endpoint.Metadata.GetMetadata<IAuthorizeData>()!.Policy.ShouldBe("operators");
            endpoint.Metadata.GetMetadata<string>().ShouldBe("shared-operation-convention");
        }
    }

    [Theory]
    [InlineData(AgentSetupTruthState.Submitted, 202)]
    [InlineData(AgentSetupTruthState.AuthoritativePending, 202)]
    [InlineData(AgentSetupTruthState.ProjectionConfirmed, 200)]
    [InlineData(AgentSetupTruthState.Unknown, 500)]
    public async Task ActualWriteRoutesMapAcceptanceTruthStages(AgentSetupTruthState truth, int expected)
    {
        IAgentsClient client = Substitute.For<IAgentsClient>();
        IAgentAdministrationOperations administration = Substitute.For<IAgentAdministrationOperations>();
        IProviderCatalogOperations catalog = Substitute.For<IProviderCatalogOperations>();
        var agentAcceptance = new AgentCommandAcceptance("agent-1", MessageId, CorrelationId, truth,
            truth == AgentSetupTruthState.ProjectionConfirmed ? AgentSetupWriteEffect.AlreadyApplied : AgentSetupWriteEffect.Applied, 1);
        administration.CreateAsync(default!, default!, default, default).ReturnsForAnyArgs(
            new ValueTask<AgentOperationResult<AgentCommandAcceptance>>(
                AgentOperationResult<AgentCommandAcceptance>.Succeeded(agentAcceptance, correlationId: CorrelationId)));
        catalog.EnableEntryAsync(default!, default, default).ReturnsForAnyArgs(
            new ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>>(
                AgentOperationResult<ProviderCatalogCommandAcceptance>.Succeeded(
                    new("provider", "model", MessageId, CorrelationId, truth), correlationId: CorrelationId)));
        client.AgentAdministration.Returns(administration);
        client.ProviderCatalog.Returns(catalog);
        await using WebApplication app = BuildHttpApp(client);
        await app.StartAsync();
        foreach ((string path, object command) in new (string, object)[]
        {
            ("/api/agents/operations/agents/agent-1", new CreateAgent("tenant", "hexa", null, "instructions")),
            ("/api/agents/operations/providers/enable", new EnableProviderModelEntry("provider", "model", 1)),
        })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(command, command.GetType()) };
            request.Headers.Add("X-Correlation-ID", CorrelationId);
            request.Headers.Add("Idempotency-Key", MessageId);
            using HttpResponseMessage response = await app.GetTestClient().SendAsync(request);
            ((int)response.StatusCode).ShouldBe(expected, path);
            string body = await response.Content.ReadAsStringAsync();
            body.ShouldContain("\"truthState\":\"" + truth + "\"");
            body.ShouldContain(CorrelationId);
        }
    }

    [Theory]
    [InlineData(AgentProposalApprovalOutcome.Approved, 202)]
    [InlineData(AgentProposalApprovalOutcome.IdempotentPostingPending, 202)]
    [InlineData(AgentProposalApprovalOutcome.Posted, 200)]
    [InlineData(AgentProposalApprovalOutcome.IdempotentPosted, 200)]
    public async Task ApprovalRouteKeepsPostingPendingUntilConfirmed(AgentProposalApprovalOutcome outcome, int expected)
    {
        IAgentsClient client = Substitute.For<IAgentsClient>();
        IProposalWorkflowOperations proposals = Substitute.For<IProposalWorkflowOperations>();
        var result = new AgentProposalApprovalResult(outcome, "proposal", "conversation", "version", "party", 1,
            ApproverPolicyValidationStatus.Valid, ApproverPolicyBasisDisclosure.OperatorOnly, "agent-party", "message", "retry", "posted-message");
        proposals.ApproveAsync(default!, default, default).ReturnsForAnyArgs(new ValueTask<AgentOperationResult<AgentProposalApprovalResult>>(
            AgentOperationResult<AgentProposalApprovalResult>.Succeeded(result, correlationId: CorrelationId)));
        client.ProposalWorkflow.Returns(proposals);
        await using WebApplication app = BuildHttpApp(client);
        await app.StartAsync();
        using HttpResponseMessage response = await app.GetTestClient().PostAsJsonAsync(
            "/api/agents/operations/proposals/approve", new ApproveProposedAgentReply("interaction", result));
        ((int)response.StatusCode).ShouldBe(expected);
        (await response.Content.ReadFromJsonAsync<AgentOperationResult<AgentProposalApprovalResult>>())!.CorrelationId.ShouldBe(CorrelationId);
    }

    [Theory]
    [InlineData(AgentInspectionStatus.Success, 200)]
    [InlineData(AgentInspectionStatus.AgentNotFound, 404)]
    [InlineData(AgentInspectionStatus.NotAuthorized, 403)]
    [InlineData(AgentInspectionStatus.Unavailable, 503)]
    public async Task SetupReadMapsNestedStatusAndPreservesBody(AgentInspectionStatus status, int expected)
    {
        IAgentAdministrationOperations administration = Substitute.For<IAgentAdministrationOperations>();
        var agent = new AgentStatusView("agent-1", "tenant", "hexa", null, AgentLifecycleStatus.Draft, 1,
            false, false, 0, false, false, null, null, AgentResponseMode.Unknown, false,
            ApproverPolicyBasisDisclosure.Unknown, 0, false, 0, false, false, [], [], false);
        AgentSetupView? view = status == AgentInspectionStatus.Success
            ? new(agent, 1, "1", null, AgentSetupFreshness.Current, AgentSetupTruthState.ProjectionConfirmed) : null;
        administration.GetStatusAsync(default!, default, default, default).ReturnsForAnyArgs(new ValueTask<AgentOperationResult<AgentSetupResult>>(
            AgentOperationResult<AgentSetupResult>.Succeeded(new(status, view), correlationId: CorrelationId)));
        await using WebApplication app = BuildHttpApp(AgentsClientWith(administration));
        await app.StartAsync();
        using HttpResponseMessage response = await app.GetTestClient().GetAsync("/api/agents/operations/agents/agent-1/status");
        ((int)response.StatusCode).ShouldBe(expected);
        var body = (await response.Content.ReadFromJsonAsync<AgentOperationResult<AgentSetupResult>>())!;
        body.Value!.Status.ShouldBe(status);
        body.CorrelationId.ShouldBe(CorrelationId);
        (body.Value.Setup is not null).ShouldBe(status == AgentInspectionStatus.Success);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("01arz3ndektsv4rrffq69g5faw")]
    public async Task LegacyOutcomeRouteDoesNotEchoMalformedCorrelationOrSuccessValue(string correlation)
    {
        await using WebApplication app = BuildHttpApp(AgentsClient.Unavailable());
        await app.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/agents/operations/providers/commands/system/message-1");
        request.Headers.Add("X-Correlation-ID", correlation);
        using HttpResponseMessage response = await app.GetTestClient().SendAsync(request);
        ((int)response.StatusCode).ShouldBe(422);
        string json = await response.Content.ReadAsStringAsync();
        json.ShouldNotContain(correlation);
        json.ShouldNotContain("Submitted");
        json.ShouldNotContain("\"value\"");
    }

    [Theory]
    [InlineData(AgentCallRequestStatus.Accepted, 202)]
    [InlineData(AgentCallRequestStatus.NotAuthorized, 403)]
    [InlineData(AgentCallRequestStatus.Rejected, 422)]
    [InlineData(AgentCallRequestStatus.Unknown, 500)]
    public async Task InteractionRequestMapsItsTypedOperationOutcome(AgentCallRequestStatus status, int expected)
    {
        IAgentsClient client = Substitute.For<IAgentsClient>();
        IAgentInteractionOperations operations = Substitute.For<IAgentInteractionOperations>();
        operations.RequestAsync(default!, default, default).ReturnsForAnyArgs(
            new ValueTask<AgentOperationResult<AgentCallRequestResult>>(
                AgentOperationResult<AgentCallRequestResult>.Succeeded(new(status,
                    status == AgentCallRequestStatus.Accepted ? new("interaction-1", AgentInteractionStatus.Requested) : null),
                    correlationId: CorrelationId)));
        client.AgentInteractions.Returns(operations);
        await using WebApplication app = BuildHttpApp(client);
        await app.StartAsync();
        using HttpResponseMessage response = await app.GetTestClient().PostAsJsonAsync(
            "/api/agents/operations/interactions/", new RequestAgentInteraction("hexa", "conversation-1", "party-1", "prompt", "retry", null));
        ((int)response.StatusCode).ShouldBe(expected);
        (await response.Content.ReadFromJsonAsync<AgentOperationResult<AgentCallRequestResult>>())!
            .CorrelationId.ShouldBe(CorrelationId);
    }

    [Theory]
    [InlineData(AgentProposalRejectionOutcome.Rejected, 200)]
    [InlineData(AgentProposalRejectionOutcome.AlreadyTerminal, 200)]
    [InlineData(AgentProposalRejectionOutcome.NotAuthorized, 403)]
    [InlineData(AgentProposalRejectionOutcome.PolicyFailure, 503)]
    [InlineData(AgentProposalRejectionOutcome.ProposalNotPending, 409)]
    [InlineData(AgentProposalRejectionOutcome.Unknown, 500)]
    public async Task ProposalRejectionDistinguishesSuccessfulRejectionFromOperationFailure(AgentProposalRejectionOutcome outcome, int expected)
    {
        IAgentsClient client = Substitute.For<IAgentsClient>();
        IProposalWorkflowOperations operations = Substitute.For<IProposalWorkflowOperations>();
        var result = new AgentProposalRejectionResult(outcome, "proposal-1", "conversation-1", "party-1", 1,
            ApproverPolicyValidationStatus.Valid, ApproverPolicyBasisDisclosure.OperatorOnly, null);
        operations.RejectAsync(default!, default, default).ReturnsForAnyArgs(
            new ValueTask<AgentOperationResult<AgentProposalRejectionResult>>(
                AgentOperationResult<AgentProposalRejectionResult>.Succeeded(result, correlationId: CorrelationId)));
        client.ProposalWorkflow.Returns(operations);
        await using WebApplication app = BuildHttpApp(client);
        await app.StartAsync();
        using HttpResponseMessage response = await app.GetTestClient().PostAsJsonAsync(
            "/api/agents/operations/proposals/reject", new RejectProposedAgentReply("interaction-1", result));
        ((int)response.StatusCode).ShouldBe(expected);
        (await response.Content.ReadFromJsonAsync<AgentOperationResult<AgentProposalRejectionResult>>())!
            .CorrelationId.ShouldBe(CorrelationId);
    }

    [Theory]
    [InlineData(ProposalOperationStatus.Rejected)]
    [InlineData(ProposalOperationStatus.PostingPending)]
    [InlineData(ProposalOperationStatus.PostingFailed)]
    public async Task ReadingAProposalResourceStateDoesNotTurnItIntoAnOperationFailure(ProposalOperationStatus state)
    {
        IAgentsClient client = Substitute.For<IAgentsClient>();
        IAgentStatusOperations status = Substitute.For<IAgentStatusOperations>();
        status.GetProposalStatusAsync(default!, default, default).ReturnsForAnyArgs(
            new ValueTask<AgentOperationResult<ProposalOperationStatus>>(
                AgentOperationResult<ProposalOperationStatus>.Succeeded(state)));
        client.Status.Returns(status);
        await using WebApplication app = BuildHttpApp(client);
        await app.StartAsync();
        using HttpResponseMessage response = await app.GetTestClient().GetAsync(
            "/api/agents/operations/status/interactions/interaction-1/proposal");
        ((int)response.StatusCode).ShouldBe(200);
        (await response.Content.ReadFromJsonAsync<AgentOperationResult<ProposalOperationStatus>>())!.Value.ShouldBe(state);
    }

    [Fact]
    public async Task LegacyCommandOutcomeRouteReturnsTypedRejectionWithCorrelation()
    {
        await using WebApplication app = BuildHttpApp(AgentsClient.Unavailable());
        await app.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "/api/agents/operations/providers/commands/system/message-1");
        request.Headers.Add("X-Correlation-ID", CorrelationId);
        using HttpResponseMessage response = await app.GetTestClient().SendAsync(request);
        ((int)response.StatusCode).ShouldBe(422);
        var body = await response.Content.ReadFromJsonAsync<AgentOperationResult<AgentSetupWriteStatus>>();
        body!.Status.ShouldBe(AgentOperationStatus.Rejected);
        body.CorrelationId.ShouldBe(CorrelationId);
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("\"value\"");
    }

    [Theory]
    [InlineData(AgentOperationStatus.Succeeded, 200)]
    [InlineData(AgentOperationStatus.Degraded, 200)]
    [InlineData(AgentOperationStatus.Pending, 202)]
    [InlineData(AgentOperationStatus.Checking, 202)]
    [InlineData(AgentOperationStatus.NotAuthorized, 403)]
    [InlineData(AgentOperationStatus.ValidationFailed, 400)]
    [InlineData(AgentOperationStatus.NotFound, 404)]
    [InlineData(AgentOperationStatus.Conflict, 409)]
    [InlineData(AgentOperationStatus.Stale, 409)]
    [InlineData(AgentOperationStatus.Rejected, 422)]
    [InlineData(AgentOperationStatus.Blocked, 503)]
    [InlineData(AgentOperationStatus.Unavailable, 503)]
    [InlineData(AgentOperationStatus.UnableToVerify, 503)]
    [InlineData(AgentOperationStatus.Unknown, 500)]
    [InlineData((AgentOperationStatus)999, 500)]
    public async Task OperationHttpStatusesPreserveTypedBodiesAndCorrelation(AgentOperationStatus status, int expected)
    {
        IProviderCatalogOperations catalog = Substitute.For<IProviderCatalogOperations>();
        var body = new AgentOperationResult<ProviderCatalogInspectionResult>(status,
            new ProviderCatalogInspectionResult(ProviderCatalogInspectionStatus.Success, [], null), CorrelationId: CorrelationId);
        catalog.ListEntriesAsync(default, default, default, default).ReturnsForAnyArgs(
            new ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>>(body));
        await using WebApplication app = BuildHttpApp(AgentsClientWith(catalog));
        await app.StartAsync();

        using HttpResponseMessage response = await app.GetTestClient().GetAsync(
            "/api/agents/operations/providers/?includeDisabled=false");

        ((int)response.StatusCode).ShouldBe(expected);
        var actual = await response.Content.ReadFromJsonAsync<AgentOperationResult<ProviderCatalogInspectionResult>>();
        actual!.CorrelationId.ShouldBe(CorrelationId);
        actual.Value.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(AgentSetupWriteOutcome.Unknown, 500)]
    [InlineData(AgentSetupWriteOutcome.Submitted, 202)]
    [InlineData(AgentSetupWriteOutcome.AlreadyApplied, 200)]
    [InlineData(AgentSetupWriteOutcome.AwaitingProjection, 202)]
    [InlineData(AgentSetupWriteOutcome.NotAuthorized, 403)]
    [InlineData(AgentSetupWriteOutcome.ValidationFailed, 400)]
    [InlineData(AgentSetupWriteOutcome.NotFound, 404)]
    [InlineData(AgentSetupWriteOutcome.Conflict, 409)]
    [InlineData(AgentSetupWriteOutcome.Rejected, 422)]
    [InlineData(AgentSetupWriteOutcome.Unavailable, 503)]
    [InlineData(AgentSetupWriteOutcome.UnableToVerify, 503)]
    [InlineData((AgentSetupWriteOutcome)999, 500)]
    public async Task VersionedOutcomeMapsTheTypedValue(AgentSetupWriteOutcome outcome, int expected)
    {
        IProviderCatalogOperations catalog = Substitute.For<IProviderCatalogOperations>();
        catalog.GetCommandWriteOutcomeAsync(default!, default!, default, default).ReturnsForAnyArgs(
            new ValueTask<AgentOperationResult<AgentSetupWriteOutcome>>(
                AgentOperationResult<AgentSetupWriteOutcome>.Succeeded(outcome, correlationId: CorrelationId)));
        await using WebApplication app = BuildHttpApp(AgentsClientWith(catalog));
        await app.StartAsync();
        using HttpResponseMessage response = await app.GetTestClient().GetAsync(
            "/api/v1/agents/operations/providers/commands/system/message-1");
        ((int)response.StatusCode).ShouldBe(expected);
        (await response.Content.ReadFromJsonAsync<AgentOperationResult<AgentSetupWriteOutcome>>())!
            .CorrelationId.ShouldBe(CorrelationId);
    }

    [Fact]
    public void Operation_endpoints_register_stable_route_area()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>(Substitute.For<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>());
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
        builder.Services.AddSingleton<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>(Substitute.For<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>());
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
        builder.Services.AddSingleton<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>(Substitute.For<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>());
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
        builder.Services.AddSingleton<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>(Substitute.For<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>());
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

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("enable")]
    [InlineData("disable")]
    [InlineData("tenant-enablement")]
    public async Task Governance_write_http_routes_forward_retry_and_correlation_headers(string write)
    {
        IProviderCatalogOperations catalog = Substitute.For<IProviderCatalogOperations>();
        var accepted = new ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>>(
            AgentOperationResult<ProviderCatalogCommandAcceptance>.Succeeded(
                new("openai", "gpt-4o", MessageId, CorrelationId, AgentSetupTruthState.Submitted)));
        catalog.CreateEntryAsync(default!, default, default).ReturnsForAnyArgs(accepted);
        catalog.UpdateEntryAsync(default!, default, default).ReturnsForAnyArgs(accepted);
        catalog.EnableEntryAsync(default!, default, default).ReturnsForAnyArgs(accepted);
        catalog.DisableEntryAsync(default!, default, default).ReturnsForAnyArgs(accepted);
        catalog.SetTenantEnablementAsync(default!, default, default).ReturnsForAnyArgs(accepted);
        await using WebApplication app = BuildHttpApp(AgentsClientWith(catalog));
        await app.StartAsync();
        var terms = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1);
        var timeout = new ProviderModelTimeoutPolicy(30_000, 3);
        var pricing = new ProviderModelPricing("USD", 0.002m, 0.008m, 1);
        (HttpMethod method, string path, object body) = write switch
        {
            "create" => (HttpMethod.Post, "", (object)new CreateProviderModelEntry("openai", "gpt-4o", "GPT-4o", true,
                true, 128_000, 16_000, timeout, ProviderModelCapabilityFlags.Streaming, "cfg", pricing, terms)),
            "update" => (HttpMethod.Put, "", new UpdateProviderModelEntry("openai", "gpt-4o", "GPT-4o", true,
                128_000, 16_000, timeout, ProviderModelCapabilityFlags.Streaming, "cfg", pricing, 1, terms)),
            "enable" => (HttpMethod.Post, "/enable", new EnableProviderModelEntry("openai", "gpt-4o", 0)),
            "disable" => (HttpMethod.Post, "/disable", new DisableProviderModelEntry("openai", "gpt-4o", 0)),
            _ => (HttpMethod.Post, "/tenant/enablement", new SetTenantProviderModelEnablement("acme", "openai",
                "gpt-4o", true, 0, null)),
        };
        using var request = new HttpRequestMessage(method, "/api/agents/operations/providers" + path)
        {
            Content = JsonContent.Create(body, body.GetType()),
        };
        request.Headers.Add("X-Correlation-ID", CorrelationId);
        request.Headers.Add("Idempotency-Key", MessageId);

        using HttpResponseMessage response = await app.GetTestClient().SendAsync(request);

        response.EnsureSuccessStatusCode();
        AgentOperationOptions? forwarded = catalog.ReceivedCalls().ShouldHaveSingleItem().GetArguments()[1]
            .ShouldBeOfType<AgentOperationOptions>();
        forwarded.CorrelationId.ShouldBe(CorrelationId);
        forwarded.IdempotencyKey.ShouldBe(MessageId);
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
        builder.Services.AddSingleton<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>(Substitute.For<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>());
        builder.Services.AddSingleton(client);

        WebApplication app = builder.Build();
        app.MapAgentsOperationEndpoints();

        return app;
    }

    private static WebApplication BuildHttpApp(IAgentsClient client)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>(Substitute.For<Hexalith.Agents.Server.Ports.IAgentDependencyAvailability>());
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
        // HTTP mappings are exercised through TestServer by OperationHttpStatusesPreserveTypedBodiesAndCorrelation.

        body.Position = 0;
        using StreamReader reader = new(body);
        return await reader.ReadToEndAsync().ConfigureAwait(true);
    }
}
