using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Agents;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Queries;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Provider inspection requires the SDK-bound originating human and current independent global authority.</summary>
public sealed class VerifiedProviderQueryCallerTests
{
    private const string Subject = "authenticated-human";

    /// <summary>List and detail use the verified human while retaining the workload transport principal.</summary>
    [Theory]
    [InlineData("valid")]
    [InlineData("subject")]
    [InlineData("tenant")]
    [InlineData("authority")]
    [InlineData("stripped")]
    [InlineData("missing-binding")]
    [InlineData("workload-role")]
    [InlineData("revoked")]
    [InlineData("stale")]
    [InlineData("missing-evidence")]
    [InlineData("foreign-evidence")]
    [InlineData("not-global")]
    public async Task DispatcherBindsProviderQueriesToCurrentGlobalHumanAuthority(string mutation)
    {
        foreach (bool detail in new[] { false, true })
        {
            var clock = new Story54TimeProvider();
            object payload = detail ? new GetProviderCatalogEntryQuery("provider", "model") : new ListProviderCatalogEntriesQuery(true);
            QueryEnvelope query = new("system", ProviderCatalogReadModelAddresses.Domain, ProviderCatalogIdentity.EntryId("provider", "model"),
                detail ? GetProviderCatalogEntryQuery.QueryType : ListProviderCatalogEntriesQuery.QueryType,
                JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()), "query-correlation", Subject, isGlobalAdmin: mutation != "not-global")
            {
                OriginalActorId = Subject,
                CallerAuthority = new("authenticated-caller", [HttpAgentAdministrationContextProvider.PlatformOperatorRole], ["read_metadata"]),
            };
            ClaimsPrincipal transport = new(new ClaimsIdentity(
            [
                new Claim("sub", "gateway-workload-subject"),
                new Claim("tenantId", "transport-tenant"),
                new Claim(ClaimTypes.Role, HttpAgentAdministrationContextProvider.PlatformOperatorRole),
                new Claim(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"),
                new Claim(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
                .. (mutation == "missing-binding" ? [] : QueryCallerAuthorityBinding.Bind(query).Select(static pair => new Claim(pair.Key, pair.Value))),
            ], "verified-workload"));
            DefaultHttpContext http = new() { User = transport };
            var accessor = new HttpContextAccessor { HttpContext = http };
            var actors = Substitute.For<ICurrentHumanActorSource>();
            actors.ReadAsync("system", Subject, Arg.Any<CancellationToken>()).Returns(_ =>
            {
                VerifiedQueryCaller.Get(http).ShouldNotBeNull().Principal.FindFirst("sub")!.Value.ShouldBe(Subject);
                http.User.ShouldBeSameAs(transport);
                return new CurrentHumanActorEvidence("system", Subject, "stable-actor", "human-party", 1, 1, clock.Now, clock.Now.AddSeconds(5));
            });
            var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
            tenants.ReadAsync("system", true, Arg.Any<CancellationToken>()).Returns(mutation == "missing-evidence" ? null :
                new TenantAuthorityEvidence("system", "global-administrators", 1, clock.Now,
                    mutation == "stale" ? clock.Now : clock.Now.AddSeconds(5),
                    [new(1, "stable-actor", null, mutation == "workload-role" ? "User" : "Agents.PlatformOperator", mutation != "revoked")],
                    SourceAggregateId: mutation == "foreign-evidence" ? "foreign" : "global-administrators"));
            var availability = Substitute.For<IAgentDependencyAvailability>();
            var context = new HttpAgentAdministrationContextProvider(accessor, tenants, actors, availability, clock);
            var store = new FakeReadModelStore();
            store.Seed("statestore", ProviderCatalogReadModelAddresses.Detail("system"), new ProviderCatalogReadModel
            {
                CatalogId = "system", TenantId = "system", LastSequenceNumber = 1,
                StreamSequences = new() { [ProviderCatalogIdentity.EntryId("provider", "model")] = 1 },
                Entries = [new("provider", "model", "protected-provider-label", ProviderModelStatus.Enabled, true, 1000, 500,
                    new(30000, 3), ProviderModelCapabilityFlags.Streaming, ProviderConfigurationState.Configured, "cfg-ref", true, 1,
                    new("USD", 0.002m, 0.008m, 1), new(30, false, ["EU"], "terms-v1", 1))],
            });
            var gateway = Substitute.For<IEventStoreGatewayClient>();
            gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                var request = call.Arg<StreamReadRequest>();
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, 1, 0, false, null));
            });
            var services = new ServiceCollection();
            services.AddSingleton(gateway);
            services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IHttpContextAccessor>(accessor);
            services.AddSingleton<IAgentAdministrationContextProvider>(context);
            services.AddSingleton<ITenantAccessReader>(new ProjectedTenantAccessReader(tenants, availability, clock, actors));
            services.AddSingleton<IReadModelStore>(store);
            services.AddSingleton<IOptions<ProviderCatalogReadModelOptions>>(Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = "statestore" }));
            services.AddTransient<IDomainQueryHandler, ListProviderCatalogEntriesQueryHandler>();
            services.AddTransient<IDomainQueryHandler, GetProviderCatalogEntryQueryHandler>();
            await using var provider = services.BuildServiceProvider();
            query = mutation switch
            {
                "subject" => query with { UserId = "forged-human", OriginalActorId = "forged-human" },
                "tenant" => query with { TenantId = "foreign-tenant" },
                "authority" => query with { CallerAuthority = query.CallerAuthority with { Roles = ["forged-role"] } },
                "stripped" => query with { CallerAuthority = null },
                _ => query,
            };

            QueryResult response = await DomainQueryDispatcher.ExecuteAsync(provider, query, CancellationToken.None);

            VerifiedQueryCaller.Get(http).ShouldBeNull(); http.User.ShouldBeSameAs(transport);
            context.GetContext().ActorUserId.ShouldBe("gateway-workload-subject");
            if (mutation is "subject" or "tenant" or "authority" or "stripped" or "missing-binding")
            {
                response.Success.ShouldBeFalse(); response.ErrorMessage.ShouldBe("safe-denial");
                actors.ReceivedCalls().ShouldBeEmpty(); tenants.ReceivedCalls().ShouldBeEmpty(); store.GetCount.ShouldBe(0);
            }
            else
            {
                response.Success.ShouldBeTrue();
                var result = JsonSerializer.Deserialize<ProviderCatalogInspectionResult>(response.PayloadBytes.ShouldNotBeNull(),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } }).ShouldNotBeNull();
                result.Status.ShouldBe(mutation == "valid" ? ProviderCatalogInspectionStatus.Success : ProviderCatalogInspectionStatus.NotAuthorized);
                store.GetCount.ShouldBe(mutation == "valid" ? 1 : 0);
                if (mutation == "valid") { result.Entries.ShouldHaveSingleItem().DisplayLabel.ShouldBe("protected-provider-label"); }
                else { result.Entries.ShouldBeEmpty(); }
            }
        }
    }

    /// <summary>The module's real SDK registration installs authentication and the query operation policy.</summary>
    [Fact]
    public async Task DomainHostRegistersSdkAuthenticationWithoutAPermissiveReplacement()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(typeof(AgentsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly);
        using var provider = builder.Services.BuildServiceProvider();
        var schemes = await provider.GetRequiredService<IAuthenticationSchemeProvider>().GetAllSchemesAsync();
        schemes.ShouldNotBeEmpty();
        schemes.ShouldNotContain(scheme => scheme.HandlerType.Name.Contains("Fixture", StringComparison.Ordinal));
        var policies = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>>().Value;
        policies.GetPolicy(EventStoreDomainServicePolicies.Query).ShouldNotBeNull();
    }
}
