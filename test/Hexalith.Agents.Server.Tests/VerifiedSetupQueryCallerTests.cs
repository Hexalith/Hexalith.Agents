using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Queries;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Exercises the EventStore SDK's authenticated originating caller through its real query dispatcher.</summary>
public sealed class VerifiedSetupQueryCallerTests
{
    private const string TenantId = "tenant-a";
    private const string Subject = "authenticated-human";
    private const string AgentId = "hexa";

    /// <summary>Only SDK-verified originating identity and current tenant authority can disclose setup data.</summary>
    [Theory]
    [InlineData(true, true, "valid")]
    [InlineData(false, true, "valid")]
    [InlineData(true, false, "valid")]
    [InlineData(false, false, "valid")]
    [InlineData(true, true, "subject")]
    [InlineData(false, true, "subject")]
    [InlineData(true, true, "tenant")]
    [InlineData(false, true, "tenant")]
    [InlineData(true, true, "authority")]
    [InlineData(false, true, "authority")]
    [InlineData(true, true, "stripped")]
    [InlineData(false, true, "stripped")]
    public async Task DispatcherUsesVerifiedHumanWithoutReplacingTransportIdentity(bool statusQuery, bool administrator, string mutation)
    {
        var clock = new Story54TimeProvider();
        QueryEnvelope query = new(TenantId, AgentSetupReadModelAddresses.Domain, AgentId,
            statusQuery ? GetAgentStatusQuery.QueryType : GetAgentConfigurationQuery.QueryType,
            [], "query-correlation", Subject)
        {
            OriginalActorId = Subject,
            // Even a verified role claim cannot replace current independent role evidence.
            CallerAuthority = new("authenticated-caller",
                [HttpAgentAdministrationContextProvider.AgentsAdministratorRole], ["read_metadata"]),
        };
        ClaimsPrincipal transport = new(new ClaimsIdentity(
        [
            new Claim("sub", "gateway-workload-subject"),
            new Claim("tenantId", "transport-tenant"),
            new Claim(ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole),
            new Claim(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"),
            new Claim(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
            .. QueryCallerAuthorityBinding.Bind(query).Select(static pair => new Claim(pair.Key, pair.Value)),
        ], "verified-workload"));
        DefaultHttpContext http = new() { User = transport };
        var accessor = new HttpContextAccessor { HttpContext = http };
        var actors = Substitute.For<ICurrentHumanActorSource>();
        actors.ReadAsync(TenantId, Subject, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            VerifiedQueryCaller.Get(http).ShouldNotBeNull().Principal.FindFirst("sub")!.Value.ShouldBe(Subject);
            http.User.ShouldBeSameAs(transport);
            return new CurrentHumanActorEvidence(TenantId, Subject, "stable-actor", "human-party", 1, 1,
                clock.Now, clock.Now.AddSeconds(5));
        });
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        tenants.ReadAsync(TenantId, false, Arg.Any<CancellationToken>()).Returns(
            new TenantAuthorityEvidence(TenantId, "tenants", 2, clock.Now, clock.Now.AddSeconds(5),
            [
                new(1, string.Empty, null, "TenantActive", true),
                new(2, "stable-actor", null, administrator ? "Agents.Administrator" : "User", true),
            ], SourceAggregateId: TenantId));
        var availability = Substitute.For<IAgentDependencyAvailability>();
        var contextProvider = new HttpAgentAdministrationContextProvider(accessor, tenants, actors, availability, clock);
        var store = new FakeReadModelStore();
        store.Seed("statestore", AgentSetupReadModelAddresses.Detail(TenantId, AgentId), new AgentSetupReadModel
        {
            IsCreated = true,
            TenantId = TenantId,
            AgentId = AgentId,
            DisplayName = "protected-setup-display",
            ConfigurationVersion = 1,
        });
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(accessor);
        services.AddSingleton<IAgentAdministrationContextProvider>(contextProvider);
        services.AddSingleton<ITenantAccessReader>(new ProjectedTenantAccessReader(tenants, availability, clock, actors));
        services.AddSingleton<IReadModelStore>(store);
        services.AddSingleton<IOptions<AgentSetupReadModelOptions>>(
            Options.Create(new AgentSetupReadModelOptions { StateStoreName = "statestore" }));
        services.AddTransient<IDomainQueryHandler, GetAgentStatusQueryHandler>();
        services.AddTransient<IDomainQueryHandler, GetAgentConfigurationQueryHandler>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        query = mutation switch
        {
            "subject" => query with { UserId = "forged-subject", OriginalActorId = "forged-subject" },
            "tenant" => query with { TenantId = "foreign-tenant" },
            "authority" => query with { CallerAuthority = query.CallerAuthority with { Roles = ["Agents.PlatformOperator"] } },
            "stripped" => query with { CallerAuthority = null },
            _ => query,
        };

        QueryResult response = await DomainQueryDispatcher.ExecuteAsync(provider, query, CancellationToken.None);

        VerifiedQueryCaller.Get(http).ShouldBeNull();
        http.User.ShouldBeSameAs(transport);
        contextProvider.GetContext().ActorUserId.ShouldBe("gateway-workload-subject");
        contextProvider.GetContext().TenantId.ShouldBe("transport-tenant");
        if (mutation != "valid")
        {
            response.Success.ShouldBeFalse();
            response.ErrorMessage.ShouldBe("safe-denial");
            store.GetCount.ShouldBe(0);
            actors.ReceivedCalls().ShouldBeEmpty();
            tenants.ReceivedCalls().ShouldBeEmpty();
            return;
        }

        response.Success.ShouldBeTrue();
        AgentSetupResult setup = JsonSerializer.Deserialize<AgentSetupResult>(
            response.PayloadBytes.ShouldNotBeNull(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } })
            .ShouldNotBeNull();
        if (administrator)
        {
            setup.Status.ShouldBe(AgentInspectionStatus.Success);
            setup.Setup.ShouldNotBeNull().Agent.DisplayName.ShouldBe("protected-setup-display");
            store.GetCount.ShouldBe(1);
        }
        else
        {
            setup.ShouldBe(AgentSetupResult.NotAuthorized());
            store.GetCount.ShouldBe(0);
        }

        actors.ReceivedCalls().Count().ShouldBe(administrator ? 2 : 1);
        tenants.ReceivedCalls().Count().ShouldBe(administrator ? 2 : 1);
    }
}
