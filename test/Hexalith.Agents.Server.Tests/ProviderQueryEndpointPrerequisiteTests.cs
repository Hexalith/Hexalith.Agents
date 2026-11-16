using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Queries;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Actual SDK HTTP query prerequisites with synthetic authority, distinct from accepted owner/live evidence.</summary>
public sealed class ProviderQueryEndpointPrerequisiteTests
{
    private const string Subject = "authenticated-human";
    private const string ChannelToken = "local-provider-query-channel";
    private const string Issuer = "local-provider-query-issuer";
    private static readonly string _signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    /// <summary>List and detail pass SDK authentication, catalog enrollment and independent current authority.</summary>
    [Theory]
    [InlineData(false, "valid")]
    [InlineData(true, "valid")]
    [InlineData(false, "missing-verifier")]
    [InlineData(true, "missing-verifier")]
    [InlineData(false, "false-verifier")]
    [InlineData(true, "false-verifier")]
    [InlineData(false, "throwing-verifier")]
    [InlineData(true, "throwing-verifier")]
    [InlineData(false, "wrong-human")]
    [InlineData(true, "wrong-human")]
    [InlineData(false, "expired-authority")]
    [InlineData(true, "expired-authority")]
    [InlineData(false, "revoked-authority")]
    [InlineData(true, "revoked-authority")]
    [InlineData(false, "wrong-workload")]
    [InlineData(true, "wrong-workload")]
    [InlineData(false, "wrong-operation")]
    [InlineData(true, "wrong-operation")]
    [InlineData(false, "expired-assertion")]
    [InlineData(true, "expired-assertion")]
    [InlineData(false, "missing-channel")]
    [InlineData(true, "missing-channel")]
    [InlineData(false, "missing-assertion")]
    [InlineData(true, "missing-assertion")]
    [InlineData(false, "wrong-catalog-generation")]
    [InlineData(true, "wrong-catalog-generation")]
    public async Task HttpQueryRequiresBoundCallerVerifierAndCurrentAuthority(bool detail, string scenario)
    {
        var clock = new Story54TimeProvider();
        var actors = Substitute.For<ICurrentHumanActorSource>();
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        var availability = Substitute.For<IAgentDependencyAvailability>();
        var accessor = new HttpContextAccessor();
        actors.ReadAsync("system", Subject, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            HttpContext http = accessor.HttpContext.ShouldNotBeNull();
            VerifiedQueryCaller.Get(http).ShouldNotBeNull().Principal.FindFirst("sub")!.Value.ShouldBe(Subject);
            http.User.HasClaim(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore").ShouldBeTrue();
            return new CurrentHumanActorEvidence("system", Subject, "stable-actor", "human-party", 1, 1, clock.Now, clock.Now.AddSeconds(5));
        });
        tenants.ReadAsync("system", true, Arg.Any<CancellationToken>()).Returns(new TenantAuthorityEvidence(
            "system", "global-administrators", 1, clock.Now,
            scenario == "expired-authority" ? clock.Now : clock.Now.AddSeconds(5),
            [new(1, "stable-actor", null, HttpAgentAdministrationContextProvider.PlatformOperatorRole, scenario != "revoked-authority")],
            SourceAggregateId: "global-administrators"));
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
            StreamReadRequest request = call.Arg<StreamReadRequest>();
            return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                new StreamReadMetadata(request.FromSequence, null, null, 1, 0, false, null));
        });
        var verifier = Substitute.For<IDomainServiceAdministratorVerifier>();
        verifier.IsCurrentGlobalAdministratorAsync(Arg.Any<DomainServiceAdministratorClaim>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.Arg<CancellationToken>().ThrowIfCancellationRequested();
                if (scenario == "throwing-verifier") { throw new InvalidOperationException("local authority unavailable"); }
                DomainServiceAdministratorClaim claim = call.Arg<DomainServiceAdministratorClaim>();
                return Task.FromResult(scenario != "false-verifier" && claim.UserId == Subject
                    && claim.TenantId == "system" && claim.Domain == ProviderCatalogReadModelAddresses.Domain);
            });
        var catalog = new FixtureProviderQueryRouteCatalog();
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DaprAppChannelToken.ConfigurationKey] = ChannelToken,
            ["EventStore:DomainService:AppId"] = "agents",
            ["Authentication:JwtBearer:Issuer"] = Issuer,
            ["Authentication:JwtBearer:Audience"] = "hexalith-eventstore",
            ["Authentication:JwtBearer:SigningKey"] = _signingKey,
            ["Authentication:JwtBearer:AllowedAlgorithms:0"] = SecurityAlgorithms.HmacSha256,
        });
        builder.AddEventStoreDomainService(typeof(AgentsAssemblyMarker).Assembly);
        builder.Services.AddSingleton<IRouteCatalogAuthority>(catalog);
        builder.Services.Configure<RouteCatalogHostOptions>(options => options.Identity =
            new("agents", "agents", "local-query-inventory", "local-query-topology", "local-query-authentication"));
        builder.Services.AddSingleton<TimeProvider>(clock);
        builder.Services.AddSingleton<IHttpContextAccessor>(accessor);
        builder.Services.AddSingleton<IReadModelStore>(store);
        builder.Services.AddSingleton<IReadModelBatchStore>(store);
        builder.Services.AddSingleton(gateway);
        builder.Services.AddSingleton<IAgentAdministrationContextProvider>(
            new HttpAgentAdministrationContextProvider(accessor, tenants, actors, availability, clock));
        builder.Services.AddSingleton<ITenantAccessReader>(new ProjectedTenantAccessReader(tenants, availability, clock, actors));
        builder.Services.AddSingleton(Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = "statestore" }));
        builder.Services.AddTransient<IDomainQueryHandler, ListProviderCatalogEntriesQueryHandler>();
        builder.Services.AddTransient<IDomainQueryHandler, GetProviderCatalogEntryQueryHandler>();
        if (scenario != "missing-verifier") { builder.Services.AddSingleton(verifier); }
        await using WebApplication app = builder.Build();
        app.UseEventStoreDomainService();
        await app.StartAsync(TestContext.Current.CancellationToken);
        object payload = detail ? new GetProviderCatalogEntryQuery("provider", "model") : new ListProviderCatalogEntriesQuery(true);
        var query = new QueryEnvelope("system", ProviderCatalogReadModelAddresses.Domain, ProviderCatalogIdentity.EntryId("provider", "model"),
            detail ? GetProviderCatalogEntryQuery.QueryType : ListProviderCatalogEntriesQuery.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()), "local-query-correlation", Subject, isGlobalAdmin: true)
        {
            OriginalActorId = Subject,
            CallerAuthority = new("authenticated-caller", [HttpAgentAdministrationContextProvider.PlatformOperatorRole], ["read_metadata"]),
        };
        string assertion = Assertion(query, scenario);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/query")
        {
            Content = JsonContent.Create(scenario == "wrong-human" ? query with { UserId = "foreign-human", OriginalActorId = "foreign-human" } : query),
        };
        if (scenario != "missing-channel") { request.Headers.Add(DaprAppChannelToken.HeaderName, ChannelToken); }
        if (scenario != "missing-assertion") { request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion); }
        request.Headers.Add(RouteCatalogInvocationHandler.RootHeader, scenario == "wrong-catalog-generation" ? "foreign-root" : catalog.Snapshot.RootDigest);
        request.Headers.Add(RouteCatalogInvocationHandler.EpochHeader, "1");
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        bool unauthenticated = scenario is "wrong-workload" or "expired-assertion" or "missing-channel" or "missing-assertion";
        bool unavailable = scenario is "throwing-verifier" or "wrong-catalog-generation";
        response.StatusCode.ShouldBe(unauthenticated ? HttpStatusCode.Unauthorized
            : scenario == "wrong-operation" ? HttpStatusCode.Forbidden
            : unavailable ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (scenario != "valid")
        {
            body.ShouldNotContain("protected-provider-label");
            body.ShouldNotContain("cfg-ref");
            store.GetCount.ShouldBe(0);
            gateway.ReceivedCalls().ShouldBeEmpty();
        }
        if (response.StatusCode == HttpStatusCode.OK)
        {
            QueryResult result = JsonSerializer.Deserialize<QueryResult>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldNotBeNull();
            result.Success.ShouldBe(scenario != "wrong-human");
            if (scenario != "valid" && result.PayloadBytes is { Length: > 0 } rawPayload)
            {
                // Inspect every decoded JSON member before typed deserialization can discard unknown fields.
                using JsonDocument payloadDocument = JsonDocument.Parse(rawPayload);
                AssertNoProtectedText(payloadDocument.RootElement, "cfg-ref");
                AssertNoProtectedText(payloadDocument.RootElement, "protected-provider-label");
            }
            if (scenario == "wrong-human") { result.ErrorMessage.ShouldBe("safe-denial"); }
            else
            {
                ProviderCatalogInspectionResult inspection = JsonSerializer.Deserialize<ProviderCatalogInspectionResult>(
                    result.PayloadBytes.ShouldNotBeNull(), new JsonSerializerOptions(JsonSerializerDefaults.Web)
                    { Converters = { new JsonStringEnumConverter() } }).ShouldNotBeNull();
                inspection.Status.ShouldBe(scenario == "valid" ? ProviderCatalogInspectionStatus.Success : ProviderCatalogInspectionStatus.NotAuthorized);
                if (scenario == "valid")
                {
                    ProviderCatalogEntryView entry = inspection.Entries.ShouldHaveSingleItem();
                    entry.DisplayLabel.ShouldBe("protected-provider-label");
                    entry.ConfigurationReferenceId.ShouldBe("cfg-ref");
                    store.GetCount.ShouldBe(1);
                }
                else { inspection.Entries.ShouldBeEmpty(); }
            }
        }
        else { body.ShouldBeEmpty(); }
        if (unauthenticated || unavailable || scenario is "wrong-operation" or "wrong-human")
        {
            actors.ReceivedCalls().ShouldBeEmpty();
            tenants.ReceivedCalls().ShouldBeEmpty();
        }
    }

    private static void AssertNoProtectedText(JsonElement value, string protectedText)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in value.EnumerateObject())
            {
                property.Name.ShouldNotContain(protectedText);
                AssertNoProtectedText(property.Value, protectedText);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray()) { AssertNoProtectedText(item, protectedText); }
        }
        else if (value.ValueKind == JsonValueKind.String)
        {
            value.GetString().ShouldNotBeNull().ShouldNotContain(protectedText);
        }
    }

    private static string Assertion(QueryEnvelope query, string scenario)
    {
        DateTime issued = scenario == "expired-assertion" ? DateTime.UtcNow.AddMinutes(-10) : DateTime.UtcNow;
        Dictionary<string, object> claims = QueryCallerAuthorityBinding.Bind(query).ToDictionary(pair => pair.Key, pair => (object)pair.Value);
        claims[EventStoreWorkloadAuthenticationDefaults.CallerClaimType] = scenario == "wrong-workload" ? "foreign-gateway" : "eventstore";
        claims[EventStoreWorkloadAuthenticationDefaults.OperationClaimType] = scenario == "wrong-operation"
            ? EventStoreWorkloadOperations.DomainServiceProcess : EventStoreWorkloadOperations.DomainServiceQuery;
        return new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer, Audience = "agents", IssuedAt = issued, NotBefore = issued, Expires = issued.AddMinutes(2), Claims = claims,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)), SecurityAlgorithms.HmacSha256),
        });
    }
}
