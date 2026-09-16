namespace Hexalith.Agents.Server.Tests;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.EventStore;

using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Configuration;
using Hexalith.EventStore.Controllers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Pipeline;
using Hexalith.EventStore.Server.Pipeline.Commands;
using Hexalith.EventStore.Validation;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// Split-provider gateway integration tests for the explicit Agents EventStore package.
/// </summary>
public sealed class AgentsEventStoreGatewayIntegrationTests
{
    private const string AgentsAppId = "agents";

    [Fact]
    public void Gateway_registration_is_explicit_idempotent_and_owns_all_five_setup_adapters()
    {
        ServiceCollection gatewayServices = new();
        _ = gatewayServices.AddAgentsEventStore(AgentsAppId);
        _ = gatewayServices.AddAgentsEventStore(AgentsAppId);
        using ServiceProvider gateway = gatewayServices.BuildServiceProvider();

        string[] commandTypes = gateway.GetServices<IIdempotencyIntentAdapter>()
            .Select(adapter => adapter.CommandType)
            .Order(StringComparer.Ordinal)
            .ToArray();
        commandTypes.ShouldBe(
        [
            nameof(ActivateAgent),
            nameof(ConfigureAgentResponseMode),
            nameof(CreateAgent),
            nameof(DisableAgent),
            nameof(UpdateAgentConfiguration),
        ]);
        gateway.GetServices<ITrustedCommandExtensionPolicy>().Count().ShouldBe(1);

        // This deliberately represents the independently composed Agents process. Importing its Server assembly
        // must not make gateway admission adapters appear there.
        ServiceCollection agentsServices = new();
        using ServiceProvider agents = agentsServices.BuildServiceProvider();
        agents.GetServices<IIdempotencyIntentAdapter>().ShouldBeEmpty();
        agents.GetServices<ITrustedCommandExtensionPolicy>().ShouldBeEmpty();
    }

    [Fact]
    public void Activation_intent_excludes_dependency_evidence_but_keeps_payload_authorization_and_version()
    {
        IdempotencyIntentAdapterRegistry registry = Registry();
        SubmitCommand original = ActivationCommand(
            payload: "{\"value\":1,\"nested\":{\"b\":2,\"a\":1}}",
            version: "7",
            providerVerdict: nameof(ProviderSelectionValidationStatus.Valid),
            approverVerdict: nameof(ApproverPolicyValidationStatus.Valid));
        SubmitCommand reorderedRetry = ActivationCommand(
            payload: "{\"nested\":{\"a\":1,\"b\":2},\"value\":1}",
            version: "7",
            providerVerdict: nameof(ProviderSelectionValidationStatus.Unavailable),
            approverVerdict: nameof(ApproverPolicyValidationStatus.Unavailable));

        TrustedIdempotencyDescriptor descriptor = registry.Resolve(original);
        descriptor.AdapterId.ShouldBe("Hexalith.Agents.EventStore.ActivateAgent.v1");
        descriptor.OperationId.ShouldBe("agents.setup.activate");
        descriptor.DescriptorVersion.ShouldBe(1);
        registry.Resolve(reorderedRetry).CanonicalIntent.ShouldBe(descriptor.CanonicalIntent);

        SubmitCommand[] conflicts =
        [
            ActivationCommand("{\"value\":2}", "7", nameof(ProviderSelectionValidationStatus.Valid), nameof(ApproverPolicyValidationStatus.Valid)),
            ActivationCommand("{\"value\":1}", "8", nameof(ProviderSelectionValidationStatus.Valid), nameof(ApproverPolicyValidationStatus.Valid)),
            ActivationCommand("{\"value\":1}", "7", nameof(ProviderSelectionValidationStatus.Valid), nameof(ApproverPolicyValidationStatus.Valid), administratorAuthorization: "false"),
        ];
        foreach (SubmitCommand conflict in conflicts)
        {
            registry.Resolve(conflict).CanonicalIntent.ShouldNotBe(descriptor.CanonicalIntent);
        }
    }

