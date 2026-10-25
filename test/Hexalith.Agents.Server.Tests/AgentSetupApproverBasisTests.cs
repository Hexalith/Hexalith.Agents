using System.Text.Json;
using System.Text.Json.Serialization;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Queries;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Checks safe approver-basis parity across the setup APIs and queries without external seam execution.</summary>
public sealed class AgentSetupApproverBasisTests
{
    /// <summary>Gets each setup read and an undefined source kind returned by a typed source adapter.</summary>
    public static TheoryData<string, int> UndefinedSourceCases
    {
        get
        {
            var cases = new TheoryData<string, int>();
            foreach (string path in new[] { "status-api", "configuration-api", "status-query", "configuration-query" })
            {
                cases.Add(path, -1);
                cases.Add(path, 999);
            }

            return cases;
        }
    }

    /// <summary>A resolved source of an undefined kind cannot present a valid API/UI basis.</summary>
    [Theory]
    [MemberData(nameof(UndefinedSourceCases))]
    public async Task UndefinedApproverSourceRemainsUnavailableAcrossSetupReads(string path, int ordinal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        const string tenant = "tenant-a";
        const string agent = "hexa";
        var configured = new ApproverPolicySource((ApproverPolicySourceKind)ordinal, null, null);
        var policy = new AgentApproverPolicy([configured], ApproverPolicyBasisDisclosure.OperatorOnly);
        var resolution = new ApproverPolicyResolutionResult([new(configured.Kind, ApproverSourceOutcome.Resolved)
        {
            Source = configured,
        }]);
        var resolver = Substitute.For<IApproverPolicyResolver>();
        resolver.ResolveAsync(tenant, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>()).Returns(resolution);
        var store = Substitute.For<IReadModelStore>();
        store.GetAsync<AgentSetupReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<AgentSetupReadModel>(new AgentSetupReadModel
            {
                IsCreated = true,
                TenantId = tenant,
                AgentId = agent,
                DisplayName = agent,
                ConfigurationVersion = 1,
                ResponseMode = AgentResponseMode.Confirmation,
                ApproverPolicySources = policy.Sources,
                ApproverPolicyDisclosure = policy.DisclosureCategory,
            }, "etag"));
        var context = Substitute.For<IAgentAdministrationContextProvider>();
        context.GetContext().Returns(new AgentAdministrationContext(tenant, "subject", false));
        context.GetContextAsync(false, Arg.Any<CancellationToken>())
            .Returns(new AgentAdministrationContext(tenant, "stable-actor", true));
        var options = Options.Create(new AgentSetupReadModelOptions());

        AgentSetupResult setup;
        if (path.EndsWith("-query", StringComparison.Ordinal))
        {
            var tenantAccess = Substitute.For<ITenantAccessReader>();
            tenantAccess.ReadAsync(tenant, "subject", string.Empty, Arg.Any<CancellationToken>())
                .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, true));
            AgentSetupQueryHandlerBase handler = path == "status-query"
                ? new GetAgentStatusQueryHandler(store, options, tenantAccess, resolver, context)
                : new GetAgentConfigurationQueryHandler(store, options, tenantAccess, resolver, context);
            QueryResult response = await handler.ExecuteAsync(new QueryEnvelope(tenant,
                AgentSetupReadModelAddresses.Domain, agent, handler.QueryType, [], "correlation", "subject"), CancellationToken.None);
            setup = JsonSerializer.Deserialize<AgentSetupResult>(response.PayloadBytes.ShouldNotBeNull(),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new JsonStringEnumConverter() },
                }).ShouldNotBeNull();
        }
        else
        {
            var dispatcher = Substitute.For<IAgentCommandDispatcher>();
            var operations = new EventStoreAgentAdministrationOperations(context,
                new AgentAdministrationOrchestrator(dispatcher), new AgentResponseModeOrchestrator(dispatcher),
                new AgentActivationProviderRevalidation(Substitute.For<IProviderCatalogReader>(), resolver, dispatcher),
                store, options, new AgentCommandIdentityFactory(), Substitute.For<IAgentCommandStatusReader>(), resolver);
            setup = (path == "status-api"
                ? await operations.GetStatusAsync(agent)
                : await operations.GetConfigurationAsync(agent)).Value.ShouldNotBeNull();
            dispatcher.ReceivedCalls().ShouldBeEmpty();
        }

        setup.Status.ShouldBe(AgentInspectionStatus.Success);
        setup.Setup.ShouldNotBeNull().Agent.ApproverPolicyBasis.Single().Status
            .ShouldBe(ApproverPolicyValidationStatus.Unavailable);
        ApproverPolicyVerdict.Evaluate(policy, resolution).ShouldBe(ApproverPolicyValidationStatus.Unavailable);
        await resolver.Received(1).ResolveAsync(tenant, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>());
    }
}