    [Fact]
    public async Task Real_gateway_HTTP_path_replays_the_same_intent_and_conflicts_changed_semantics_without_a_second_execution()
    {
        var executedCommands = new List<SubmitCommand>();
        await using WebApplication domain = BuildDomainApp(executedCommands.Add);
        await domain.StartAsync().ConfigureAwait(true);
        domain.Services.GetServices<IIdempotencyIntentAdapter>().ShouldBeEmpty();
        domain.Services.GetServices<ITrustedCommandExtensionPolicy>().ShouldBeEmpty();
        using HttpClient domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient);
        await using WebApplication gateway = BuildGatewayApp(
            Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId),
            ledger,
            registerAgentsIntegration: true);
        await gateway.StartAsync().ConfigureAwait(true);
        using HttpClient client = gateway.GetTestClient();

        SubmitCommandRequest original = ActivationRequest(
            payload: JsonSerializer.SerializeToElement(new { value = 1, nested = new { b = 2, a = 1 } }),
            version: "7",
            providerVerdict: nameof(ProviderSelectionValidationStatus.Valid),
            approverVerdict: nameof(ApproverPolicyValidationStatus.Valid));
        SubmitCommandRequest reorderedRetry = ActivationRequest(
            payload: JsonSerializer.SerializeToElement(new { nested = new { a = 1, b = 2 }, value = 1 }),
            version: "7",
            providerVerdict: nameof(ProviderSelectionValidationStatus.Unavailable),
            approverVerdict: nameof(ApproverPolicyValidationStatus.Unavailable));

        using HttpResponseMessage originalResponse = await client
            .PostAsJsonAsync("/api/v1/commands", original)
            .ConfigureAwait(true);
        originalResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        SubmitCommandResponse originalReceipt = (await originalResponse.Content
            .ReadFromJsonAsync<SubmitCommandResponse>()
            .ConfigureAwait(true)).ShouldNotBeNull();

        using HttpResponseMessage replayResponse = await client
            .PostAsJsonAsync("/api/v1/commands", reorderedRetry)
            .ConfigureAwait(true);
        replayResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        SubmitCommandResponse replayReceipt = (await replayResponse.Content
            .ReadFromJsonAsync<SubmitCommandResponse>()
            .ConfigureAwait(true)).ShouldNotBeNull();
        replayReceipt.CorrelationId.ShouldBe(originalReceipt.CorrelationId);
        replayReceipt.MessageId.ShouldBe(originalReceipt.MessageId);
        replayReceipt.ResultPayload.ShouldNotBeNull().GetRawText()
            .ShouldBe(originalReceipt.ResultPayload.ShouldNotBeNull().GetRawText());
        executedCommands.Count.ShouldBe(1);
        executedCommands[0].Extensions?[AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion]
            .ShouldBe("7");

        SubmitCommandRequest[] conflicts =
        [
            ActivationRequest(JsonSerializer.SerializeToElement(new { value = 2 }), "7", nameof(ProviderSelectionValidationStatus.Valid), nameof(ApproverPolicyValidationStatus.Valid)),
            ActivationRequest(JsonSerializer.SerializeToElement(new { value = 1 }), "8", nameof(ProviderSelectionValidationStatus.Valid), nameof(ApproverPolicyValidationStatus.Valid)),
        ];
        foreach (SubmitCommandRequest conflict in conflicts)
        {
            using HttpResponseMessage response = await client
                .PostAsJsonAsync("/api/v1/commands", conflict)
                .ConfigureAwait(true);
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
        }

        executedCommands.Count.ShouldBe(1);
        ledger.ExecutionCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false, DaprInternalAuthenticationOptions.SchemeName)]
    [InlineData(true, "Bearer")]
    public async Task Missing_gateway_registration_or_non_Dapr_caller_rejects_reserved_extensions_before_execution(
        bool registerAgentsIntegration,
        string authenticationType)
    {
        int domainExecutions = 0;
        await using WebApplication domain = BuildDomainApp(_ => domainExecutions++);
        await domain.StartAsync().ConfigureAwait(true);
        using HttpClient domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient);
        await using WebApplication gateway = BuildGatewayApp(
            Principal(authenticationType, AgentsAppId),
            ledger,
            registerAgentsIntegration);
        await gateway.StartAsync().ConfigureAwait(true);
        using HttpClient client = gateway.GetTestClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/commands",
            ActivationRequest(
                JsonSerializer.SerializeToElement(new ActivateAgent()),
                "7",
                nameof(ProviderSelectionValidationStatus.Valid),
                nameof(ApproverPolicyValidationStatus.Valid))).ConfigureAwait(true);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        domainExecutions.ShouldBe(0);
        ledger.ExecutionCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ConfigureAgentResponseMode), AgentSetupTrustedExtensions.AgentAdministrator, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(CreateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(DisableAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(UpdateAgentConfiguration), AgentSetupTrustedExtensions.AgentAdministrator, "true", true)]
    [InlineData("Bearer", AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, "other-app", "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "other-domain", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", "OtherCommand", AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "True", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(UpdateAgentConfiguration), AgentSetupTrustedExtensions.ProviderSelectionValidation, "Valid", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ProviderSelectionValidation, "valid", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, "07", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), "agent:unknown", "true", false)]
    public void Reserved_extension_policy_accepts_only_the_exact_Dapr_identity_command_key_and_value(
        string authenticationType,
        string appId,
        string domain,
        string commandType,
        string key,
        string value,
        bool expected)
    {
        ITrustedCommandExtensionPolicy policy = Policy();

        policy.Accepts(
            Principal(authenticationType, appId),
            Request(domain, commandType),
            key,
            value).ShouldBe(expected);
    }

    [Fact]
    public void Reserved_extension_policy_rejects_missing_duplicate_or_foreign_identity_claims()
    {
        ITrustedCommandExtensionPolicy policy = Policy();
        SubmitCommandRequest request = Request("agent", nameof(ActivateAgent));
        var missingClaim = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "system:agents")],
            DaprInternalAuthenticationOptions.SchemeName));
        var duplicateClaim = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "system:agents"),
                new Claim("dapr_caller_app_id", AgentsAppId),
                new Claim("dapr_caller_app_id", AgentsAppId),
            ],
            DaprInternalAuthenticationOptions.SchemeName));
        var claimOnAnotherIdentity = new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim("sub", "system:agents")], DaprInternalAuthenticationOptions.SchemeName),
            new ClaimsIdentity([new Claim("dapr_caller_app_id", AgentsAppId)], "Bearer"),
        ]);
        var additionalAuthenticatedIdentity = new ClaimsPrincipal(
        [
            new ClaimsIdentity(
            [
                new Claim("sub", "system:agents"),
                new Claim("dapr_caller_app_id", AgentsAppId),
            ],
            DaprInternalAuthenticationOptions.SchemeName),
            new ClaimsIdentity([new Claim("sub", "public-user")], "Bearer"),
        ]);

        foreach (ClaimsPrincipal principal in new[]
        {
            missingClaim,
            duplicateClaim,
            claimOnAnotherIdentity,
            additionalAuthenticatedIdentity,
        })
        {
            policy.Accepts(
                principal,
                request,
                AgentSetupTrustedExtensions.AgentAdministrator,
                "true").ShouldBeFalse();
        }
    }

    private static IdempotencyIntentAdapterRegistry Registry()
    {
        ServiceCollection services = new();
        _ = services.AddAgentsEventStore(AgentsAppId);
        using ServiceProvider provider = services.BuildServiceProvider();
        return new IdempotencyIntentAdapterRegistry(
            provider.GetServices<IIdempotencyIntentAdapter>().ToArray(),
            new CanonicalIdempotencyIntentEncoder());
    }

    private static ITrustedCommandExtensionPolicy Policy()
    {
        ServiceCollection services = new();
        _ = services.AddAgentsEventStore(AgentsAppId);
        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ITrustedCommandExtensionPolicy>();
    }

    private static SubmitCommand ActivationCommand(
        string payload,
        string version,
        string providerVerdict,
        string approverVerdict,
        string administratorAuthorization = "true")
        => new(
            MessageId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            Tenant: "tenant-a",
            Domain: "agent",
            AggregateId: "agent-a",
            CommandType: nameof(ActivateAgent),
            Payload: Encoding.UTF8.GetBytes(payload),
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            UserId: "system:agents",
            Extensions: new Dictionary<string, string>
            {
                [AgentSetupTrustedExtensions.AgentAdministrator] = administratorAuthorization,
                [AgentSetupTrustedExtensions.ProviderSelectionValidation] = providerVerdict,
                [AgentSetupTrustedExtensions.ApproverPolicyValidation] = approverVerdict,
                [AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion] = version,
            },
            IsGlobalAdmin: true,
            IdempotencyKey: "01ARZ3NDEKTSV4RRFFQ69G5FAV");

    private static ClaimsPrincipal Principal(string authenticationType, string appId)
        => new(new ClaimsIdentity(
        [
            new Claim("sub", $"system:{appId}"),
            new Claim("dapr_caller_app_id", appId),
        ],
        authenticationType));

    private static SubmitCommandRequest Request(string domain, string commandType)
        => new(
            MessageId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            Tenant: "tenant-a",
            Domain: domain,
            AggregateId: "agent-a",
            CommandType: commandType,
            Payload: JsonSerializer.SerializeToElement(new ActivateAgent()),
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            Extensions: null,
            IdempotencyKey: "01ARZ3NDEKTSV4RRFFQ69G5FAV");

    private static SubmitCommandRequest ActivationRequest(
        JsonElement payload,
        string version,
        string providerVerdict,
        string approverVerdict)
        => Request("agent", nameof(ActivateAgent)) with
        {
            Payload = payload,
            Extensions = new Dictionary<string, string>
            {
                [AgentSetupTrustedExtensions.AgentAdministrator] = "true",
                [AgentSetupTrustedExtensions.ProviderSelectionValidation] = providerVerdict,
                [AgentSetupTrustedExtensions.ApproverPolicyValidation] = approverVerdict,
                [AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion] = version,
            },
        };

    private static WebApplication BuildDomainApp(Action<SubmitCommand> onExecute)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        WebApplication app = builder.Build();
        app.MapPost("/process", (SubmitCommand command) =>
        {
            onExecute(command);
            return Results.Ok(new SubmitCommandResult(
                command.CorrelationId,
                "{\"effect\":\"Applied\",\"configurationVersion\":8}",
                command.MessageId));
        });
        return app;
    }

    private static WebApplication BuildGatewayApp(
        ClaimsPrincipal principal,
        InMemoryAdmissionLedger ledger,
        bool registerAgentsIntegration)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        _ = builder.Services.AddAuthorization();
        _ = builder.Services.AddControllers().AddApplicationPart(typeof(CommandsController).Assembly);
        builder.Services.AddSingleton<IOptions<ExtensionMetadataOptions>>(
            Options.Create(new ExtensionMetadataOptions()));
        builder.Services.AddSingleton<ExtensionMetadataSanitizer>();
        if (registerAgentsIntegration)
        {
            _ = builder.Services.AddAgentsEventStore(AgentsAppId);
        }

        builder.Services.AddSingleton<IMediator>(services =>
        {
            var mediator = Substitute.For<IMediator>();
            var registry = new IdempotencyIntentAdapterRegistry(
                services.GetServices<IIdempotencyIntentAdapter>(),
                new CanonicalIdempotencyIntentEncoder());
            IIdempotencyAdmissionCoordinator coordinator = Substitute.For<IIdempotencyAdmissionCoordinator>();
            coordinator.AdmitAsync(Arg.Any<SubmitCommand>(), Arg.Any<CancellationToken>())
                .Returns(call => ledger.AdmitAsync(
                    registry,
                    call.ArgAt<SubmitCommand>(0),
                    call.ArgAt<CancellationToken>(1)));
            coordinator.ValidateExecutionCapabilityAsync(
                    Arg.Any<IdempotencyAdmissionSession>(),
                    Arg.Any<SubmitCommand>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            coordinator.ValidateExecutionAsync(
                    Arg.Any<IdempotencyAdmissionSession>(),
                    Arg.Any<SubmitCommand>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            coordinator.BeginAsync(Arg.Any<IdempotencyAdmissionSession>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            coordinator.CompleteAsync(
                    Arg.Any<IdempotencyAdmissionSession>(),
                    Arg.Any<CommandProcessingResult>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    ledger.Complete(call.ArgAt<CommandProcessingResult>(1));
                    return Task.CompletedTask;
                });

            ICommandRouter router = Substitute.For<ICommandRouter>();
            router.RouteFencedCommandAsync(
                    Arg.Any<SubmitCommand>(),
                    Arg.Any<IdempotencyExecutionContext>(),
                    Arg.Any<CancellationToken>())
                .Returns(call => ledger.RouteAsync(
                    call.ArgAt<SubmitCommand>(0),
                    call.ArgAt<CancellationToken>(2)));
            ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
            statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult<CommandStatusRecord?>(new CommandStatusRecord(
                    CommandStatus.Completed,
                    DateTimeOffset.UtcNow,
                    AggregateId: "agent-a",
                    EventCount: 1,
                    RejectionEventType: null,
                    FailureReason: null,
                    TimeoutDuration: null,
                    MessageId: call.ArgAt<string>(1),
                    CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW")));
            ICommandArchiveStore archiveStore = Substitute.For<ICommandArchiveStore>();
            archiveStore.ReadCommandAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ArchivedCommand?>(null));
            var handler = new SubmitCommandHandler(
                statusStore,
                archiveStore,
                router,
                NullLogger<SubmitCommandHandler>.Instance,
                coordinator);
            mediator.Send(Arg.Any<SubmitCommand>(), Arg.Any<CancellationToken>())
                .Returns(call => handler.Handle(
                    call.ArgAt<SubmitCommand>(0),
                    call.ArgAt<CancellationToken>(1)));
            return mediator;
        });

        WebApplication app = builder.Build();
        app.Use(async (context, next) =>
        {
            try
            {
                context.User = principal;
                await next(context).ConfigureAwait(false);
            }
            catch (IdempotencyConflictException)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
            }
        });
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    private sealed class InMemoryAdmissionLedger(HttpClient domainClient)
    {
        private byte[]? _intent;
        private CommandProcessingResult? _result;

        public int ExecutionCount { get; private set; }

        public Task<IdempotencyAdmissionSession?> AdmitAsync(
            IdempotencyIntentAdapterRegistry registry,
            SubmitCommand command,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] intent = registry.Resolve(command).CanonicalIntent;
            if (_intent is not null)
            {
                if (!_intent.AsSpan().SequenceEqual(intent))
                {
                    return Task.FromResult<IdempotencyAdmissionSession?>(Session(
                        command,
                        IdempotencyAdmissionDecision.Conflict));
                }

                return Task.FromResult<IdempotencyAdmissionSession?>(Session(
                    command,
                    IdempotencyAdmissionDecision.Replay,
                    _result ?? throw new InvalidOperationException("Replay was requested before completion.")));
            }

            _intent = intent.ToArray();
            return Task.FromResult<IdempotencyAdmissionSession?>(Session(
                command,
                IdempotencyAdmissionDecision.Execute));
        }

        public void Complete(CommandProcessingResult result)
        {
            _result = result;
        }

        public async Task<CommandProcessingResult> RouteAsync(
            SubmitCommand command,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            using HttpResponseMessage response = await domainClient
                .PostAsJsonAsync("/process", command, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            SubmitCommandResult receipt = await response.Content
                .ReadFromJsonAsync<SubmitCommandResult>(cancellationToken: cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("The independent domain endpoint returned no receipt.");
            return new CommandProcessingResult(
                Accepted: true,
                CorrelationId: receipt.CorrelationId,
                EventCount: 1,
                ResultPayload: receipt.ResultPayload);
        }

        private static IdempotencyAdmissionSession Session(
            SubmitCommand command,
            IdempotencyAdmissionDecision decision,
            CommandProcessingResult? replay = null)
            => new(
                ActorId: "test-admission",
                FencingToken: 1,
                decision,
                replay,
                ExecutionContext: decision == IdempotencyAdmissionDecision.Execute
                    ? new IdempotencyExecutionContext(
                        IdempotencyExecutionContext.CurrentSchemaVersion,
                        "test-admission",
                        1,
                        "v1",
                        command.MessageId,
                        command.CorrelationId,
                        command.Tenant,
                        command.Domain,
                        command.AggregateId,
                        command.CommandType,
                        "test-proof")
                    : null,
                ExecutionMessageId: command.MessageId,
                ExecutionCorrelationId: command.CorrelationId);
    }
}
